using Microsoft.Data.SqlClient;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace StoredProcedureValidator
{
    public partial class MainForm : Form
    {

        private readonly string _connectionString;
        private readonly string _logFilePath;

        private DataGridView dataGridView1 = null!;
        private Button btnRunCheck = null!;

        public MainForm()
        {
            InitializeComponent();
            _connectionString =
                $"Server={Environment.GetEnvironmentVariable("VIR_SQL_SERVER_NAME")};" +
                "Database=qad;" +
                $"User Id={Environment.GetEnvironmentVariable("VIR_SQL_USER")};" +
                $"Password={Environment.GetEnvironmentVariable("VIR_SQL_PASSWORD")};" +
                "Connection Timeout=500;Trust Server Certificate=true";

            _logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "stored_proc.log");
            InitializeGrid();
        }

        private void InitializeGrid()
        {
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            dataGridView1.Columns.Add("ProcedureName", "Procedure Name");
            dataGridView1.Columns.Add("Status", "Status");
            dataGridView1.Columns.Add("Message", "Message");

            dataGridView1.Columns[0].FillWeight = 20;
            dataGridView1.Columns[1].FillWeight = 5;
            dataGridView1.Columns[2].FillWeight = 75;
        }

        private void DataGridView1_RowPrePaint(object sender, DataGridViewRowPrePaintEventArgs e)
        {
            var grid = sender as DataGridView;
            if (grid == null) return;

            var row = grid.Rows[e.RowIndex];
            if (row.IsNewRow) return;

            var statusCell = row.Cells["Status"];
            if (statusCell?.Value?.ToString() == "OK")
            {
                row.DefaultCellStyle.BackColor = Color.LightGreen;
                row.DefaultCellStyle.ForeColor = Color.Black;
            }
            else
            {
                row.DefaultCellStyle.BackColor = Color.DarkRed;
                row.DefaultCellStyle.ForeColor = Color.White;
            }
        }

        private void btnRunCheck_Click(object sender, EventArgs e)
        {
            dataGridView1.Rows.Clear();
            File.WriteAllText(_logFilePath, string.Empty);

            using SqlConnection conn = new SqlConnection(_connectionString);
            conn.Open();

            string sql = "SELECT name, OBJECT_DEFINITION(object_id) AS source FROM sys.procedures WHERE name LIKE 'sp_Refresh%';";
            using SqlCommand cmd = new SqlCommand(sql, conn);
            using SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                string procName = reader.GetString(0);
                string source = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
                File.AppendAllText(_logFilePath, $"-- {procName} --\n{source}\n\n");

                var (status, message) = AnalyzeProcedure(source);
                dataGridView1.Rows.Add(procName, status, message);
            }
        }

        private (string Status, string Message) AnalyzeProcedure(string source)
        {
            List<string> missing = new();

            if (!source.Contains("DECLARE @StartTime", StringComparison.OrdinalIgnoreCase))
                missing.Add("Missing @StartTime declaration");
            if (!source.Contains("DECLARE @EndTime", StringComparison.OrdinalIgnoreCase))
                missing.Add("Missing @EndTime declaration");
            if (!source.Contains("DECLARE @DurationSeconds", StringComparison.OrdinalIgnoreCase))
                missing.Add("Missing @DurationSeconds declaration");
            if (!source.Contains("DECLARE @Status", StringComparison.OrdinalIgnoreCase))
                missing.Add("Missing @Status declaration");
            if (!source.Contains("DECLARE @ErrorMessage", StringComparison.OrdinalIgnoreCase))
                missing.Add("Missing @ErrorMessage declaration");
            if (!source.Contains("DECLARE @ProcedureName", StringComparison.OrdinalIgnoreCase))
                missing.Add("Missing @ProcedureName declaration");
            if (!source.Contains("DECLARE @ExecutedBy", StringComparison.OrdinalIgnoreCase))
                missing.Add("Missing @ExecutedBy declaration");
            if (!source.Contains("OBJECT_NAME(@@PROCID)", StringComparison.OrdinalIgnoreCase))
                missing.Add("Missing OBJECT_NAME(@@PROCID) declaration");

            bool hasBeginTransaction = source.Contains("BEGIN TRANSACTION", StringComparison.OrdinalIgnoreCase);

            if (hasBeginTransaction)
            {
                if (!source.Contains("COMMIT TRANSACTION", StringComparison.OrdinalIgnoreCase))
                    missing.Add("Missing COMMIT TRANSACTION");
                if (!source.Contains("ROLLBACK TRANSACTION", StringComparison.OrdinalIgnoreCase))
                    missing.Add("Missing ROLLBACK TRANSACTION");
            } else
            {
                if (source.Contains("COMMIT TRANSACTION", StringComparison.OrdinalIgnoreCase))
                    missing.Add("Invalid extra COMMIT TRANSACTION");
                if (source.Contains("ROLLBACK TRANSACTION", StringComparison.OrdinalIgnoreCase))
                    missing.Add("Invalid extra ROLLBACK TRANSACTION");
            }

            if (!Regex.IsMatch(source, @"INSERT\s+INTO\s+dbo\.ProcedureExecutionLog.*CATCH", RegexOptions.IgnoreCase | RegexOptions.Singleline))
                missing.Add("Missing INSERT INTO log table in CATCH block");

            string finalInsertPattern = @"INSERT\s+INTO\s+dbo\.ProcedureExecutionLog\s*\(\s*ProcedureName,\s*StartTime,\s*EndTime,\s*DurationSeconds,\s*Status,\s*ErrorMessage,\s*ExecutedBy\s*\)\s*VALUES\s*\(\s*@ProcedureName,\s*@StartTime,\s*@EndTime,\s*@DurationSeconds,\s*@Status,\s*NULL,\s*@ExecutedBy\s*\)";
            if (!Regex.IsMatch(source.TrimEnd(), finalInsertPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline))
                missing.Add("Missing or incorrect final INSERT INTO log statement");

            string catchPattern = @"BEGIN\s+CATCH(.*?)END\s+CATCH";
            string nullErrorPattern = @"INSERT\s+INTO\s+dbo\.ProcedureExecutionLog\s*\(.*?ErrorMessage.*?\)\s*VALUES\s*\(.*?NULL.*?\)";

            var catchMatches = Regex.Matches(source, catchPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);

            foreach (Match catchMatch in catchMatches)
            {
                string catchBlock = catchMatch.Groups[1].Value;

                bool hasNullErrorMessage = Regex.IsMatch(catchBlock, nullErrorPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);

                if (hasNullErrorMessage)
                {
                    missing.Add("ERROR: This procedure inserts NULL into ErrorMessage in the CATCH block.");
                }
            }


            return missing.Count == 0 ? ("OK", "All checks passed") : ("FAILURE", string.Join("; ", missing));
        }


        private void exitButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        }
}
