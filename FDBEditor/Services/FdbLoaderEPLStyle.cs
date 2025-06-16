using System.Text;
using FDBEditorEO.Models;

namespace FDBEditorEO.Services
{
    public static class FdbLoaderEPLStyle
    {
        public static (List<FdbField>, List<List<object>>, byte[]) Load(string path)
        {
            var data = File.ReadAllBytes(path);
            const int HEADER_SIZE = 0x20;

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
                ptr++;
            }

            var fields = new List<FdbField>();
            for (int i = 0; i < fieldCount; i++)
            {
                int fieldOffset = HEADER_SIZE + i * 5;
                byte type = data[fieldOffset];
                fields.Add(new FdbField { Type = type, Name = labels[i] });
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
                        default: val = ""; break;
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

            Array.Copy(header, outBuf, HEADER_SIZE);
            BitConverter.GetBytes(fieldCount).CopyTo(outBuf, 0x14);
            BitConverter.GetBytes(rowCount).CopyTo(outBuf, 0x18);
            BitConverter.GetBytes(textLen).CopyTo(outBuf, 0x1C);

            for (int i = 0; i < fieldCount; i++)
            {
                int fieldOffset = HEADER_SIZE + i * 5;
                outBuf[fieldOffset] = fields[i].Type;
            }

            int ptrTableOffset = HEADER_SIZE + fieldCount * 5;
            int rowDataBase = ptrTableOffset + rowCount * 8;
            int rowPtr = rowDataBase;

            for (int i = 0; i < rowCount; i++)
            {
                int ptrPos = ptrTableOffset + i * 8;
                int rowId = 0;
                if (!(rows[i].All(x => x == null || x.ToString() == "")))
                {
                    object idVal = rows[i][0];
                    if (idVal != null && int.TryParse(idVal.ToString(), out int parsedId))
                        rowId = parsedId;
                }
                BitConverter.GetBytes(rowId).CopyTo(outBuf, ptrPos);

                bool isEmpty = rows[i].All(x => x == null || x.ToString() == "");
                if (isEmpty)
                {
                    BitConverter.GetBytes(0).CopyTo(outBuf, ptrPos + 4);
                    continue;
                }

                BitConverter.GetBytes(rowPtr).CopyTo(outBuf, ptrPos + 4);

                for (int f = 0; f < fieldCount; f++)
                {
                    var field = fields[f];
                    object val = rows[i][f];
                    switch (field.Type)
                    {
                        case 1: outBuf[rowPtr++] = Convert.ToByte(val ?? 0); break;
                        case 2: BitConverter.GetBytes(Convert.ToInt16(val ?? 0)).CopyTo(outBuf, rowPtr); rowPtr += 2; break;
                        case 3: BitConverter.GetBytes(Convert.ToUInt16(val ?? 0)).CopyTo(outBuf, rowPtr); rowPtr += 2; break;
                        case 4: BitConverter.GetBytes(Convert.ToInt32(val ?? 0)).CopyTo(outBuf, rowPtr); rowPtr += 4; break;
                        case 5: BitConverter.GetBytes(Convert.ToUInt32(val ?? 0)).CopyTo(outBuf, rowPtr); rowPtr += 4; break;
                        case 6: BitConverter.GetBytes(Convert.ToSingle(val ?? 0)).CopyTo(outBuf, rowPtr); rowPtr += 4; break;
                        case 7: BitConverter.GetBytes(Convert.ToDouble(val ?? 0)).CopyTo(outBuf, rowPtr); rowPtr += 8; break;
                        case 8: BitConverter.GetBytes(Convert.ToInt64(val ?? 0)).CopyTo(outBuf, rowPtr); rowPtr += 8; break;
                        case 9: BitConverter.GetBytes(Convert.ToUInt64(val ?? 0)).CopyTo(outBuf, rowPtr); rowPtr += 8; break;
                        case 10:
                            string s = val?.ToString() ?? "";
                            int strPtr = stringPointerDict.ContainsKey(s) ? stringPointerDict[s] : 0;
                            BitConverter.GetBytes(strPtr).CopyTo(outBuf, rowPtr);
                            rowPtr += 4;
                            break;
                        default:
                            BitConverter.GetBytes(0).CopyTo(outBuf, rowPtr);
                            rowPtr += 4;
                            break;
                    }
                }
            }

            Array.Copy(textBytes.ToArray(), 0, outBuf, rowPtr, textBytes.Count);
            int realTotalLen = rowPtr + textBytes.Count;
            File.WriteAllBytes(path, outBuf.Take(realTotalLen).ToArray());
        }
    }
}
