namespace FDBEditor
{
    partial class MainEditor
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainEditor));
            btnLoad = new Button();
            dataGridView1 = new DataGridView();
            btnSave = new Button();
            btnAutoFit = new Button();
            btnExport = new Button();
            btnImport = new Button();
            txtSearch = new TextBox();
            btnSearch = new Button();
            cmbSearchColumn = new ComboBox();
            label1 = new Label();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            SuspendLayout();
            // 
            // btnLoad
            // 
            btnLoad.Location = new Point(12, 12);
            btnLoad.Name = "btnLoad";
            btnLoad.Size = new Size(120, 32);
            btnLoad.TabIndex = 0;
            btnLoad.Text = "Load FDB";
            btnLoad.UseVisualStyleBackColor = true;
            btnLoad.Click += btnLoad_Click;
            // 
            // dataGridView1
            // 
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.AllowUserToDeleteRows = false;
            dataGridView1.AllowUserToOrderColumns = true;
            dataGridView1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
            dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dataGridView1.Location = new Point(12, 54);
            dataGridView1.Name = "dataGridView1";
            dataGridView1.RowHeadersWidth = 45;
            dataGridView1.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            dataGridView1.RowTemplate.Height = 29;
            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView1.Size = new Size(1018, 707);
            dataGridView1.TabIndex = 1;
            dataGridView1.VirtualMode = true;
            dataGridView1.CellValueNeeded += DataGridView1_CellValueNeeded;
            dataGridView1.CellValuePushed += DataGridView1_CellValuePushed;
            dataGridView1.ColumnHeaderMouseClick += DataGridView1_ColumnHeaderMouseClick;
            dataGridView1.MouseMove += DataGridView1_MouseMove;
            // 
            // btnSave
            // 
            btnSave.Location = new Point(910, 12);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(120, 32);
            btnSave.TabIndex = 2;
            btnSave.Text = "Save";
            btnSave.UseVisualStyleBackColor = true;
            btnSave.Click += btnSave_Click;
            // 
            // btnAutoFit
            // 
            btnAutoFit.Location = new Point(138, 11);
            btnAutoFit.Name = "btnAutoFit";
            btnAutoFit.Size = new Size(120, 32);
            btnAutoFit.TabIndex = 3;
            btnAutoFit.Text = "Auto Fit Columns";
            btnAutoFit.UseVisualStyleBackColor = true;
            btnAutoFit.Click += BtnAutoFit_Click;
            // 
            // btnExport
            // 
            btnExport.Location = new Point(264, 12);
            btnExport.Name = "btnExport";
            btnExport.Size = new Size(102, 32);
            btnExport.TabIndex = 4;
            btnExport.Text = "Export All";
            btnExport.UseVisualStyleBackColor = true;
            btnExport.Click += btnExport_Click;
            // 
            // btnImport
            // 
            btnImport.Location = new Point(372, 12);
            btnImport.Name = "btnImport";
            btnImport.Size = new Size(101, 32);
            btnImport.TabIndex = 5;
            btnImport.Text = "Import";
            btnImport.UseVisualStyleBackColor = true;
            btnImport.Click += btnImport_Click;
            // 
            // txtSearch
            // 
            txtSearch.Location = new Point(665, 18);
            txtSearch.Name = "txtSearch";
            txtSearch.Size = new Size(147, 23);
            txtSearch.TabIndex = 6;
            txtSearch.KeyDown += txtSearch_KeyDown;
            // 
            // btnSearch
            // 
            btnSearch.Location = new Point(818, 17);
            btnSearch.Name = "btnSearch";
            btnSearch.Size = new Size(75, 23);
            btnSearch.TabIndex = 7;
            btnSearch.Text = "Search";
            btnSearch.UseVisualStyleBackColor = true;
            btnSearch.Click += btnSearch_Click;
            // 
            // cmbSearchColumn
            // 
            cmbSearchColumn.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbSearchColumn.FormattingEnabled = true;
            cmbSearchColumn.Items.AddRange(new object[] { "All" });
            cmbSearchColumn.Location = new Point(538, 18);
            cmbSearchColumn.Name = "cmbSearchColumn";
            cmbSearchColumn.Size = new Size(121, 23);
            cmbSearchColumn.TabIndex = 8;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(484, 22);
            label1.Name = "label1";
            label1.Size = new Size(48, 15);
            label1.TabIndex = 9;
            label1.Text = "Search :";
            // 
            // MainEditor
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1042, 773);
            Controls.Add(label1);
            Controls.Add(cmbSearchColumn);
            Controls.Add(btnSearch);
            Controls.Add(txtSearch);
            Controls.Add(btnImport);
            Controls.Add(btnExport);
            Controls.Add(btnAutoFit);
            Controls.Add(btnSave);
            Controls.Add(dataGridView1);
            Controls.Add(btnLoad);
            Icon = (Icon)resources.GetObject("$this.Icon");
            KeyPreview = true;
            Name = "MainEditor";
            Text = "FDB Editor by DuaSelipar";
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnLoad;
        private System.Windows.Forms.DataGridView dataGridView1;

        public Button btnSave, btnAutoFit, btnExport, btnImport, btnSearch;
        public ComboBox cmbSearchColumn;
        public TextBox txtSearch;
        private Label label1;
    }
}
