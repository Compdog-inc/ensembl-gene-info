using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneInfo
{
    public class DomainFilter : IModule
    {
        public string Name => "domain_filter";

        public string Description => "Filters the list of domains in a table and adds them to a new column.";

        public string Usage => "domain_filter [path_to_transcript_list or wildcard] [path_to_domain_list] [path_of_output_file.csv or wildcard (first * - name, second * - extension)]";

        public string[] Examples => [
            "domain_filter gene.csv domains.txt gene.csv",
            "domain_filter tables/*.csv domains.txt tables/*.*"
            ];

        public async Task<bool> Run(string[] args)
        {
            if (args.Length < 3)
                return false;

            string transcriptListPath = args[0];
            string domainListPath = args[1];
            string outputPath = args[2];

            bool validationError = false;

            string[] transcriptListPaths = [];

            if (transcriptListPath.Contains('*') || transcriptListPath.Contains('?')) // wildcard
            {
                string? wildcardRoot = Path.GetDirectoryName(transcriptListPath);
                transcriptListPaths = Directory.GetFiles(wildcardRoot ?? "./", Path.GetFileName(transcriptListPath));
            }
            else
            {
                transcriptListPaths = [transcriptListPath];
                if (!File.Exists(transcriptListPath))
                {
                    Logger.Error($"File '{transcriptListPath}' does not exist.");
                    validationError = true;
                }
            }

            if (!File.Exists(domainListPath))
            {
                Logger.Error($"File '{domainListPath}' does not exist.");
                validationError = true;
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

            Logger.MinLevel = Logger.LogLevel.Info;
            CsvTable domainList = CsvReader.ReadFile(domainListPath, ['\n', ','], 256, 2);
            Logger.MinLevel = Logger.LogLevel.Trace;

            CsvTable[] transcriptLists = new CsvTable[transcriptListPaths.Length];

            bool CheckDomain(string? domain)
            {
                if (domain == null) return false;

                foreach (var row in domainList.Rows)
                {
                    if (row.Values.Length > 0)
                    {
                        string[] vals = row.Values[0].ToString().Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).SelectMany(v => v.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)).ToArray();
                        foreach (var val in vals)
                        {
                            if (domain.Contains(val, StringComparison.InvariantCultureIgnoreCase))
                            {
                                return true;
                            }
                        }
                    }
                }

                return false;
            }

            for (int i = 0; i < transcriptListPaths.Length; i++)
            {
                Logger.Info("Reading table " + transcriptListPaths[i]);
                Logger.MinLevel = Logger.LogLevel.Info;
                transcriptLists[i] = CsvReader.ReadFile(transcriptListPaths[i], ['\n'], 256, 2);
                Logger.MinLevel = Logger.LogLevel.Trace;
                transcriptLists[i].Columns = transcriptLists[i].Columns.Concat([new CsvColumn("filtered domains", CsvType.String)]).ToArray();
                transcriptLists[i].Rows[0].Values = transcriptLists[i].Rows[0].Values.Concat([new CsvValue("filtered domains", transcriptLists[i].Columns.Length - 1, CsvType.String)]).ToArray();
                int domainColumnIndex = transcriptLists[i].Columns.ToImmutableList().FindIndex(c => c.Name?.Contains("list of domains", StringComparison.InvariantCultureIgnoreCase) ?? false);
                if (domainColumnIndex == -1)
                {
                    Logger.Error("Table at " + transcriptListPaths[i] + " does not contain any valid domain lists. skipping");
                    continue;
                }

                foreach (var row in transcriptLists[i].Rows[1..])
                {
                    string[] domains = row.Values[domainColumnIndex].ToString().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    row.Values = row.Values.Concat([new CsvValue(string.Join(',', domains.Where(CheckDomain)), transcriptLists[i].Columns.Length - 1, CsvType.String)]).ToArray();
                }

                if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                string output;
                if (isOutputWildcard)
                {
                    output = IModule.ParseFileWildcard(outputDir, outputPath, transcriptListPaths[i]);
                }
                else
                {
                    output = outputPath;
                }

                Logger.Info("Writing output to " + output);
                CsvWriter.WriteToFile(output, transcriptLists[i], new CsvDialect(',', '"', '\\'), '\n');
            }

            await Task.Yield();

            return true;
        }
    }
}
