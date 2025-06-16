using System.Text;
using FDBEditorEO.Models;
using FDBEditorEO.Handlers;

namespace FDBEditor
{
    public partial class MainEditor : Form
    {
        private List<FdbField> fdbFields;
        private List<List<object>> fdbRows;
        private string loadedFilePath = "";
        private ContextMenuStrip contextMenu;
        private bool sortAscending = true;
        private int lastSortColumn = -1;
        private byte[] originalHeader = null; // HEADER PATCH

        private int lastSearchRow = -1;
        private ContextMenuHandler contextMenuHandler;

        public MainEditor()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            InitializeComponent();

            // Init context menu
            contextMenuHandler = new ContextMenuHandler(dataGridView1, fdbRows, fdbFields, this, cmbSearchColumn);
            contextMenu = contextMenuHandler.ContextMenu;
            dataGridView1.ContextMenuStrip = contextMenu;

            this.KeyPreview = true;
            this.KeyDown += (s, e) => contextMenuHandler.HandleKeyDown(e);

            UpdateControlState(false); // Mula-mula semua disable
        }

        private void UpdateControlState(bool fileLoaded)
        {
            btnSave.Enabled = fileLoaded;
            btnAutoFit.Enabled = fileLoaded;
            btnExport.Enabled = fileLoaded;
            btnImport.Enabled = fileLoaded;
            cmbSearchColumn.Enabled = fileLoaded;
            txtSearch.Enabled = fileLoaded;
            btnSearch.Enabled = fileLoaded;
            dataGridView1.Enabled = fileLoaded;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            SaveButton.Handle(fdbFields, fdbRows, loadedFilePath, originalHeader, dataGridView1);
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            SearchButton.Handle(txtSearch, cmbSearchColumn, fdbFields, dataGridView1, ref lastSearchRow);
        }

        private void txtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                btnSearch_Click(null, null);
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            lastSearchRow = -1;
        }

        private void btnLoad_Click(object sender, EventArgs e)
        {
            LoadButton.Handle(
                ref loadedFilePath,
                ref originalHeader,
                ref fdbFields,
                ref fdbRows,
                dataGridView1,
                cmbSearchColumn,
                this,
                UpdateControlState
            );

            // Reinitialize context menu with new data
            contextMenuHandler = new ContextMenuHandler(dataGridView1, fdbRows, fdbFields, this, cmbSearchColumn);
            dataGridView1.ContextMenuStrip = contextMenuHandler.ContextMenu;
        }

        private void BtnAutoFit_Click(object sender, EventArgs e)
        {
            AutoFitButton.Handle(dataGridView1);
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            ExportButton.Handle(dataGridView1, loadedFilePath);
        }

        private void btnImport_Click(object sender, EventArgs e)
        {
            ImportButton.Handle(fdbFields, fdbRows, dataGridView1);
        }

        private void DataGridView1_MouseMove(object sender, MouseEventArgs e)
        {
            DataGridEvents.MouseMove(e, dataGridView1);
        }

        private void DataGridView1_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            DataGridEvents.CellValueNeeded(e, fdbRows, fdbFields?.Count ?? 0);
        }

        private void DataGridView1_CellValuePushed(object sender, DataGridViewCellValueEventArgs e)
        {
            DataGridEvents.CellValuePushed(e, fdbRows, fdbFields?.Count ?? 0);
        }

        private void DataGridView1_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            DataGridEvents.ColumnHeaderMouseClick(
                e,
                dataGridView1,
                fdbFields,
                fdbRows,
                ref lastSortColumn,
                ref sortAscending
            );
        }
    }
}