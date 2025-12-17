using Microsoft.Data.SqlClient;
using System.Data;

namespace StoredProcedureValidator
{
    public partial class TableCheckerForm : Form
    {

        private readonly string _connectionString;
        private readonly string _logFilePath;

        private DataGridView dataGridView1 = null!;
        private Button btnRunCheck = null!;

        private Form? _sqlPopup;
        private TextBox? _sqlPopupTextBox;

        private int MeasureTextHeight(string text, Font font, int width)
        {
            using var bmp = new Bitmap(1, 1);
            using var g = Graphics.FromImage(bmp);

            var size = g.MeasureString(
                text,
                font,
                width,
                new StringFormat(StringFormatFlags.LineLimit)
            );

            return (int)Math.Ceiling(size.Height);
        }
        private void InitializeSqlPopup()
        {
            _sqlPopup = new Form
            {
                FormBorderStyle = FormBorderStyle.None,
                StartPosition = FormStartPosition.Manual,
                ShowInTaskbar = false,
                TopMost = true,
                BackColor = Color.LightGreen
            };

            _sqlPopupTextBox = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                ScrollBars = ScrollBars.Vertical,
                Dock = DockStyle.Fill,
                BackColor = Color.LightGreen,
                ForeColor = Color.Black,
                Font = new Font("Consolas", 9)
            };

            _sqlPopup.Controls.Add(_sqlPopupTextBox);
            _sqlPopupTextBox.MouseWheel += (s, ev) =>
            {
                _sqlPopupTextBox.ScrollToCaret();
            };
        }

        private void dataGridView1_CellMouseEnter(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return;

            if (dataGridView1.Columns[e.ColumnIndex].Name != "Message")
                return;

            var fullText = dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex].Value as string;
            if (string.IsNullOrWhiteSpace(fullText))
                return;

            _sqlPopupTextBox!.Text = fullText;

            int popupWidth = 800;
            int padding = 10;

            _sqlPopupTextBox.Width = popupWidth - padding;

            int textHeight = MeasureTextHeight(
                fullText,
                _sqlPopupTextBox.Font!,
                _sqlPopupTextBox.Width
            );

            var screen = Screen.FromPoint(Cursor.Position).WorkingArea;

            int maxHeight = screen.Height;
            int desiredHeight = textHeight + padding;

            int finalHeight = Math.Min(desiredHeight, maxHeight);

            _sqlPopup!.Size = new Size(popupWidth, finalHeight);

            _sqlPopupTextBox.ScrollBars =
                desiredHeight > maxHeight
                    ? ScrollBars.Vertical
                    : ScrollBars.None;

            // Position popup (screen-aware)
            var mousePos = Cursor.Position;
            int x = mousePos.X + 15;
            int y = mousePos.Y + 15;

            if (x + _sqlPopup.Width > screen.Right)
                x = screen.Right - _sqlPopup.Width;

            if (y + _sqlPopup.Height > screen.Bottom)
                y = screen.Bottom - _sqlPopup.Height;

            _sqlPopup.Location = new Point(x, y);
            _sqlPopup.Show();
            _sqlPopup.Activate();
            _sqlPopupTextBox.Focus();
        }


        private void dataGridView1_CellMouseLeave(object sender, DataGridViewCellEventArgs e)
        {
            if (!_sqlPopup!.Bounds.Contains(Cursor.Position))
                _sqlPopup.Hide();
        }


        private void dataGridView1_MouseLeave(object sender, EventArgs e)
        {
            if (!_sqlPopup!.Bounds.Contains(Cursor.Position))
                _sqlPopup.Hide();
        }
        public TableCheckerForm()
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
            dataGridView1.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            dataGridView1.ScrollBars = ScrollBars.Both;
            dataGridView1.ShowCellToolTips = true;

            dataGridView1.Columns.Add("TableName", "Table");
            dataGridView1.Columns.Add("Status", "Status");

            var messageColumn = new DataGridViewTextBoxColumn
            {
                Name = "Message",
                HeaderText = "Message",
                DefaultCellStyle =
        {
            WrapMode = DataGridViewTriState.False,
            Font = new Font("Consolas", 9)
        }
            };

            dataGridView1.Columns.Add(messageColumn);

            dataGridView1.Columns[0].FillWeight = 20;
            dataGridView1.Columns[1].FillWeight = 5;
            dataGridView1.Columns[2].FillWeight = 75;

            dataGridView1.ShowCellToolTips = false;

            dataGridView1.CellMouseEnter += dataGridView1_CellMouseEnter;
            dataGridView1.CellMouseLeave += dataGridView1_CellMouseLeave;
            dataGridView1.MouseLeave += dataGridView1_MouseLeave;

            InitializeSqlPopup();
        }

        private void DataGridView1_RowPostPaint(object sender, DataGridViewRowPostPaintEventArgs e)
        {
            var grid = sender as DataGridView;
            if (grid == null) return;

            using var brush = new SolidBrush(grid.RowHeadersDefaultCellStyle.ForeColor);

            string rowNumber = (e.RowIndex + 1).ToString();

            e.Graphics.DrawString(
                rowNumber,
                grid.RowHeadersDefaultCellStyle.Font!,
                brush,
                e.RowBounds.Left + 10,
                e.RowBounds.Top + 4
            );
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

            ListTablesAndViewsContainingPatterns();
        }

        private void ListTablesAndViewsContainingPatterns()
        {
            using SqlConnection conn = new SqlConnection(_connectionString);
            conn.Open();

            string sql = @"
        SELECT 
            o.name AS ObjectName,
            s.name AS SchemaName,
            o.type_desc AS ObjectType,
            OBJECT_DEFINITION(o.object_id) AS SourceCode
        FROM sys.objects o
        JOIN sys.schemas s ON o.schema_id = s.schema_id
        WHERE 
            o.type IN ('U', 'V')   -- tables + views
            AND (
                   LOWER(OBJECT_DEFINITION(o.object_id)) LIKE @patVendor1
                OR LOWER(OBJECT_DEFINITION(o.object_id)) LIKE @patVendor2
                OR LOWER(OBJECT_DEFINITION(o.object_id)) LIKE @patVendor3
                OR LOWER(OBJECT_DEFINITION(o.object_id)) LIKE @patDate
            )
        ORDER BY o.name;
    ";

            using SqlCommand cmd = new SqlCommand(sql, conn);

            cmd.Parameters.Add("@patVendor1", SqlDbType.NVarChar, -1).Value = "%gwpnyrt%";
            cmd.Parameters.Add("@patVendor2", SqlDbType.NVarChar, -1).Value = "%zipper%";
            cmd.Parameters.Add("@patVendor3", SqlDbType.NVarChar, -1).Value = "%vegafood%";
            cmd.Parameters.Add("@patDate", SqlDbType.NVarChar, -1).Value = "%2024-12-31%";

            using SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                string objectName = reader.GetString(0);
                string schemaName = reader.GetString(1);
                string sourceCode = reader.IsDBNull(3) ? "" : reader.GetString(3);

                string fullName = $"{schemaName}.{objectName}";
                string status = "OK";
                string message = sourceCode;

                dataGridView1.Rows.Add(fullName, status, message);
            }
        }

        private void exitButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void TableCheckerForm_KeyPress(object sender, KeyPressEventArgs e)
        {

        }

        private void TableCheckerForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                this.Close();
                e.Handled = true;
            }
        }
    }
}
