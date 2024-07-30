using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneInfo
{
    public class CsvBuilder
    {
        private bool hasHeader = false;
        private List<CsvColumn> columns = new();
        private List<CsvRow> rows = new();
        private List<CsvValue> currentRow = new();

        public CsvBuilder AddColumn(CsvType type)
        {
            columns.Add(new(type));
            return this;
        }

        public CsvBuilder AddColumn(string name, CsvType type)
        {
            hasHeader = true;
            columns.Add(new(name, type));
            return this;
        }

        public CsvBuilder AddColumns(int count, CsvType type)
        {
            hasHeader = true;
            for (int i = 0; i < count; i++)
                columns.Add(new(type));
            return this;
        }

        public CsvBuilder AddColumns(CsvColumn[] columns)
        {
            hasHeader = true;
            this.columns.AddRange(columns);
            return this;
        }

        public CsvBuilder SetHeader(bool hasHeader)
        {
            this.hasHeader = hasHeader;
            return this;
        }

        public CsvBuilder AddToRow(string value)
        {
            currentRow.Add(new CsvValue(value, currentRow.Count, CsvType.String));
            return this;
        }

        public CsvBuilder AddToRow(bool value)
        {
            currentRow.Add(new CsvValue(value ? "yes" : "no", currentRow.Count, CsvType.Boolean));
            return this;
        }

        public CsvBuilder AddToRow(long value)
        {
            currentRow.Add(new CsvValue(value.ToString(), currentRow.Count, CsvType.Number));
            return this;
        }

        public CsvBuilder AddToRow(double value)
        {
            var str = value.ToString();
            if(!str.Contains('.'))
                str += ".0"; // force decimal point
            currentRow.Add(new CsvValue(str, currentRow.Count, CsvType.Double));
            return this;
        }

        public CsvBuilder AddToRow(TimeOnly value)
        {
            currentRow.Add(new CsvValue(value.ToString(), currentRow.Count, CsvType.Time));
            return this;
        }

        public CsvBuilder AddToRow(DateOnly value)
        {
            currentRow.Add(new CsvValue(value.ToString(), currentRow.Count, CsvType.Date));
            return this;
        }

        public CsvBuilder AddToRow(DateTime value)
        {
            currentRow.Add(new CsvValue(value.ToString(), currentRow.Count, CsvType.Timestamp));
            return this;
        }

        public CsvBuilder AddToRow(CsvType type, string serializedValue)
        {
            currentRow.Add(new CsvValue(serializedValue, currentRow.Count, type));
            return this;
        }

        public CsvBuilder AddToRow((CsvType, string) serializedTuple)
        {
            return AddToRow(serializedTuple.Item1, serializedTuple.Item2);
        }

        public CsvBuilder AddToRow(CsvValue[] values)
        {
            int baseIndex = currentRow.Count;
            currentRow.AddRange(values.Select(v => v.IsList ? new CsvValue(v.Values, baseIndex + v.ColumnIndex, v.ColumnType) : new CsvValue(v.Value, baseIndex + v.ColumnIndex, v.ColumnType)));
            return this;
        }

        public CsvBuilder PushRow()
        {
            rows.Add(new CsvRow(rows.Count, currentRow.ToArray()));
            currentRow.Clear();
            return this;
        }

        public CsvBuilder AddRow(CsvRow row)
        {
            rows.Add(row);
            return this;
        }

        public CsvTable ToTable()
        {
            if(currentRow.Count > 0)
            {
                Logger.Warn("Building table with an unpushed row. Make sure to call PushRow if this is not the intended behavior.");
            }

            if (hasHeader)
            {
                // inject header row
                CsvValue[] headerValues = new CsvValue[columns.Count];
                for (int i = 0; i < columns.Count; i++)
                {
                    headerValues[i] = new(columns[i].Name ?? "column_" + i, i, CsvType.String);
                }
                rows.Insert(0, new CsvRow(0, headerValues));
            }
            return new CsvTable(columns.ToArray(), rows.ToArray(), hasHeader);
        }
    }
}
