using System.Text;
using FDBEditorEO.Models;

namespace FDBEditorEO.Handlers
{
    public static class ImportButton
    {
        public static void Handle(
            List<FdbField> fdbFields,
            List<List<object>> fdbRows,
            DataGridView dataGridView1)
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

            var headers = SplitCsvLine(lines[0], -1);
            int fieldCount = headers.Length;
            if (fieldCount == 0)
            {
                MessageBox.Show("CSV header error.");
                return;
            }

            int keyColCsv = 0;
            string keyFieldName = headers[0];
            int keyColFdb = fdbFields.FindIndex(f => f.Name == keyFieldName);
            if (keyColFdb < 0)
            {
                MessageBox.Show("Column " + keyFieldName + " not found in FDB table.");
                return;
            }

            var existingKeys = new HashSet<string>(fdbRows.Select(r => r[keyColFdb]?.ToString() ?? ""));

            int added = 0, skipped = 0, error = 0;

            for (int i = 1; i < lines.Length; i++)
            {
                var vals = SplitCsvLine(lines[i], fieldCount);
                if (vals.Length != fieldCount)
                {
                    error++;
                    continue;
                }

                string keyVal = vals[0];
                if (existingKeys.Contains(keyVal))
                {
                    skipped++;
                    continue;
                }

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

        private static string[] SplitCsvLine(string line, int expectCount)
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
                while (res.Count < expectCount) res.Add("");
            }
            return res.ToArray();
        }
    }
}
