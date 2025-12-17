namespace StoredProcedureValidator
{
    partial class TableCheckerForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            dataGridView1 = new DataGridView();
            btnRunCheck = new Button();
            button1 = new Button();
            buttonPanel = new Panel();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            buttonPanel.SuspendLayout();
            SuspendLayout();
            // 
            // dataGridView1
            // 
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView1.ColumnHeadersHeight = 29;
            dataGridView1.Dock = DockStyle.Fill;
            dataGridView1.Location = new Point(0, 50);
            dataGridView1.Name = "dataGridView1";
            dataGridView1.RowHeadersWidth = 51;
            dataGridView1.Size = new Size(800, 400);
            dataGridView1.TabIndex = 0;
            dataGridView1.RowPrePaint += DataGridView1_RowPrePaint;
            dataGridView1.RowPostPaint += DataGridView1_RowPostPaint;

            // 
            // btnRunCheck
            // 
            btnRunCheck.Location = new Point(10, 10);
            btnRunCheck.Name = "btnRunCheck";
            btnRunCheck.Size = new Size(349, 30);
            btnRunCheck.TabIndex = 1;
            btnRunCheck.Text = "Run Check";
            btnRunCheck.Click += btnRunCheck_Click;
            // 
            // button1
            // 
            button1.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            button1.Location = new Point(415, 10);
            button1.Name = "button1";
            button1.Size = new Size(373, 30);
            button1.TabIndex = 2;
            button1.Text = "Exit";
            button1.Click += exitButton_Click;
            // 
            // buttonPanel
            // 
            buttonPanel.Controls.Add(btnRunCheck);
            buttonPanel.Controls.Add(button1);
            buttonPanel.Dock = DockStyle.Top;
            buttonPanel.Location = new Point(0, 0);
            buttonPanel.Name = "buttonPanel";
            buttonPanel.Size = new Size(800, 50);
            buttonPanel.TabIndex = 1;
            // 
            // TableCheckerForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(dataGridView1);
            Controls.Add(buttonPanel);
            KeyPreview = true;
            Name = "TableCheckerForm";
            Text = "Table verifier";
            WindowState = FormWindowState.Maximized;
            KeyDown += TableCheckerForm_KeyDown;
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            buttonPanel.ResumeLayout(false);
            ResumeLayout(false);
        }

        private Button button1;
        private Panel buttonPanel;

        #endregion
    }
}