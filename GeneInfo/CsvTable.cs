using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneInfo
{
    public class CsvTable
    {
        public CsvColumn[] Columns { get; set; }
        public CsvRow[] Rows { get; set; }
        public bool HasHeader { get; set; }

        public CsvTable(CsvColumn[] columns, CsvRow[] rows, bool hasHeader)
        {
            Columns = columns;
            Rows = rows;
            HasHeader = hasHeader;
        }

        public CsvTable Merge(CsvTable other, bool combineColumns, Func<(CsvValue, CsvColumn), (CsvValue, CsvColumn), int, (CsvValue, CsvColumn)[]> mergeCallback, Func<object?, object?, object?> attachedDataMergeCallback)
        {
            List<CsvEntry> list = new(Math.Max(Rows.Length, other.Rows.Length));

            for (int i = 0; i < Math.Max(Rows.Length, other.Rows.Length); i++)
            {
                if ((HasHeader ? i + 1 : i) < Rows.Length && (other.HasHeader ? i + 1 : i) < other.Rows.Length) // merge
                {
                    var src = new CsvEntry(Rows[HasHeader ? i + 1 : i], Columns);
                    var otr = new CsvEntry(other.Rows[other.HasHeader ? i + 1 : i], other.Columns);

                    if (combineColumns) // use CsvEntry merge
                        list.Add(src.Merge(otr, mergeCallback, attachedDataMergeCallback));
                    else
                    {
                        var columns = src.Columns.Concat(otr.Columns).ToList();
                        var values = src.Row.Values.Concat(otr.Row.Values).ToList();
                        for (int j = columns.Count - 1; j >= 0; j--)
                        {
                            // distinct columns with same-column value merging
                            int first = columns.IndexOf(columns[j]);
                            if (first < j) // not first occurrence
                            {
                                // remove column and merge row with first occurrence
                                columns.RemoveAt(j);
                                var tmp = values[j];
                                values.RemoveAt(j);
                                values[first] = mergeCallback((values[first], columns[first]), (tmp, columns[first]), first)[0].Item1;
                            }
                        }
                        for (int j = 0; j < values.Count; j++)
                        {
                            // update to global column index
                            if (values[j].IsList)
                                values[j] = new CsvValue(values[j].Values, j, values[j].ColumnType);
                            else
                                values[j] = new CsvValue(values[j].Value, j, values[j].ColumnType);
                        }
                        list.Add(new CsvEntry(new CsvRow(i, values.ToArray()), columns.ToArray()));
                    }
                }
                else if ((HasHeader ? i + 1 : i) < Rows.Length) // copy source
                {
                    var src = new CsvEntry(Rows[HasHeader ? i + 1 : i], Columns);
                    list.Add(src);
                }
                else if ((other.HasHeader ? i + 1 : i) < other.Rows.Length) // copy other
                {
                    var otr = new CsvEntry(other.Rows[other.HasHeader ? i + 1 : i], other.Columns);
                    list.Add(otr);
                }
            }

            var header = new List<CsvColumn>();
            foreach (var entry in list)
            {
                foreach (var col in entry.Columns)
                {
                    if (!header.Contains(col))
                    {
                        header.Add(col);
                    }
                }
            }

            var csv = new CsvBuilder()
                .AddColumns(header.ToArray());
            foreach (var entry in list)
            {
                csv.AddToRow(entry.Row.Values);
                csv.PushRow();
            }

            return csv.ToTable();
        }

        public CsvTable Merge(CsvTable other, bool combineColumns)
        {
            return Merge(other, combineColumns,
                (a, b, i) =>
                {
                    if (!a.Item1.IsList && !b.Item1.IsList && a.Item1.Value.Equals(b.Item1.Value)) // no point in merging equal values
                    {
                        return new[] { a };
                    }

                    if (a.Item2.Type == b.Item2.Type && (a.Item2.Name?.Equals(b.Item2.Name) ?? false))
                    {
                        return new[] { (new CsvValue(a.Item1.Values.Concat(b.Item1.Values).Distinct().ToArray(), i, a.Item1.ColumnType), a.Item2) };
                    }
                    else
                    {
                        return new[]{(new CsvValue(a.Item1.Values.Concat(b.Item1.Values).Distinct().ToArray(), i, a.Item1.ColumnType),
                        new CsvColumn(
                            a.Item2.Name != null && b.Item2.Name != null ? a.Item2.Name + ";" + b.Item2.Name :
                            a.Item2.Name ?? b.Item2.Name ?? null, a.Item2.Type)) };
                    }
                },
                (a, b) => a);
        }
    }

    public class CsvEntry
    {
        public CsvRow Row { get; set; }
        public CsvColumn[] Columns { get; set; }

        public CsvEntry(CsvRow row, CsvColumn[] columns)
        {
            Row = row;
            Columns = columns;
        }

        public CsvTable ToTable(bool hasHeader)
        {
            if (hasHeader)
                return new CsvBuilder()
                    .AddColumns(Columns)
                    .AddRow(Row)
                    .ToTable();
            else
                return new CsvBuilder()
                    .AddRow(Row)
                    .ToTable();
        }

        public CsvEntry Merge(CsvEntry other, Func<(CsvValue, CsvColumn), (CsvValue, CsvColumn), int, (CsvValue, CsvColumn)[]> mergeCallback, Func<object?, object?, object?> attachedDataMergeCallback)
        {
            List<(CsvValue, CsvColumn)> values = new(Math.Max(Row.Values.Length, other.Row.Values.Length));

            int index = 0;
            for (int i = 0; i < Math.Max(Row.Values.Length, other.Row.Values.Length); i++)
            {
                if (i < Row.Values.Length && i < other.Row.Values.Length) // merge
                {
                    if (Row.Values[i].ColumnType == other.Row.Values[i].ColumnType) // same column type
                    {
                        var add = mergeCallback((Row.Values[i], Columns[i]), (other.Row.Values[i], other.Columns[i]), index);
                        values.AddRange(add);
                        index += add.Length - 1;
                    }
                    else
                    {
                        values.Add((new(Row.Values[i].Value, index, Row.Values[i].ColumnType), Columns[i])); // add source
                        index++;
                        values.Add((new(other.Row.Values[i].Value, index, other.Row.Values[i].ColumnType), other.Columns[i])); // add other
                    }
                }
                else if (i < Row.Values.Length) // copy source
                {
                    values.Add((new CsvValue(Row.Values[i].Values, index, Row.Values[i].ColumnType), i < Columns.Length ? Columns[i] : new CsvColumn(Row.Values[i].ColumnType)));
                }
                else if (i < other.Row.Values.Length) // copy other
                {
                    values.Add((new CsvValue(other.Row.Values[i].Values, index, other.Row.Values[i].ColumnType), i < other.Columns.Length ? other.Columns[i] : new CsvColumn(other.Row.Values[i].ColumnType)));
                }
                index++;
            }

            return new CsvEntry(new CsvRow(Row.Index, values.Select(p => p.Item1).ToArray())
            {
                AttachedData = attachedDataMergeCallback(Row.AttachedData, other.Row.AttachedData)
            }, values.Select(p => p.Item2).ToArray());
        }

        public CsvEntry Merge(CsvEntry other)
        {
            return Merge(other,
                (a, b, i) =>
                {
                    if (a.Item2.Type == b.Item2.Type && ((a.Item2.Name?.Equals(b.Item2.Name) ?? false) || (a.Item2.Name?.Split(';').Contains(b.Item2.Name) ?? false)))
                    {
                        return new[] { (new CsvValue(a.Item1.Values.Concat(b.Item1.Values).Distinct().ToArray(), i, a.Item1.ColumnType), a.Item2) };
                    }
                    else
                    {
                        /*return new[]{(new CsvValue(a.Item1.Values.Concat(b.Item1.Values).Distinct().ToArray(), i, a.Item1.ColumnType),
                        new CsvColumn(
                            a.Item2.Name != null && b.Item2.Name != null ? a.Item2.Name + ";" + b.Item2.Name :
                            a.Item2.Name ?? b.Item2.Name ?? null, a.Item2.Type))};*/
                        return new[] { (new CsvValue(a.Item1.Values, i, a.Item1.ColumnType, a.Item1.IsList, a.Item1.IsReduced), a.Item2) ,
                         (new CsvValue(b.Item1.Values, i+1, b.Item1.ColumnType, b.Item1.IsList, b.Item1.IsReduced), b.Item2) };
                    }
                },
                (a, b) => a);
        }
    }

    public class CsvRow
    {
        public int Index { get; set; }
        public CsvValue[] Values { get; set; }
        public object? AttachedData { get; set; }

        public CsvRow(int index, CsvValue[] values)
        {
            Index = index;
            Values = values;
        }

        /// <summary>
        /// <para>Merges two rows with same column types into lists of values. Attached data and index are not merged.</para>
        /// <para>If the column types are different - both are added, increasing the final column count.</para>
        /// </summary>
        /// <param name="other">Other row to merge with</param>
        /// <param name="mergeCallback">Callback to handle value merges (source, other, columnIndex) => mergedValue</param>
        /// <param name="attachedDataMergeCallback">Callback to handle attached data merges (source, other) => merged</param>
        public CsvRow Merge(CsvRow other, Func<CsvValue, CsvValue, int, CsvValue> mergeCallback, Func<object?, object?, object?> attachedDataMergeCallback)
        {
            List<CsvValue> values = new(Math.Max(Values.Length, other.Values.Length));
            int index = 0;
            for (int i = 0; i < Math.Max(Values.Length, other.Values.Length); i++)
            {
                if (i < Values.Length && i < other.Values.Length) // merge
                {
                    if (Values[i].ColumnType == other.Values[i].ColumnType) // same column type
                    {
                        values.Add(mergeCallback(Values[i], other.Values[i], index));
                    }
                    else
                    {
                        values.Add(new(Values[i].Value, index, Values[i].ColumnType)); // add source
                        index++;
                        values.Add(new(other.Values[i].Value, index, other.Values[i].ColumnType)); // add other
                    }
                }
                else if (i < Values.Length) // copy source
                {
                    values.Add(new CsvValue(Values[i].Values, index, Values[i].ColumnType));
                }
                else if (i < other.Values.Length) // copy other
                {
                    values.Add(new CsvValue(other.Values[i].Values, index, other.Values[i].ColumnType));
                }
                index++;
            }

            return new CsvRow(Index, values.ToArray())
            {
                AttachedData = attachedDataMergeCallback(AttachedData, other.AttachedData)
            };
        }

        public CsvRow Merge(CsvRow other)
        {
            return Merge(other,
                (a, b, i) =>
                {
                    if (!a.IsList && !b.IsList && a.Value.Equals(b.Value)) // no point in merging equal values
                    {
                        return a;
                    }

                    return new CsvValue(a.Values.Concat(b.Values).Distinct().ToArray(), i, a.ColumnType);
                },
                (a, b) => a);
        }
    }

    public struct CsvValue
    {
        public readonly string Value
        {
            get => Values[0]; set
            {
                Values[0] = value;
            }
        }
        public string[] Values { get; set; }
        public int ColumnIndex { get; set; }
        public CsvType ColumnType { get; set; }
        public bool IsList { get; }
        public bool IsReduced { get; }

        public CsvValue(string value, int columnIndex, CsvType columnType)
        {
            Values = new string[1] { value };
            ColumnIndex = columnIndex;
            ColumnType = columnType;
            IsList = false;
            IsReduced = false;
        }

        public CsvValue(string[] values, int columnIndex, CsvType columnType)
        {
            Values = values;
            ColumnIndex = columnIndex;
            ColumnType = columnType;
            IsList = true;
            IsReduced = false;
        }

        public CsvValue(string[] values, int columnIndex, CsvType columnType, bool isList, bool isReduced)
        {
            Values = values;
            ColumnIndex = columnIndex;
            ColumnType = columnType;
            IsList = isList;
            IsReduced = isReduced;
        }

        public readonly CsvValue WithList()
        {
            return new CsvValue(new string[1] { Value }, ColumnIndex, ColumnType);
        }

        public readonly bool ToBoolean()
        {
            if (CsvParser.TryParseBoolean(Value, out bool result))
                return result;
            else
                throw new InvalidCastException("Can't convert " + ColumnType + " to Boolean");
        }

        public readonly bool[] ToBooleanList()
        {
            if (!IsList) throw new InvalidCastException("Can't convert single value to list");

            bool[] result = new bool[Values.Length];

            for (int i = 0; i < result.Length; i++)
            {
                if (CsvParser.TryParseBoolean(Values[i], out bool tmp))
                    result[i] = tmp;
                else
                    throw new InvalidCastException("Can't convert " + ColumnType + " to Boolean");
            }

            return result;
        }

        public readonly long ToLong()
        {
            if (CsvParser.TryParseNumber(Value, out long result))
                return result;
            else
                throw new InvalidCastException("Can't convert " + ColumnType + " to Number");
        }

        public readonly long[] ToLongList()
        {
            if (!IsList) throw new InvalidCastException("Can't convert single value to list");

            long[] result = new long[Values.Length];

            for (int i = 0; i < result.Length; i++)
            {
                if (CsvParser.TryParseNumber(Values[i], out long tmp))
                    result[i] = tmp;
                else
                    throw new InvalidCastException("Can't convert " + ColumnType + " to Number");
            }

            return result;
        }

        public readonly double ToDouble()
        {
            if (CsvParser.TryParseDouble(Value, out double result))
                return result;
            else
                throw new InvalidCastException("Can't convert " + ColumnType + " to Double");
        }

        public readonly double[] ToDoubleList()
        {
            if (!IsList) throw new InvalidCastException("Can't convert single value to list");

            double[] result = new double[Values.Length];

            for (int i = 0; i < result.Length; i++)
            {
                if (CsvParser.TryParseDouble(Values[i], out double tmp))
                    result[i] = tmp;
                else
                    throw new InvalidCastException("Can't convert " + ColumnType + " to Double");
            }

            return result;
        }

        public readonly TimeOnly ToTime()
        {
            if (CsvParser.TryParseTime(Value, out TimeOnly result))
                return result;
            else
                throw new InvalidCastException("Can't convert " + ColumnType + " to Time");
        }

        public readonly TimeOnly[] ToTimeList()
        {
            if (!IsList) throw new InvalidCastException("Can't convert single value to list");

            TimeOnly[] result = new TimeOnly[Values.Length];

            for (int i = 0; i < result.Length; i++)
            {
                if (CsvParser.TryParseTime(Values[i], out TimeOnly tmp))
                    result[i] = tmp;
                else
                    throw new InvalidCastException("Can't convert " + ColumnType + " to Time");
            }

            return result;
        }

        public readonly DateOnly ToDate()
        {
            if (CsvParser.TryParseDate(Value, out DateOnly result))
                return result;
            else
                throw new InvalidCastException("Can't convert " + ColumnType + " to Date");
        }

        public readonly DateOnly[] ToDateList()
        {
            if (!IsList) throw new InvalidCastException("Can't convert single value to list");

            DateOnly[] result = new DateOnly[Values.Length];

            for (int i = 0; i < result.Length; i++)
            {
                if (CsvParser.TryParseDate(Values[i], out DateOnly tmp))
                    result[i] = tmp;
                else
                    throw new InvalidCastException("Can't convert " + ColumnType + " to Date");
            }

            return result;
        }

        public readonly DateTime ToDateTime()
        {
            if (CsvParser.TryParseTimestamp(Value, out DateTime result))
                return result;
            else
                throw new InvalidCastException("Can't convert " + ColumnType + " to Timestamp");
        }

        public readonly DateTime[] ToDateTimeList()
        {
            if (!IsList) throw new InvalidCastException("Can't convert single value to list");

            DateTime[] result = new DateTime[Values.Length];

            for (int i = 0; i < result.Length; i++)
            {
                if (CsvParser.TryParseTimestamp(Values[i], out DateTime tmp))
                    result[i] = tmp;
                else
                    throw new InvalidCastException("Can't convert " + ColumnType + " to Timestamp");
            }

            return result;
        }

        public override readonly string ToString()
        {
            return Value;
        }

        public readonly string[] ToStringList()
        {
            if (!IsList) throw new InvalidCastException("Can't convert single value to list");

            return Values;
        }

        public static implicit operator bool(CsvValue v) => v.ToBoolean();
        public static implicit operator long(CsvValue v) => v.ToLong();
        public static implicit operator double(CsvValue v) => v.ToDouble();
        public static implicit operator TimeOnly(CsvValue v) => v.ToTime();
        public static implicit operator DateOnly(CsvValue v) => v.ToDate();
        public static implicit operator DateTime(CsvValue v) => v.ToDateTime();
        public static implicit operator string(CsvValue v) => v.Value;

        public static implicit operator bool[](CsvValue v) => v.ToBooleanList();
        public static implicit operator long[](CsvValue v) => v.ToLongList();
        public static implicit operator double[](CsvValue v) => v.ToDoubleList();
        public static implicit operator TimeOnly[](CsvValue v) => v.ToTimeList();
        public static implicit operator DateOnly[](CsvValue v) => v.ToDateList();
        public static implicit operator DateTime[](CsvValue v) => v.ToDateTimeList();
        public static implicit operator string[](CsvValue v) => v.Values;
    }
}
