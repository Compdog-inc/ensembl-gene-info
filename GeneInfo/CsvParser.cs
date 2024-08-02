using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneInfo
{
    public enum CsvType
    {
        Boolean = 0,
        Number = 1,
        Double = 2,
        Time = 3,
        Date = 4,
        Timestamp = 5,
        String = 6
    }

    public static class CsvParser
    {
        public static bool IsQuotable(CsvType type)
        {
            switch (type)
            {
                case CsvType.Number:
                case CsvType.Double:
                    return false;
                case CsvType.Boolean:
                case CsvType.Time:
                case CsvType.Date:
                case CsvType.Timestamp:
                case CsvType.String:
                    return true;
            }
            return true;
        }

        public static bool TryParseBoolean(string content, out bool value)
        {
            if (
                content.Equals("true", StringComparison.InvariantCultureIgnoreCase) ||
                content.Equals("yes", StringComparison.InvariantCultureIgnoreCase)
                )
            {
                value = true;
                return true;
            }
            else if (
                content.Equals("false", StringComparison.InvariantCultureIgnoreCase) ||
                content.Equals("no", StringComparison.InvariantCultureIgnoreCase)
                )
            {
                value = false;
                return true;
            }
            value = false;
            return false;
        }

        public static bool TryParseNumber(string content, out long value)
        {
            if (!content.Contains('.'))
            {
                return long.TryParse(content, out value);
            }
            value = 0;
            return false;
        }

        public static bool TryParseDouble(string content, out double value)
        {
            if(content == "NaN")
            {
                value = double.NaN;
                return true;
            }

            if (content.Contains('.'))
            {
                return double.TryParse(content, out value);
            }
            value = double.NaN;
            return false;
        }

        public static bool TryParseTime(string content, out TimeOnly value)
        {
            return TimeOnly.TryParse(content, out value);
        }

        public static bool TryParseDate(string content, out DateOnly value)
        {
            return DateOnly.TryParse(content, out value);
        }

        public static bool TryParseTimestamp(string content, out DateTime value)
        {
            return DateTime.TryParse(content, out value);
        }

        public static CsvType DetectContentType(string content)
        {
            if (TryParseBoolean(content, out _))
                return CsvType.Boolean;
            if (TryParseNumber(content, out _))
                return CsvType.Number;
            if (TryParseDouble(content, out _))
                return CsvType.Double;
            if (TryParseTime(content, out _))
                return CsvType.Time;
            if (TryParseDate(content, out _))
                return CsvType.Date;
            if (TryParseTimestamp(content, out _))
                return CsvType.Timestamp;
            return CsvType.String; // anything can be a string
        }

        public static CsvType[] TryDetectColumnTypes(string[] sample, CsvDialect dialect, int safeRowCount)
        {
            List<CsvType>[]? possibleColumnTypes = null;

            for(int r=0;r<sample.Length; r++)
            {
                var columns = CsvTransformer.TransformRow(sample[r], dialect);
                if (possibleColumnTypes == null)
                    possibleColumnTypes = new List<CsvType>[columns.Length];
                else if (columns.Length != possibleColumnTypes.Length)
                {
                    Logger.Error($"TryDetectColumnTypes error: column mismatch {columns.Length} != {possibleColumnTypes.Length}");
                    if (r <= safeRowCount)
                    {
                        Logger.Warn("TryDetectColumnTypes failed on safe row. Possible header, resetting columns");
                        possibleColumnTypes = new List<CsvType>[columns.Length];
                    }
                    else
                    {
                        continue;
                    }
                }

                for (int i = 0; i < possibleColumnTypes.Length; i++)
                {
                    var type = DetectContentType(columns[i]);
                    if (possibleColumnTypes[i] == null)
                        possibleColumnTypes[i] = new List<CsvType>();
                    if (!possibleColumnTypes[i].Contains(type))
                        possibleColumnTypes[i].Add(type);
                }
            }

            if (possibleColumnTypes == null)
            {
                Logger.Error("TryDetectColumnTypes failed: possibleColumnTypes is null");
                return Array.Empty<CsvType>();
            }

            CsvType[] columnTypes = new CsvType[possibleColumnTypes.Length];
            for (int i = 0; i < columnTypes.Length; i++)
            {
                int org = (int)CsvType.String;
                foreach (var type in possibleColumnTypes[i])
                {
                    if ((int)type < org)
                        org = (int)type; // lowest takes priority
                }
                columnTypes[i] = (CsvType)org;
            }

            return columnTypes;
        }
    }
}
