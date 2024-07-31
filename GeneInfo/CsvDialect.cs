using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneInfo
{
    public class CsvDialect
    {
        public static readonly char[] PossibleDelimiters = new[] { '|', ',', ';', '\t' };
        public static readonly char?[] PossibleQuotes = new char?[] { '"', '\'', null };
        public static readonly char?[] PossiblesEscapes = new char?[] { '"', '\'', '\\', null };

        public char Delimiter { get; set; }
        public char? Quote { get; set; }
        public char? Escape { get; set; }

        public CsvDialect(char delimiter, char? quote, char? escape)
        {
            Delimiter = delimiter;
            Quote = quote;
            Escape = escape;
        }

        public static string[] ApproximateColumnSplit(string row, char delimiter)
        {
            List<string> columns = [];

            StringBuilder sb = new();
            bool inQuote = false;
            for (int i = 0; i < row.Length; i++)
            {
                if (row[i] == '"' || row[i] == '\'')
                {
                    inQuote = !inQuote;
                    sb.Append(row[i]);
                }
                else if (row[i] == delimiter && !inQuote)
                {
                    columns.Add(sb.ToString());
                    sb.Clear();
                }
                else
                {
                    sb.Append(row[i]);
                }
            }

            if (sb.Length > 0)
                columns.Add(sb.ToString());

            return columns.ToArray();
        }

        public static bool TryParseDelimiter(string[] sample, char delimiter, out int columnCount, int safeRowCount)
        {
            Logger.Trace($"Trying to parse delimiter {delimiter}");
            columnCount = -1;

            for (int i = 0; i < sample.Length; i++)
            {
                int tmp = ApproximateColumnSplit(sample[i], delimiter).Length;
                if (i == 0)
                {
                    Logger.Trace($"First row has {tmp} column(s)");
                    columnCount = tmp;
                }
                else if (tmp != columnCount) // column count not consistent
                {
                    Logger.Error($"TryParseDelimiter failed: {tmp} != {columnCount} @ {sample[i]}");
                    if (i <= safeRowCount)
                    {
                        Logger.Warn("TryParseDelimiter failed on safe row. Possible header, resetting columns");
                        columnCount = tmp;
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public static bool TryParseQuote(string[] sample, char delimiter, char? quote)
        {
            Logger.Trace($"Trying to parse quote {quote}");
            if (quote == null) return true; // always a possibility
            for (int i = 0; i < sample.Length; i++)
            {
                var columns = ApproximateColumnSplit(sample[i], delimiter);
                bool bad = false;
                foreach (var column in columns)
                {
                    if (column.Length < 2 && (column.Length == 0 || !char.IsDigit(column[0])))
                    {
                        Logger.Trace($"TryParseQuote failed: invalid column {column}");
                        bad = true;
                        break;
                    }

                    if (column[0] != quote && !char.IsDigit(column[0]))
                    {
                        Logger.Trace($"TryParseQuote failed: invalid first char {column[0]}");
                        bad = true;
                        break;
                    }

                    if (column[^1] != quote && !char.IsDigit(column[0]))
                    {
                        Logger.Trace($"TryParseQuote failed: invalid last char {column[^1]}");
                        bad = true;
                        break;
                    }
                }

                if (bad)
                {
                    if (i == 0)
                    {
                        Logger.Warn("TryParseQuote failed on first row. Possible header, ignoring");
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            // all rows and columns passed
            return true;
        }

        public static bool TryParseEscape(string[] sample, char delimiter, char? quote, char? escape)
        {
            Logger.Trace($"Trying to parse escape {escape}");
            if (escape == null) return true; // always a possibility
            foreach (var row in sample)
            {
                var columns = ApproximateColumnSplit(row, delimiter);
                foreach (var column in columns)
                {
                    string content;
                    if (quote == null || (column.Length > 0 && column[0] != quote) || (column.Length > 0 && column[^1] != quote))
                        content = column;
                    else
                        content = column[1..^1];

                    if (content.Length > 0)
                    {
                        char definitelyNot = content[0]; // first char cannot be escaped
                        for (int i = 1; i < content.Length; i++)
                        {
                            if (
                                // escapable characters
                                content[i] == 't' ||
                                content[i] == 'b' ||
                                content[i] == 'n' ||
                                content[i] == 'r' ||
                                content[i] == 'f' ||
                                content[i] == 's' ||
                                content[i] == '\'' ||
                                content[i] == '"' ||
                                content[i] == '\\' ||
                                content[i] == quote // can't use quotes without escape
                                )
                            {
                                char possibleEscape = content[i - 1];
                                if (possibleEscape != definitelyNot && possibleEscape == escape)
                                    return true;
                            }
                        }
                    }
                }
            }

            // no escape found
            return false;
        }

        /// <summary>
        /// Tries to detect dialect from csv sample
        /// </summary>
        public static CsvDialect Detect(string[] sample, int safeRowCount, params char[] delimiters)
        {
            // delimiter detection
            var delims = delimiters.Concat(PossibleDelimiters).Distinct().ToArray();
            Logger.Trace($"Checking for [{string.Join(',', delims)}]");

            var tries = delims.Select(d =>
            {
                bool possible = TryParseDelimiter(sample, d, out int columnCount, safeRowCount);
                return (possible, columnCount);
            }).ToArray();

            char delimiter = ',';
            int columnCount = -1;
            for (int i = 0; i < tries.Length; i++)
            {
                if (tries[i].possible)
                {
                    // get delimiter with biggest column count
                    if (tries[i].columnCount > columnCount)
                    {
                        columnCount = tries[i].columnCount;
                        delimiter = delims[i];
                    }
                }
            }

            Logger.Debug($"Detected delimiter {delimiter} with {columnCount} column(s)");

            // quote detection
            Logger.Trace($"Checking for [{string.Join(',', PossibleQuotes)}]");
            var quoteTries = PossibleQuotes.Select(q =>
            {
                bool possible = TryParseQuote(sample, delimiter, q);
                return possible;
            }).ToArray();

            char? quote = null;
            for (int i = 0; i < quoteTries.Length; i++)
            {
                if (quoteTries[i])
                {
                    quote = PossibleQuotes[i];
                    break; // order important
                }
            }

            Logger.Debug($"Detected quote {quote}");

            // escape detection
            Logger.Trace($"Checking for [{string.Join(',', PossiblesEscapes)}]");
            var escapeTries = PossiblesEscapes.Select(e =>
            {
                bool possible = TryParseEscape(sample, delimiter, quote, e);
                return possible;
            }).ToArray();

            char? escape = null;
            for (int i = 0; i < escapeTries.Length; i++)
            {
                if (escapeTries[i])
                {
                    escape = PossiblesEscapes[i];
                    break; // order important
                }
            }

            Logger.Debug($"Detected escape {escape}");

            return new CsvDialect(delimiter, quote, escape);
        }
    }
}
