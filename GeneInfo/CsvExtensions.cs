using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace GeneInfo
{
    public static class CsvExtensions
    {
        /// <summary>
        /// Serializes object into one of the following standard types:
        /// bool, long, double, TimeOnly, DateOnly, DateTime, string
        /// </summary>
        /// <param name="obj">If one of the supported types, cast to standard type. Otherwise, type is set to string and value is obj.ToString()</param>
        /// <returns>Serialized object to string with its type</returns>
        public static (CsvType, string) SerializeObject(object? obj)
        {
            if (obj is bool valB)
            {
                return (CsvType.Boolean, ((bool)valB) ? "yes" : "no");
            }
            else if (obj is int valI)
            {
                return (CsvType.Number, ((long)valI).ToString());
            }
            else if (obj is long valL)
            {
                return (CsvType.Number, ((long)valL).ToString());
            }
            else if (obj is short valS)
            {
                return (CsvType.Number, ((long)valS).ToString());
            }
            else if (obj is byte valBB)
            {
                return (CsvType.Number, ((long)valBB).ToString());
            }
            else if (obj is ushort valUS)
            {
                return (CsvType.Number, ((long)valUS).ToString());
            }
            else if (obj is uint valUI)
            {
                return (CsvType.Number, ((long)valUI).ToString());
            }
            else if (obj is ulong valUL)
            {
                return (CsvType.Number, ((long)valUL).ToString());
            }
            else if (obj is double valD)
            {
                var str = ((double)valD).ToString();
                if (!str.Contains('.'))
                    str += ".0"; // force decimal point
                return (CsvType.Double, str);
            }
            else if (obj is float valF)
            {
                var str = ((double)valF).ToString();
                if (!str.Contains('.'))
                    str += ".0"; // force decimal point
                return (CsvType.Double, str);
            }
            else if (obj is TimeOnly valT)
            {
                return (CsvType.Time, ((TimeOnly)valT).ToString());
            }
            else if (obj is DateOnly valDT)
            {
                return (CsvType.Date, ((DateOnly)valDT).ToString());
            }
            else if (obj is DateTime valDD)
            {
                return (CsvType.Timestamp, ((DateTime)valDD).ToString());
            }
            else if (obj is DateTimeOffset valDDO)
            {
                return (CsvType.Timestamp, ((DateTime)valDDO.UtcDateTime).ToString());
            }
            else if (obj is string valSTR)
            {
                return (CsvType.String, (string)valSTR);
            }
            else if (obj is char valCH)
            {
                return (CsvType.String, (string)(valCH + ""));
            }
            else if (obj is null)
            {
                return (CsvType.String, "null");
            }
            else
            {
                return (CsvType.String, obj.ToString() ?? "null");
            }
        }

        public static CsvTable ToTable<TKey, TValue>(this Dictionary<TKey, TValue> dictionary) where TKey : notnull
        {
            var csv = new CsvBuilder();

            foreach (var pair in dictionary)
            {
                csv.AddToRow(SerializeObject(pair.Key));
                csv.AddToRow(SerializeObject(pair.Value));
                csv.PushRow();
            }

            return csv.ToTable();
        }

        public static CsvTable ToTable<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, string keyName, string valueName) where TKey : notnull
        {
            var csv = new CsvBuilder()
                .AddColumn(keyName, CsvType.String)
                .AddColumn(valueName, CsvType.String);

            foreach (var pair in dictionary)
            {
                csv.AddToRow(SerializeObject(pair.Key));
                csv.AddToRow(SerializeObject(pair.Value));
                csv.PushRow();
            }

            return csv.ToTable();
        }

        public static CsvTable ToTable<T>(this IList<T> list)
        {
            var csv = new CsvBuilder();

            foreach (var item in list)
            {
                csv.AddToRow(SerializeObject(item));
                csv.PushRow();
            }

            return csv.ToTable();
        }

        public static CsvTable ToTable<T>(this IList<T> list, string columnName)
        {
            var csv = new CsvBuilder()
                .AddColumn(columnName, CsvType.String);

            foreach (var item in list)
            {
                csv.AddToRow(SerializeObject(item));
                csv.PushRow();
            }

            return csv.ToTable();
        }
    }
}
