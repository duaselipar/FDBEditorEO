using FDBEditorEO.Models;

namespace FDBEditorEO.Handlers
{
    public static class DataGridEvents
    {
        public static void MouseMove(MouseEventArgs e, DataGridView dgv)
        {
            var hit = dgv.HitTest(e.X, e.Y);
            if (hit.Type == DataGridViewHitTestType.RowHeader && e.X <= dgv.RowHeadersWidth + 2)
            {
                Cursor.Current = Cursors.Default;
            }
        }

        public static void CellValueNeeded(DataGridViewCellValueEventArgs e, List<List<object>> fdbRows, int colCount)
        {
            if (fdbRows == null || e.RowIndex < 0 || e.RowIndex >= fdbRows.Count || e.ColumnIndex < 0 || e.ColumnIndex >= colCount)
                e.Value = "";
            else
                e.Value = fdbRows[e.RowIndex][e.ColumnIndex];
        }

        public static void CellValuePushed(DataGridViewCellValueEventArgs e, List<List<object>> fdbRows, int colCount)
        {
            if (fdbRows == null || e.RowIndex < 0 || e.RowIndex >= fdbRows.Count || e.ColumnIndex < 0 || e.ColumnIndex >= colCount)
                return;
            fdbRows[e.RowIndex][e.ColumnIndex] = e.Value;
        }

        public static void ColumnHeaderMouseClick(
            DataGridViewCellMouseEventArgs e,
            DataGridView dgv,
            List<FdbField> fdbFields,
            List<List<object>> fdbRows,
            ref int lastSortColumn,
            ref bool sortAscending)
        {
            if (fdbRows == null) return;
            int col = e.ColumnIndex;
            if (lastSortColumn == col)
                sortAscending = !sortAscending;
            else
                sortAscending = true;

            byte type = fdbFields[col].Type;

            Comparison<List<object>> comparer = (a, b) =>
            {
                object va = a[col] ?? "";
                object vb = b[col] ?? "";

                try
                {
                    switch (type)
                    {
                        case 1: return Comparer<byte>.Default.Compare(Convert.ToByte(va), Convert.ToByte(vb));
                        case 2: return Comparer<short>.Default.Compare(Convert.ToInt16(va), Convert.ToInt16(vb));
                        case 3: return Comparer<ushort>.Default.Compare(Convert.ToUInt16(va), Convert.ToUInt16(vb));
                        case 4: return Comparer<int>.Default.Compare(Convert.ToInt32(va), Convert.ToInt32(vb));
                        case 5: return Comparer<uint>.Default.Compare(Convert.ToUInt32(va), Convert.ToUInt32(vb));
                        case 6: return Comparer<float>.Default.Compare(Convert.ToSingle(va), Convert.ToSingle(vb));
                        case 7: return Comparer<double>.Default.Compare(Convert.ToDouble(va), Convert.ToDouble(vb));
                        case 8: return Comparer<long>.Default.Compare(Convert.ToInt64(va), Convert.ToInt64(vb));
                        case 9: return Comparer<ulong>.Default.Compare(Convert.ToUInt64(va), Convert.ToUInt64(vb));
                        default:
                            return string.Compare(va.ToString(), vb.ToString(), StringComparison.OrdinalIgnoreCase);
                    }
                }
                catch
                {
                    return string.Compare(va.ToString(), vb.ToString(), StringComparison.OrdinalIgnoreCase);
                }
            };

            if (sortAscending)
                fdbRows.Sort(comparer);
            else
                fdbRows.Sort((a, b) => comparer(b, a));

            lastSortColumn = col;
            dgv.Invalidate();
        }
    }
}
