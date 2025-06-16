using FDBEditorEO.Models;
using FDBEditorEO.Services;

namespace FDBEditorEO.Handlers
{
    public static class SaveButton
    {
        public static void Handle(
            List<FdbField> fdbFields,
            List<List<object>> fdbRows,
            string loadedFilePath,
            byte[] originalHeader,
            DataGridView dataGridView1)
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
                    if (row.IsNewRow) continue;
                    List<object> vals = new();
                    for (int j = 0; j < dataGridView1.Columns.Count; j++)
                        vals.Add(row.Cells[j].Value);
                    rowsToSave.Add(vals);
                }

                FdbLoaderEPLStyle.Save(dlg.FileName, fdbFields, rowsToSave, originalHeader);
                MessageBox.Show("Save completed.", "Success");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save FDB:\n{ex.Message}", "Error");
            }
        }
    }
}
