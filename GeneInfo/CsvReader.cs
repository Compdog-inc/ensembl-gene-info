using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneInfo
{
    public static class CsvReader
    {
        public static CsvTable ReadText(string text, char rowDelimiter, int sampleSize, int safeRowCount, params char[] columnDelimiters)
        {
            text = text.ReplaceLineEndings("\n");
            var raw_rows = CsvTransformer.TransformText(text, rowDelimiter);
            var sample = raw_rows[..Math.Min(raw_rows.Length, sampleSize)];
            Logger.Trace($"Detecting CSV with {raw_rows.Length} row(s) using {sample.Length} sample size.");

            CsvDialect dialect = CsvDialect.Detect(sample, safeRowCount, columnDelimiters);
            CsvColumn[] columns = CsvHeader.DetectHeader(sample, dialect, safeRowCount, out bool hasHeader);
            CsvRow[] rows = new CsvRow[raw_rows.Length];

            for (int i = 0; i < rows.Length; i++)
            {
                var values = CsvTransformer.TransformRow(raw_rows[i], dialect);
                CsvValue[] csvValues = new CsvValue[values.Length];

                for (int j = 0; j < columns.Length &&  j < values.Length; j++)
                {
                    csvValues[j] = new CsvValue(values[j], j, columns[j].Type);
                }

                rows[i] = new CsvRow(i, csvValues);
            }

            return new CsvTable(columns, rows, hasHeader);
        }

        public static CsvTable ReadFile(string path, char rowDelimiter, int sampleSize, int safeRowCount, params char[] columnDelimiters)
        {
            using FileStream fs = File.OpenRead(path);
            using StreamReader reader = new(fs);
            string text = reader.ReadToEnd();
            reader.Close();
            fs.Close();
            return ReadText(text, rowDelimiter, sampleSize, safeRowCount, columnDelimiters);
        }
    }
}
