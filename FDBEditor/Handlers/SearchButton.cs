using FDBEditorEO.Models;

namespace FDBEditorEO.Handlers
{
    public static class SearchButton
    {
        public static void Handle(
            TextBox txtSearch,
            ComboBox cmbSearchColumn,
            List<FdbField> fdbFields,
            DataGridView dataGridView1,
            ref int lastSearchRow)
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
    }
}
