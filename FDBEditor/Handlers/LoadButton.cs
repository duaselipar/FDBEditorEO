using FDBEditorEO.Models;
using FDBEditorEO.Services;

namespace FDBEditorEO.Handlers
{
    public static class LoadButton
    {
        public static bool Handle(
            ref string loadedFilePath,
            ref byte[] originalHeader,
            ref List<FdbField> fdbFields,
            ref List<List<object>> fdbRows,
            DataGridView dataGridView1,
            ComboBox cmbSearchColumn,
            Form mainForm,
            Action<bool> UpdateControlState)
        {
            UpdateControlState(false);

            // Cleanup
            fdbRows?.Clear();
            fdbFields?.Clear();
            fdbRows = null;
            fdbFields = null;
            originalHeader = null;

            dataGridView1.Columns.Clear();
            dataGridView1.RowCount = 0;
            dataGridView1.DataSource = null;

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
                return false;
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

                cmbSearchColumn.Items.Clear();
                cmbSearchColumn.Items.Add("All");
                foreach (var field in fdbFields)
                    cmbSearchColumn.Items.Add(field.Name);
                cmbSearchColumn.SelectedIndex = 0;

                mainForm.Text = $"FDB Editor ({fileName}) — {fdbRows.Count} records, {fdbFields.Count} fields";
                UpdateControlState(true);
                return true;
            }
            catch (IOException ex) when (ex.Message.Contains("because it is being used by another process"))
            {
                MessageBox.Show(
                    "Failed to load FDB:\n\nFile is currently used by another process.\n\n" +
                    "Please close the game/client FIRST before opening this FDB file.",
                    "File in Use",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load FDB:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            loadedFilePath = "";
            originalHeader = null;
            UpdateControlState(false);
            return false;
        }
    }
}
