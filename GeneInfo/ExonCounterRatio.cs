using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneInfo
{
    public class ExonCounterRatio : IModule
    {
        public string Name => "exon_counter_ratio";

        public string Description => "Appends an additional column to the output of the exon_counter module containing the ratio of exons inside and outside domain.";

        public string Usage => "exon_counter_ratio [path_to_exon_count_list or wildcard] [path_of_output_file.csv or wildcard (first * - name, second * - extension)]";

        public string[] Examples => [
            "exon_counter_ratio exoncounts.csv exoncounts.ratio.csv",
            "exon_counter_ratio tables/exoncounts/*.csv tables/exoncounts/ratios/*.*"
            ];

        public async Task<bool> Run(string[] args)
        {
            if (args.Length < 2)
                return false;

            string exonCountListPath = args[0];
            string outputPath = args[1];

            bool validationError = false;

            string[] exonCountListPaths = [];

            if (exonCountListPath.Contains('*') || exonCountListPath.Contains('?')) // wildcard
            {
                string? wildcardRoot = Path.GetDirectoryName(exonCountListPath);
                exonCountListPaths = Directory.GetFiles(wildcardRoot ?? "./", Path.GetFileName(exonCountListPath));
            }
            else
            {
                exonCountListPaths = [exonCountListPath];
                if (!File.Exists(exonCountListPath))
                {
                    Logger.Error($"File '{exonCountListPath}' does not exist.");
                    validationError = true;
                }
            }

            string? outputDir = Path.GetDirectoryName(outputPath);

            try
            {
                if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }
            }
            catch
            {
                Logger.Error($"Directory '{outputDir}' is not accessible.");
                validationError = true;
            }

            bool isOutputWildcard;

            if (outputPath.Contains('*')) // wildcard
            {
                isOutputWildcard = true;
            }
            else
            {
                isOutputWildcard = false;
                try
                {
                    if (!File.Exists(outputPath))
                    {
                        File.Create(outputPath).Dispose();
                    }
                }
                catch
                {
                    Logger.Error($"File '{outputPath}' is not accessible.");
                    validationError = true;
                }
            }

            if (validationError)
                return true;

            CsvTable[] exonCountLists = new CsvTable[exonCountListPaths.Length];

            for (int i = 0; i < exonCountListPaths.Length; i++)
            {
                Logger.Info("Reading table " + exonCountListPaths[i]);
                Logger.MinLevel = Logger.LogLevel.Info;
                exonCountLists[i] = CsvReader.ReadFile(exonCountListPaths[i], ['\n'], 256, 2);
                Logger.MinLevel = Logger.LogLevel.Trace;
                exonCountLists[i].Columns = exonCountLists[i].Columns.Concat([new CsvColumn("ratio", CsvType.Double)]).ToArray();
                exonCountLists[i].Rows[0].Values = exonCountLists[i].Rows[0].Values.Concat([new CsvValue("ratio", exonCountLists[i].Columns.Length - 1, CsvType.Double)]).ToArray();
                int insideColumnIndex = exonCountLists[i].Columns.ToImmutableList().FindIndex(c => c.Name?.Contains("exons inside domain", StringComparison.InvariantCultureIgnoreCase) ?? false);
                int outsideColumnIndex = exonCountLists[i].Columns.ToImmutableList().FindIndex(c => c.Name?.Contains("exons outside domain", StringComparison.InvariantCultureIgnoreCase) ?? false);
                if (insideColumnIndex == -1)
                {
                    Logger.Error("Table at " + exonCountListPaths[i] + " does not contain an exon inside domain column. skipping");
                    continue;
                }
                if (outsideColumnIndex == -1)
                {
                    Logger.Error("Table at " + exonCountListPaths[i] + " does not contain an exon outside domain column. skipping");
                    continue;
                }

                foreach (var row in exonCountLists[i].Rows[1..])
                {
                    long inside = row.Values[insideColumnIndex].ToLong();
                    long outside = row.Values[outsideColumnIndex].ToLong();
                    double ratio = outside == 0 ? 0 : ((double)inside / (double)outside);
                    row.Values = row.Values.Concat([new CsvValue(ratio.ToString(), exonCountLists[i].Columns.Length - 1, CsvType.Double)]).ToArray();
                }

                if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                string output;
                if (isOutputWildcard)
                {
                    output = IModule.ParseFileWildcard(outputDir, outputPath, exonCountListPaths[i]);
                }
                else
                {
                    output = outputPath;
                }

                Logger.Info("Writing output to " + output);
                CsvWriter.WriteToFile(output, exonCountLists[i], new CsvDialect(',', '"', '\\'), '\n');
            }

            await Task.Yield();

            return true;
        }
    }
}
