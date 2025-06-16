namespace FDBEditorEO.Handlers
{
    public static class AutoFitButton
    {
        public static void Handle(DataGridView dataGridView)
        {
            foreach (DataGridViewColumn col in dataGridView.Columns)
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
        }
    }
}
