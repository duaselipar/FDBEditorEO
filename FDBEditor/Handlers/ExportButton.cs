using System.Text;

namespace FDBEditorEO.Handlers
{
    public static class ExportButton
    {
        public static void Handle(DataGridView dataGridView1, string loadedFilePath)
        {
            if (dataGridView1.Rows.Count == 0)
            {
                MessageBox.Show("No data to export.");
                return;
            }

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
    }
}
