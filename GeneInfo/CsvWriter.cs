using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneInfo
{
    public static class CsvWriter
    {
        public static void WriteToTextWriter(TextWriter writer, CsvTable table, CsvDialect dialect, char rowDelimiter)
        {
            for (int j = 0; j < table.Rows.Length; j++)
            {
                writer.Write(CsvTransformer.FormatRow(table.Rows[j].Values.Select(v => string.Join(',', v.Values)).ToArray(), table.Columns.Select(v => v.Type).ToArray(), dialect));
                if (j < table.Rows.Length - 1)
                    writer.Write(rowDelimiter);
            }
        }

        public static void WriteToTextWriter(TextWriter writer, CsvTable[] tables, CsvDialect dialect, char rowDelimiter)
        {
            for (int i = 0; i < tables.Length; i++)
            {
                for (int j = 0; j < tables[i].Rows.Length; j++)
                {
                    writer.Write(CsvTransformer.FormatRow(tables[i].Rows[j].Values.Select(v => string.Join(',', v.Values)).ToArray(), tables[i].Columns.Select(v => v.Type).ToArray(), dialect));
                    if (j < tables[i].Rows.Length - 1)
                        writer.Write(rowDelimiter);
                }

                if (i < tables.Length - 1)
                    writer.Write("\n==================================================\n");
            }
        }

        public static void WriteToFile(string path, CsvTable table, CsvDialect dialect, char rowDelimiter)
        {
            using var fs = File.Create(path);
            using var writer = new StreamWriter(fs);
            WriteToTextWriter(writer, table, dialect, rowDelimiter);
        }

        public static void WriteToFile(string path, CsvTable[] tables, CsvDialect dialect, char rowDelimiter)
        {
            using var fs = File.Create(path);
            using var writer = new StreamWriter(fs);
            WriteToTextWriter(writer, tables, dialect, rowDelimiter);
        }

        public static string WriteToText(CsvTable table, CsvDialect dialect, char rowDelimiter)
        {
            using var writer = new StringWriter();
            WriteToTextWriter(writer, table, dialect, rowDelimiter);
            return writer.ToString();
        }

        public static string WriteToText(CsvTable[] tables, CsvDialect dialect, char rowDelimiter)
        {
            using var writer = new StringWriter();
            WriteToTextWriter(writer, tables, dialect, rowDelimiter);
            return writer.ToString();
        }
    }
}
