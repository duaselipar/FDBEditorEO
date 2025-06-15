using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

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

        public MainEditor()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            InitializeComponent();


            dataGridView1.VirtualMode = true;
            dataGridView1.ReadOnly = false;
            dataGridView1.MultiSelect = true;
            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.AllowUserToDeleteRows = false;
            dataGridView1.AllowUserToOrderColumns = true;
            dataGridView1.AllowUserToResizeRows = true;
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;

            dataGridView1.CellValueNeeded += DataGridView1_CellValueNeeded;
            dataGridView1.CellValuePushed += DataGridView1_CellValuePushed;
            dataGridView1.ColumnHeaderMouseClick += DataGridView1_ColumnHeaderMouseClick;


            dataGridView1.RowHeadersWidth = 45;
            dataGridView1.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing;

            // Override event
            dataGridView1.MouseMove += (s, e) =>
            {
                var hit = dataGridView1.HitTest(e.X, e.Y);
                if (hit.Type == DataGridViewHitTestType.RowHeader && e.X <= dataGridView1.RowHeadersWidth + 2)
                {
                    Cursor.Current = Cursors.Default; // Paksa tukar cursor
                }
            };

            btnSave.Click += btnSave_Click;
            InitializeContextMenu();
            dataGridView1.ContextMenuStrip = contextMenu;

            this.KeyPreview = true;
            this.KeyDown += Form1_KeyDown;

            this.KeyDown += (s, e) => {
                if (e.Control && e.KeyCode == Keys.V)
                {
                    PasteRows_Click(null, null);
                    e.Handled = true;
                }
            };

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


        private void btnSearch_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSearch.Text)) return;
            string search = txtSearch.Text.Trim().ToLower();

            int colIdx = -1;
            string selectedCol = cmbSearchColumn.SelectedItem?.ToString();
            if (!string.IsNullOrEmpty(selectedCol) && selectedCol != "All")
            {
                colIdx = fdbFields.FindIndex(f => f.Name == selectedCol);
                if (colIdx < 0) return; // Kolum tak jumpa
            }

            int startRow = lastSearchRow + 1;
            bool found = false;

            // Tiada clear selection, hanya tukar current cell (lebih jimat RAM)
            for (int i = startRow; i < dataGridView1.Rows.Count; i++)
            {
                var row = dataGridView1.Rows[i];
                if (row.IsNewRow) continue;
                if (colIdx >= 0)
                {
                    var cell = row.Cells[colIdx];
                    if (cell.Value != null && cell.Value.ToString().ToLower().Contains(search))
                    {
                        dataGridView1.CurrentCell = cell;
                        dataGridView1.FirstDisplayedScrollingRowIndex = row.Index;
                        lastSearchRow = row.Index;
                        found = true;
                        return;
                    }
                }
                else
                {
                    foreach (DataGridViewCell cell in row.Cells)
                    {
                        if (cell.Value != null && cell.Value.ToString().ToLower().Contains(search))
                        {
                            dataGridView1.CurrentCell = cell;
                            dataGridView1.FirstDisplayedScrollingRowIndex = row.Index;
                            lastSearchRow = row.Index;
                            found = true;
                            return;
                        }
                    }
                }
            }

            // Wrap around (cari dari atas balik)
            if (!found && lastSearchRow != -1)
            {
                for (int i = 0; i <= lastSearchRow; i++)
                {
                    var row = dataGridView1.Rows[i];
                    if (row.IsNewRow) continue;
                    if (colIdx >= 0)
                    {
                        var cell = row.Cells[colIdx];
                        if (cell.Value != null && cell.Value.ToString().ToLower().Contains(search))
                        {
                            dataGridView1.CurrentCell = cell;
                            dataGridView1.FirstDisplayedScrollingRowIndex = row.Index;
                            lastSearchRow = row.Index;
                            return;
                        }
                    }
                    else
                    {
                        foreach (DataGridViewCell cell in row.Cells)
                        {
                            if (cell.Value != null && cell.Value.ToString().ToLower().Contains(search))
                            {
                                dataGridView1.CurrentCell = cell;
                                dataGridView1.FirstDisplayedScrollingRowIndex = row.Index;
                                lastSearchRow = row.Index;
                                return;
                            }
                        }
                    }
                }
            }

            MessageBox.Show("No more results!");
            lastSearchRow = -1;
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





        // OPTIONAL: Tekan Enter dalam txtSearch akan trigger search
        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            lastSearchRow = -1;
        }

        private void btnLoad_Click(object sender, EventArgs e)
        {
            UpdateControlState(false); // Sentiasa reset dahulu
            // Clean up
            if (fdbRows != null) fdbRows.Clear();
            if (fdbFields != null) fdbFields.Clear();
            fdbRows = null;
            fdbFields = null;
            originalHeader = null;

            dataGridView1.Columns.Clear();
            dataGridView1.RowCount = 0;
            dataGridView1.DataSource = null;

            // Kosongkan dropdown search column
            cmbSearchColumn.Items.Clear();
            cmbSearchColumn.Items.Add("All");
            cmbSearchColumn.SelectedIndex = 0;

            GC.Collect();
            GC.WaitForPendingFinalizers();

            var dlg = new OpenFileDialog
            {
                Filter = "FDB Files (*.fdb)|*.fdb|All Files (*.*)|*.*"
            };
            if (dlg.ShowDialog() != DialogResult.OK)
            {
                UpdateControlState(false);
                return;
            }

            loadedFilePath = dlg.FileName;
            string fileName = Path.GetFileName(loadedFilePath);

            try
            {
                (fdbFields, fdbRows, originalHeader) = FdbLoaderEPLStyle.Load(loadedFilePath);

                dataGridView1.Columns.Clear();
                foreach (var field in fdbFields)
                {
                    string name = string.IsNullOrWhiteSpace(field.Name) ? $"Field{fdbFields.IndexOf(field) + 1}" : field.Name;
                    dataGridView1.Columns.Add(name, name);
                }
                dataGridView1.RowCount = fdbRows.Count;

                // Update dropdown hanya bila loaded sukses
                cmbSearchColumn.Items.Clear();
                cmbSearchColumn.Items.Add("All");
                foreach (var field in fdbFields)
                    cmbSearchColumn.Items.Add(field.Name);
                cmbSearchColumn.SelectedIndex = 0;

                this.Text = $"FDB Editor ({fileName}) — {fdbRows.Count} records, {fdbFields.Count} fields";
                UpdateControlState(true); // Only enable controls lepas load OK

            }
            catch (IOException ex) when (ex.Message.Contains("because it is being used by another process"))
            {
                MessageBox.Show(
                    "Failed to load FDB:\n\nFile is currently used by another process.\n\n" +
                    "Please close the game/client FIRST before opening this FDB file.",
                    "File in Use",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning
                );
                loadedFilePath = "";
                originalHeader = null;
                UpdateControlState(false);
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load FDB:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                loadedFilePath = "";
                originalHeader = null;
                UpdateControlState(false);
                return;
            }
        }


        // ---- Virtual Mode Cell Events ----
        private void DataGridView1_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            if (fdbRows == null || e.RowIndex < 0 || e.RowIndex >= fdbRows.Count || e.ColumnIndex < 0 || e.ColumnIndex >= fdbFields.Count)
                e.Value = "";
            else
                e.Value = fdbRows[e.RowIndex][e.ColumnIndex];
        }
        private void DataGridView1_CellValuePushed(object sender, DataGridViewCellValueEventArgs e)
        {
            if (fdbRows == null || e.RowIndex < 0 || e.RowIndex >= fdbRows.Count || e.ColumnIndex < 0 || e.ColumnIndex >= fdbFields.Count)
                return;
            fdbRows[e.RowIndex][e.ColumnIndex] = e.Value;
        }

        private void DataGridView1_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (fdbRows == null) return;
            int col = e.ColumnIndex;
            if (lastSortColumn == col)
                sortAscending = !sortAscending;
            else
                sortAscending = true;

            // Ambil field type
            byte type = fdbFields[col].Type;

            Comparison<List<object>> comparer = (a, b) =>
            {
                object va = a[col];
                object vb = b[col];
                // Null to ""
                va = va ?? "";
                vb = vb ?? "";

                try
                {
                    switch (type)
                    {
                        case 1: // byte
                            return Comparer<byte>.Default.Compare(Convert.ToByte(va), Convert.ToByte(vb));
                        case 2: // short
                            return Comparer<short>.Default.Compare(Convert.ToInt16(va), Convert.ToInt16(vb));
                        case 3: // ushort
                            return Comparer<ushort>.Default.Compare(Convert.ToUInt16(va), Convert.ToUInt16(vb));
                        case 4: // int
                            return Comparer<int>.Default.Compare(Convert.ToInt32(va), Convert.ToInt32(vb));
                        case 5: // uint
                            return Comparer<uint>.Default.Compare(Convert.ToUInt32(va), Convert.ToUInt32(vb));
                        case 6: // float
                            return Comparer<float>.Default.Compare(Convert.ToSingle(va), Convert.ToSingle(vb));
                        case 7: // double
                            return Comparer<double>.Default.Compare(Convert.ToDouble(va), Convert.ToDouble(vb));
                        case 8: // long
                            return Comparer<long>.Default.Compare(Convert.ToInt64(va), Convert.ToInt64(vb));
                        case 9: // ulong
                            return Comparer<ulong>.Default.Compare(Convert.ToUInt64(va), Convert.ToUInt64(vb));
                        default:
                            // Compare as string
                            return string.Compare(va.ToString(), vb.ToString(), StringComparison.OrdinalIgnoreCase);
                    }
                }
                catch
                {
                    // If conversion fail, fallback string compare
                    return string.Compare(va.ToString(), vb.ToString(), StringComparison.OrdinalIgnoreCase);
                }
            };

            // Sorting
            if (sortAscending)
                fdbRows.Sort(comparer);
            else
                fdbRows.Sort((a, b) => comparer(b, a));

            lastSortColumn = col;
            dataGridView1.Invalidate();
        }


        // ---- Context Menu Delete (multi-select) ----
        private List<List<object>> clipboardRows = null;

        private void InitializeContextMenu()
        {
            contextMenu = new ContextMenuStrip();

            var addRowItem = new ToolStripMenuItem("Add New Row");
            addRowItem.ShortcutKeys = Keys.Control | Keys.N;
            addRowItem.ShowShortcutKeys = true;
            addRowItem.Click += AddNewRow_Click;
            contextMenu.Items.Add(addRowItem);

            var copyRowItem = new ToolStripMenuItem("Copy Row(s)");
            copyRowItem.ShortcutKeys = Keys.Control | Keys.C;
            copyRowItem.ShowShortcutKeys = true;
            copyRowItem.Click += CopySelectedRows_Click;
            contextMenu.Items.Add(copyRowItem);

            var pasteRowItem = new ToolStripMenuItem("Paste Row(s)");
            pasteRowItem.ShortcutKeys = Keys.Control | Keys.V;
            pasteRowItem.ShowShortcutKeys = true;
            pasteRowItem.Click += PasteRows_Click;
            contextMenu.Items.Add(pasteRowItem);

            var deleteItem = new ToolStripMenuItem("Delete Selected Row(s)");
            deleteItem.ShortcutKeys = Keys.Delete;
            deleteItem.ShowShortcutKeys = true;
            deleteItem.Click += DeleteSelectedRows_Click;
            contextMenu.Items.Add(deleteItem);

            var exportSelectedItem = new ToolStripMenuItem("Export Selected Row(s)...");
            exportSelectedItem.ShortcutKeys = Keys.Control | Keys.E;
            exportSelectedItem.ShowShortcutKeys = true;
            exportSelectedItem.Click += ExportSelectedRows_Click;
            contextMenu.Items.Add(exportSelectedItem);
        }


        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            // Add New Row: Ctrl+N
            if (e.Control && e.KeyCode == Keys.N)
            {
                AddNewRow_Click(sender, EventArgs.Empty);
                e.Handled = true;
            }
            // Copy: Ctrl+C
            else if (e.Control && e.KeyCode == Keys.C)
            {
                CopySelectedRows_Click(sender, EventArgs.Empty);
                e.Handled = true;
            }
            // Paste: Ctrl+V
            else if (e.Control && e.KeyCode == Keys.V)
            {
                PasteRows_Click(sender, EventArgs.Empty);
                e.Handled = true;
            }
            // Delete: Delete
            else if (e.KeyCode == Keys.Delete)
            {
                DeleteSelectedRows_Click(sender, EventArgs.Empty);
                e.Handled = true;
            }
            // Export Selected: Ctrl+E
            else if (e.Control && e.KeyCode == Keys.E)
            {
                ExportSelectedRows_Click(sender, EventArgs.Empty);
                e.Handled = true;
            }
        }


        // Add New Row
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

        // Copy Row(s)
        private void CopySelectedRows_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count == 0 || fdbRows == null) return;

            // === 1. Header (ikut export CSV) ===
            var sb = new StringBuilder();
            for (int i = 0; i < dataGridView1.ColumnCount; i++)
            {
                sb.Append("\"");
                sb.Append(dataGridView1.Columns[i].HeaderText.Replace("\"", "\"\""));
                sb.Append("\"");
                if (i < dataGridView1.ColumnCount - 1) sb.Append(",");
            }
            sb.AppendLine();

            // === 2. Data, setiap value wrap dengan quote ===
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
                    sb.Append(val.Replace("\"", "\"\"")); // Escape double quote
                    sb.Append("\"");
                    if (i < dataGridView1.ColumnCount - 1) sb.Append(",");
                }
                sb.AppendLine();
            }

            Clipboard.SetText(sb.ToString());
        }



        // Paste Row(s)
        private void PasteRows_Click(object sender, EventArgs e)
        {
            // Cuba dapatkan dari clipboard (kalau Clipboard ada text CSV)
            string clipboardText = Clipboard.GetText();
            if (string.IsNullOrWhiteSpace(clipboardText)) return;

            // Pecah line
            var lines = clipboardText
                .Replace("\r\n", "\n")
                .Replace('\r', '\n')
                .Split('\n')
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToArray();

            if (lines.Length == 0) return;

            // === Check header ===
            var firstLine = lines[0];
            var hasHeader = false;
            var headers = firstLine.Split(',').Select(h => h.Trim('\"')).ToList();

            // Kalau header sama, skip first line
            if (headers.Count == fdbFields.Count && headers.SequenceEqual(fdbFields.Select(f => f.Name)))
                hasHeader = true;

            int startLine = hasHeader ? 1 : 0;

            int rowsAdded = 0;

            for (int i = startLine; i < lines.Length; i++)
            {
                var row = ParseCsvLine(lines[i], fdbFields.Count);
                if (row == null) continue;
                // Masuk ke row baru
                fdbRows.Add(row);
                dataGridView1.RowCount = fdbRows.Count;
                rowsAdded++;
            }

            if (rowsAdded > 0)
            {
                dataGridView1.Invalidate();
                dataGridView1.ClearSelection();
                // Optional: Select new rows
                for (int i = fdbRows.Count - rowsAdded; i < fdbRows.Count; i++)
                    dataGridView1.Rows[i].Selected = true;
                dataGridView1.FirstDisplayedScrollingRowIndex = Math.Max(0, fdbRows.Count - 1);
            }
            MessageBox.Show($"Paste complete. {rowsAdded} row(s) added.");
        }

        // Helper untuk parse CSV line ke List<object>
        private List<object> ParseCsvLine(string line, int fieldCount)
        {
            if (string.IsNullOrWhiteSpace(line)) return null;
            var vals = new List<object>();
            int i = 0;
            bool inQuotes = false;
            var sb = new StringBuilder();
            foreach (char c in line)
            {
                if (c == '\"')
                {
                    inQuotes = !inQuotes;
                    continue;
                }
                if (c == ',' && !inQuotes)
                {
                    vals.Add(sb.ToString());
                    sb.Clear();
                    i++;
                }
                else
                {
                    sb.Append(c);
                }
            }
            vals.Add(sb.ToString());
            // Pad ke cukup field kalau perlu
            while (vals.Count < fieldCount) vals.Add("");
            if (vals.Count != fieldCount) return null; // Mismatch
            return vals;
        }


        // Delete Selected Row(s)
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

        // Export Selected Row(s) to CSV
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

            var lines = new List<string>();
            // Header
            lines.Add(string.Join(",", fdbFields.Select(f => $"\"{f.Name}\"")));
            // Data
            foreach (var row in selectedRows)
                lines.Add(string.Join(",", row.Select(val => $"\"{val?.ToString().Replace("\"", "\"\"")}\"")));

            File.WriteAllLines(dlg.FileName, lines, Encoding.UTF8);
            MessageBox.Show("Export completed.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }








        private void BtnAutoFit_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewColumn col in dataGridView1.Columns)
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
        }


        // ---- Save Function (ikut susunan grid semasa) ----
        private void btnSave_Click(object sender, EventArgs e)
        {
            if (fdbFields == null || dataGridView1.Rows.Count == 0)
            {
                MessageBox.Show("No data to save!", "Error");
                return;
            }

            var dlg = new SaveFileDialog
            {
                Filter = "FDB Files (*.fdb)|*.fdb|All Files (*.*)|*.*",
                FileName = Path.GetFileNameWithoutExtension(loadedFilePath) + ".fdb"
            };
            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                List<List<object>> rowsToSave = new();
                for (int i = 0; i < dataGridView1.Rows.Count; i++)
                {
                    var row = dataGridView1.Rows[i];
                    if (row.IsNewRow) continue; // skip new/empty rows
                    List<object> vals = new();
                    for (int j = 0; j < dataGridView1.Columns.Count; j++)
                        vals.Add(row.Cells[j].Value);
                    rowsToSave.Add(vals);
                }

                // Save ikut current grid order
                FdbLoaderEPLStyle.Save(dlg.FileName, fdbFields, rowsToSave, originalHeader);
                MessageBox.Show("Save completed.", "Success");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save FDB:\n{ex.Message}", "Error");
            }
        }


        private void btnExport_Click(object sender, EventArgs e)
        {
            if (dataGridView1.Rows.Count == 0) { MessageBox.Show("No data to export."); return; }

            var dlg = new SaveFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                FileName = Path.GetFileNameWithoutExtension(loadedFilePath) + ".csv"
            };
            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            var sb = new StringBuilder();

            // Export header
            for (int i = 0; i < dataGridView1.Columns.Count; i++)
            {
                sb.Append('"');
                sb.Append(dataGridView1.Columns[i].HeaderText.Replace("\"", "\"\"")); // Escape double quotes
                sb.Append('"');
                if (i < dataGridView1.Columns.Count - 1) sb.Append(",");
            }
            sb.AppendLine();

            // Export rows
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (row.IsNewRow) continue;
                for (int c = 0; c < dataGridView1.Columns.Count; c++)
                {
                    string val = row.Cells[c].Value?.ToString() ?? "";
                    val = val.Replace("\"", "\"\""); // Escape double quotes
                    sb.Append('"');
                    sb.Append(val);
                    sb.Append('"');
                    if (c < dataGridView1.Columns.Count - 1) sb.Append(",");
                }
                sb.AppendLine();
            }

            File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8);
            MessageBox.Show("Export completed.", "Success");
        }


        private void btnImport_Click(object sender, EventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*"
            };
            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            var lines = File.ReadAllLines(dlg.FileName);
            if (lines.Length < 2)
            {
                MessageBox.Show("CSV empty or header only.");
                return;
            }

            // Parse header
            var headers = SplitCsvLine(lines[0], -1);
            int fieldCount = headers.Length;
            if (fieldCount == 0)
            {
                MessageBox.Show("CSV header error.");
                return;
            }

            // -- Ambil field pertama (unique key, e.g. uID/ID/Key) --
            int keyColCsv = 0; // CSV: column 0 = unique key

            // Cari index dalam fdbFields untuk field pertama (maybe "uID" or lain)
            string keyFieldName = headers[0];
            int keyColFdb = fdbFields.FindIndex(f => f.Name == keyFieldName);
            if (keyColFdb < 0)
            {
                MessageBox.Show("Column " + keyFieldName + " not found in FDB table.");
                return;
            }

            // Kumpul semua key sedia ada (dalam FDB, ikut field pertama)
            var existingKeys = new HashSet<string>(fdbRows.Select(r => r[keyColFdb]?.ToString() ?? ""));

            int added = 0, skipped = 0, error = 0;

            for (int i = 1; i < lines.Length; i++)
            {
                var vals = SplitCsvLine(lines[i], fieldCount);
                if (vals.Length != fieldCount)
                {
                    error++;
                    continue; // row salah
                }
                string keyVal = vals[0];

                // Skip duplicate
                if (existingKeys.Contains(keyVal))
                {
                    skipped++;
                    continue;
                }

                // Mapping ikut FDB fields (nama kolum sama sahaja akan match)
                var newRow = new List<object>();
                foreach (var f in fdbFields)
                {
                    int idx = Array.FindIndex(headers, h => h.Equals(f.Name, StringComparison.OrdinalIgnoreCase));
                    newRow.Add(idx >= 0 ? vals[idx] : "");
                }
                fdbRows.Add(newRow);
                existingKeys.Add(keyVal);
                added++;
            }

            dataGridView1.RowCount = fdbRows.Count;
            dataGridView1.Refresh();

            MessageBox.Show($"Import completed!\nAdded: {added}\nSkipped: {skipped}\nError Row(s): {error}", "Import Info");
        }

        // Helper: split CSV, handle quoted value (basic)
        private string[] SplitCsvLine(string line, int expectCount)
        {
            var res = new List<string>();
            bool inQuotes = false;
            var sb = new StringBuilder();
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                    continue;
                }
                if (c == ',' && !inQuotes)
                {
                    res.Add(sb.ToString());
                    sb.Clear();
                }
                else
                {
                    sb.Append(c);
                }
            }
            res.Add(sb.ToString());
            if (expectCount > 0 && res.Count < expectCount)
            {
                // Add empty for missing
                while (res.Count < expectCount) res.Add("");
            }
            return res.ToArray();
        }






    }

    public class FdbField
    {
        public byte Type;    // 1-10
        public int Offset;   // offset from textbase for name (tak wajib)
        public string Name;  // field name
    }

    public static class FdbLoaderEPLStyle
    {
        public static (List<FdbField>, List<List<object>>, byte[]) Load(string path)
        {
            var data = File.ReadAllBytes(path);
            const int HEADER_SIZE = 0x20;

            // PATCH: Save original header
            byte[] header = new byte[HEADER_SIZE];
            Array.Copy(data, 0, header, 0, HEADER_SIZE);

            int fieldCount = BitConverter.ToInt32(data, 0x14);
            int rowCount = BitConverter.ToInt32(data, 0x18);
            int textLen = BitConverter.ToInt32(data, 0x1C);
            int textBase = data.Length - textLen;

            var gbk = Encoding.GetEncoding("GBK");
            List<string> labels = new();
            int ptr = textBase;
            for (int i = 0; i < fieldCount; i++)
            {
                int start = ptr;
                while (ptr < data.Length && data[ptr] != 0) ptr++;
                labels.Add(gbk.GetString(data, start, ptr - start));
                ptr++; // skip null
            }

            var fields = new List<FdbField>();
            for (int i = 0; i < fieldCount; i++)
            {
                int fieldOffset = HEADER_SIZE + i * 5;
                byte type = data[fieldOffset];
                fields.Add(new FdbField
                {
                    Type = type,
                    Name = labels[i]
                });
            }

            int ptrTableOffset = HEADER_SIZE + fieldCount * 5;
            var rowPtrs = new List<int>();
            for (int i = 0; i < rowCount; i++)
            {
                int recPos = ptrTableOffset + i * 8;
                int recPtr = BitConverter.ToInt32(data, recPos + 4);
                rowPtrs.Add(recPtr);
            }

            var rows = new List<List<object>>(rowCount);
            foreach (var rowPtr in rowPtrs)
            {
                if (rowPtr <= 0 || rowPtr == 0xD6000000)
                {
                    rows.Add(Enumerable.Repeat<object>("", fields.Count).ToList());
                    continue;
                }
                int pos = rowPtr;
                var values = new List<object>(fields.Count);
                for (int f = 0; f < fields.Count; f++)
                {
                    var field = fields[f];
                    object val = null;
                    switch (field.Type)
                    {
                        case 1: val = data[pos]; pos += 1; break;
                        case 2: val = BitConverter.ToInt16(data, pos); pos += 2; break;
                        case 3: val = (ushort)BitConverter.ToInt16(data, pos); pos += 2; break;
                        case 4: val = BitConverter.ToInt32(data, pos); pos += 4; break;
                        case 5: val = (uint)BitConverter.ToInt32(data, pos); pos += 4; break;
                        case 6: val = BitConverter.ToSingle(data, pos); pos += 4; break;
                        case 7: val = BitConverter.ToDouble(data, pos); pos += 8; break;
                        case 8: val = BitConverter.ToInt64(data, pos); pos += 8; break;
                        case 9: val = (ulong)BitConverter.ToInt64(data, pos); pos += 8; break;
                        case 10:
                            int strPtr = BitConverter.ToInt32(data, pos);
                            int strAddr = textBase + strPtr;
                            val = "";
                            if (strAddr >= 0 && strAddr < data.Length)
                            {
                                int end = strAddr;
                                while (end < data.Length && data[end] != 0) end++;
                                val = gbk.GetString(data, strAddr, end - strAddr);
                            }
                            pos += 4;
                            break;
                        default:
                            val = ""; break;
                    }
                    values.Add(val);
                }
                rows.Add(values);
            }
            return (fields, rows, header);
        }

        public static void Save(string path, List<FdbField> fields, List<List<object>> rows, byte[] header)
        {
            const int HEADER_SIZE = 0x20;
            var gbk = Encoding.GetEncoding("GBK");
            int fieldCount = fields.Count;
            int rowCount = rows.Count;

            // ===== 1. BUILD TEXT POOL =====
            List<byte> textBytes = new();
            Dictionary<string, int> stringPointerDict = new();
            foreach (var f in fields)
            {
                var raw = gbk.GetBytes(f.Name ?? "");
                textBytes.AddRange(raw);
                textBytes.Add(0);
            }
            foreach (var row in rows)
            {
                for (int f = 0; f < fields.Count; f++)
                {
                    if (fields[f].Type == 10)
                    {
                        string s = row[f]?.ToString() ?? "";
                        if (!stringPointerDict.ContainsKey(s))
                        {
                            stringPointerDict[s] = textBytes.Count;
                            var raw = gbk.GetBytes(s);
                            textBytes.AddRange(raw);
                            textBytes.Add(0);
                        }
                    }
                }
            }

            int textLen = textBytes.Count;
            int textBase = HEADER_SIZE + fieldCount * 5 + rowCount * 8;
            int estimatedRowBytes = rows.Count * fields.Count * 8 + 1024;
            byte[] outBuf = new byte[textBase + estimatedRowBytes + textLen];

            // === 3. Salin header asal ===
            Array.Copy(header, outBuf, HEADER_SIZE);

            BitConverter.GetBytes(fieldCount).CopyTo(outBuf, 0x14);
            BitConverter.GetBytes(rowCount).CopyTo(outBuf, 0x18);
            BitConverter.GetBytes(textLen).CopyTo(outBuf, 0x1C);

            // === 4. Field types ===
            for (int i = 0; i < fieldCount; i++)
            {
                int fieldOffset = HEADER_SIZE + i * 5;
                outBuf[fieldOffset] = fields[i].Type;
            }

            // === 5. Bina pointer table dan tulis row data ===
            int ptrTableOffset = HEADER_SIZE + fieldCount * 5;
            int rowDataBase = ptrTableOffset + rowCount * 8;
            int rowPtr = rowDataBase;

            for (int i = 0; i < rowCount; i++)
            {
                int ptrPos = ptrTableOffset + i * 8;

                // **Ambil ID dari kolum pertama row, default 0**
                int rowId = 0;
                if (!(rows[i].All(x => x == null || x.ToString() == "")))
                {
                    object idVal = rows[i][0];
                    if (idVal != null)
                    {
                        // safe conversion for numeric & string types
                        if (idVal is int) rowId = (int)idVal;
                        else int.TryParse(idVal.ToString(), out rowId);
                    }
                }
                BitConverter.GetBytes(rowId).CopyTo(outBuf, ptrPos); // +0: ID

                // --- Check if this row is "empty" (semua kosong/null) ---
                bool isEmpty = rows[i].All(x => x == null || x.ToString() == "");
                if (isEmpty)
                {
                    BitConverter.GetBytes(0).CopyTo(outBuf, ptrPos + 4); // +4: offset = 0 utk kosong
                    continue;
                }

                BitConverter.GetBytes(rowPtr).CopyTo(outBuf, ptrPos + 4); // +4: offset ke row data

                // === Tulis row data, ikut format asal ===
                for (int f = 0; f < fieldCount; f++)
                {
                    var field = fields[f];
                    object val = rows[i][f];
                    switch (field.Type)
                    {
                        case 1:
                            outBuf[rowPtr++] = Convert.ToByte(val ?? 0);
                            break;
                        case 2:
                            BitConverter.GetBytes(Convert.ToInt16(val ?? 0)).CopyTo(outBuf, rowPtr);
                            rowPtr += 2;
                            break;
                        case 3:
                            BitConverter.GetBytes(Convert.ToUInt16(val ?? 0)).CopyTo(outBuf, rowPtr);
                            rowPtr += 2;
                            break;
                        case 4:
                            BitConverter.GetBytes(Convert.ToInt32(val ?? 0)).CopyTo(outBuf, rowPtr);
                            rowPtr += 4;
                            break;
                        case 5:
                            BitConverter.GetBytes(Convert.ToUInt32(val ?? 0)).CopyTo(outBuf, rowPtr);
                            rowPtr += 4;
                            break;
                        case 6:
                            BitConverter.GetBytes(Convert.ToSingle(val ?? 0)).CopyTo(outBuf, rowPtr);
                            rowPtr += 4;
                            break;
                        case 7:
                            BitConverter.GetBytes(Convert.ToDouble(val ?? 0)).CopyTo(outBuf, rowPtr);
                            rowPtr += 8;
                            break;
                        case 8:
                            BitConverter.GetBytes(Convert.ToInt64(val ?? 0)).CopyTo(outBuf, rowPtr);
                            rowPtr += 8;
                            break;
                        case 9:
                            BitConverter.GetBytes(Convert.ToUInt64(val ?? 0)).CopyTo(outBuf, rowPtr);
                            rowPtr += 8;
                            break;
                        case 10:
                            string s = val?.ToString() ?? "";
                            int strPtr = stringPointerDict.ContainsKey(s) ? stringPointerDict[s] : 0;
                            BitConverter.GetBytes(strPtr).CopyTo(outBuf, rowPtr);
                            rowPtr += 4;
                            break;
                        default:
                            // fallback
                            BitConverter.GetBytes(0).CopyTo(outBuf, rowPtr);
                            rowPtr += 4;
                            break;
                    }
                }
            }

            // === 6. Sambung text pool ===
            Array.Copy(textBytes.ToArray(), 0, outBuf, rowPtr, textBytes.Count);
            int realTotalLen = rowPtr + textBytes.Count;

            // === 7. Simpan file, potong betul² size ===
            File.WriteAllBytes(path, outBuf.Take(realTotalLen).ToArray());
        }

















    }
}
