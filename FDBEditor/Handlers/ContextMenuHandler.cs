using FDBEditorEO.Models;
using System.Text;

namespace FDBEditorEO.Handlers
{
    public class ContextMenuHandler
    {
        private readonly DataGridView dataGridView1;
        private readonly List<List<object>> fdbRows;
        private readonly List<FdbField> fdbFields;
        private readonly Form mainForm;
        private readonly ComboBox cmbSearchColumn;
        public ContextMenuStrip ContextMenu { get; private set; }

        public ContextMenuHandler(DataGridView dgv, List<List<object>> rows, List<FdbField> fields, Form form, ComboBox cmb)
        {
            dataGridView1 = dgv;
            fdbRows = rows;
            fdbFields = fields;
            mainForm = form;
            cmbSearchColumn = cmb;
            InitializeContextMenu();
        }

        private void InitializeContextMenu()
        {
            ContextMenu = new ContextMenuStrip();

            AddItem("Add New Row", Keys.Control | Keys.N, AddNewRow_Click);
            AddItem("Copy Row(s)", Keys.Control | Keys.C, CopySelectedRows_Click);
            AddItem("Paste Row(s)", Keys.Control | Keys.V, PasteRows_Click);
            AddItem("Delete Selected Row(s)", Keys.Delete, DeleteSelectedRows_Click);
            AddItem("Export Selected Row(s)...", Keys.Control | Keys.E, ExportSelectedRows_Click);
        }

        private void AddItem(string text, Keys shortcut, EventHandler handler)
        {
            var item = new ToolStripMenuItem(text)
            {
                ShortcutKeys = shortcut,
                ShowShortcutKeys = true
            };
            item.Click += handler;
            ContextMenu.Items.Add(item);
        }

        private void AddNewRow_Click(object sender, EventArgs e)
        {
            if (fdbFields == null) return;
            fdbRows.Add(new List<object>(new object[fdbFields.Count]));
            dataGridView1.RowCount = fdbRows.Count;
            dataGridView1.ClearSelection();
            int idx = fdbRows.Count - 1;
            dataGridView1.Rows[idx].Selected = true;
            dataGridView1.FirstDisplayedScrollingRowIndex = idx;
            dataGridView1.CurrentCell = dataGridView1.Rows[idx].Cells[0];
        }

        private void CopySelectedRows_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count == 0 || fdbRows == null) return;

            var sb = new StringBuilder();
            for (int i = 0; i < dataGridView1.ColumnCount; i++)
            {
                sb.Append("\"");
                sb.Append(dataGridView1.Columns[i].HeaderText.Replace("\"", "\"\""));
                sb.Append("\"");
                if (i < dataGridView1.ColumnCount - 1) sb.Append(",");
            }
            sb.AppendLine();

            var rows = dataGridView1.SelectedRows
                .Cast<DataGridViewRow>()
                .OrderBy(r => r.Index)
                .ToList();

            foreach (var row in rows)
            {
                for (int i = 0; i < dataGridView1.ColumnCount; i++)
                {
                    sb.Append("\"");
                    var val = row.Cells[i].Value?.ToString() ?? "";
                    sb.Append(val.Replace("\"", "\"\""));
                    sb.Append("\"");
                    if (i < dataGridView1.ColumnCount - 1) sb.Append(",");
                }
                sb.AppendLine();
            }

            Clipboard.SetText(sb.ToString());
        }

        private void PasteRows_Click(object sender, EventArgs e)
        {
            string clipboardText = Clipboard.GetText();
            if (string.IsNullOrWhiteSpace(clipboardText)) return;

            var lines = clipboardText
                .Replace("\r\n", "\n")
                .Replace('\r', '\n')
                .Split('\n')
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToArray();

            if (lines.Length == 0) return;

            var firstLine = lines[0];
            var hasHeader = false;
            var headers = firstLine.Split(',').Select(h => h.Trim('"')).ToList();

            if (headers.Count == fdbFields.Count && headers.SequenceEqual(fdbFields.Select(f => f.Name)))
                hasHeader = true;

            int startLine = hasHeader ? 1 : 0;
            int rowsAdded = 0;

            for (int i = startLine; i < lines.Length; i++)
            {
                var row = ParseCsvLine(lines[i], fdbFields.Count);
                if (row == null) continue;
                fdbRows.Add(row);
                dataGridView1.RowCount = fdbRows.Count;
                rowsAdded++;
            }

            if (rowsAdded > 0)
            {
                dataGridView1.Invalidate();
                dataGridView1.ClearSelection();
                for (int i = fdbRows.Count - rowsAdded; i < fdbRows.Count; i++)
                    dataGridView1.Rows[i].Selected = true;
                dataGridView1.FirstDisplayedScrollingRowIndex = Math.Max(0, fdbRows.Count - 1);
            }

            MessageBox.Show($"Paste complete. {rowsAdded} row(s) added.");
        }

        private void DeleteSelectedRows_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count == 0 || fdbRows == null) return;
            var indexes = dataGridView1.SelectedRows
                .Cast<DataGridViewRow>()
                .Where(r => !r.IsNewRow)
                .Select(r => r.Index)
                .OrderByDescending(i => i)
                .ToList();

            foreach (var idx in indexes)
            {
                if (idx >= 0 && idx < fdbRows.Count)
                    fdbRows.RemoveAt(idx);
            }

            dataGridView1.RowCount = fdbRows.Count;
            dataGridView1.ClearSelection();
            dataGridView1.Invalidate();
        }

        private void ExportSelectedRows_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count == 0 || fdbFields == null) return;

            var dlg = new SaveFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                FileName = "export.csv"
            };
            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            var selectedRows = dataGridView1.SelectedRows
                .Cast<DataGridViewRow>()
                .OrderBy(r => r.Index)
                .Select(r => fdbRows[r.Index])
                .ToList();

            var lines = new List<string>
            {
                string.Join(",", fdbFields.Select(f => $"\"{f.Name}\""))
            };

            foreach (var row in selectedRows)
                lines.Add(string.Join(",", row.Select(val => $"\"{val?.ToString().Replace("\"", "\"\"")}\"")));

            File.WriteAllLines(dlg.FileName, lines, Encoding.UTF8);
            MessageBox.Show("Export completed.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private List<object> ParseCsvLine(string line, int fieldCount)
        {
            if (string.IsNullOrWhiteSpace(line)) return null;

            var vals = new List<object>();
            bool inQuotes = false;
            var sb = new StringBuilder();

            foreach (char c in line)
            {
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                    continue;
                }
                if (c == ',' && !inQuotes)
                {
                    vals.Add(sb.ToString());
                    sb.Clear();
                }
                else
                {
                    sb.Append(c);
                }
            }
            vals.Add(sb.ToString());
            while (vals.Count < fieldCount) vals.Add("");
            return vals.Count == fieldCount ? vals : null;
        }

        public void HandleKeyDown(KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.N)
            {
                AddNewRow_Click(null, EventArgs.Empty);
                e.Handled = true;
            }
            else if (e.Control && e.KeyCode == Keys.C)
            {
                CopySelectedRows_Click(null, EventArgs.Empty);
                e.Handled = true;
            }
            else if (e.Control && e.KeyCode == Keys.V)
            {
                PasteRows_Click(null, EventArgs.Empty);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Delete)
            {
                DeleteSelectedRows_Click(null, EventArgs.Empty);
                e.Handled = true;
            }
            else if (e.Control && e.KeyCode == Keys.E)
            {
                ExportSelectedRows_Click(null, EventArgs.Empty);
                e.Handled = true;
            }
        }
    }
}