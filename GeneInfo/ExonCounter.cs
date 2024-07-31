using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace GeneInfo
{
    public class ExonCounter : IModule
    {
        public string Name => "exon_counter";

        public string Description => "Counts the number of exons that start inside a domain of a transcript.";

        public string Usage => "exon_counter [path_to_transcript_list] [path_to_domain_list] [path_of_output_file.csv]";

        public string Example => "exon_counter transcripts.txt domains.txt exoncounts.csv";

        public static async Task<TranscriptExonCounts?> GetTranscriptCounts(string transcriptId, Func<string?, bool> domainMatch)
        {
            API.Transcript? transcript = await API.GetTranscript(transcriptId);
            if (transcript == null || transcript.Id == null)
                return null;

            int exonsInside = 0;

            if (transcript.Translation != null && transcript.Translation.Id != null && transcript.Exon != null && transcript.Exon.Length > 0)
            {
                var domains = await GetDomains(transcript.Translation.Id, domainMatch);
                if (domains != null)
                {
                    foreach (var exon in transcript.Exon)
                    {
                        foreach (var domain in domains)
                        {
                            bool inDomain = false;
                            for (int i = 0; i < domain.Start.Length; i++)
                            {
                                if (exon.Start >= domain.Start[i] && exon.Start <= domain.End[i])
                                {
                                    inDomain = true;
                                    break;
                                }
                            }

                            if (inDomain)
                            {
                                if (domain.Start.Length > 1)
                                {
                                    Logger.Info($"Detected exon inside domain with {domain.Start.Length} ranges");
                                }

                                exonsInside++;
                                break;
                            }
                        }
                    }
                }
            }

            return new TranscriptExonCounts(transcript.Id, exonsInside, transcript.Exon != null ? (transcript.Exon.Length - exonsInside) : 0);
        }

        public static async Task<GenomeDomain[]?> GetDomains(string translationId, Func<string?, bool> domainMatch)
        {
            API.Domain[]? domains = await API.GetSmartDomains(translationId);

            if (domains == null)
                return null;

            List<GenomeDomain> genomeDomains = new List<GenomeDomain>(domains.Length);

            API.MapEntry[]?[] results = new API.MapEntry[]?[domains.Length];
            await Parallel.ForAsync(0, results.Length, async (i, cancel) =>
            {
                if (domains[i].Id != null && domainMatch(domains[i].Description))
                {
                    int localStart = domains[i].Start;
                    int localEnd = domains[i].End;
                    results[i] = await API.MapTranslation(translationId, localStart, localEnd);
                }
            });

            int i = 0;
            foreach (var domain in domains)
            {
                if (domain.Id != null && domainMatch(domain.Description))
                {
                    int localStart = domain.Start;
                    int localEnd = domain.End;
                    API.MapEntry[]? map = results[i];
                    if (map != null)
                    {
                        int[] start = new int[map.Length];
                        int[] end = new int[map.Length];
                        for (int j = 0; j < map.Length; j++)
                        {
                            start[j] = map[j].Start;
                            end[j] = map[j].End;
                        }
                        genomeDomains.Add(new GenomeDomain(domain.Id, domain.InterPro, start, end, localStart, localEnd, domain.Description));
                    }
                }
                i++;
            }

            return genomeDomains.ToArray();
        }

        public async Task<bool> Run(string[] args)
        {
            if (args.Length < 3)
                return false;

            string transcriptListPath = args[0];
            string domainListPath = args[1];
            string outputPath = args[2];

            bool validationError = false;
            if (!File.Exists(transcriptListPath))
            {
                Logger.Error($"File '{transcriptListPath}' does not exist.");
                validationError = true;
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

            if (validationError)
                return true;

            Logger.MinLevel = Logger.LogLevel.Info;
            CsvTable domainList = CsvReader.ReadFile(domainListPath, ['\n', ','], 256, 2);
            CsvTable transcriptList = CsvReader.ReadFile(transcriptListPath, ['\n'], 256, 2);
            Logger.MinLevel = Logger.LogLevel.Trace;

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

            void BuildTable(TranscriptExonCounts?[] transcripts)
            {
                var builder = new CsvBuilder()
                .AddColumn("Transcript Ensembl ID", CsvType.String)
                .AddColumn("# of Exons inside domain", CsvType.Number)
                .AddColumn("# of Exons outside domain", CsvType.Number);

                foreach (var transcript in transcripts)
                {
                    if (transcript != null)
                    {
                        builder
                            .AddToRow(transcript.Id)
                            .AddToRow(transcript.NumberOfExonsInsideDomain)
                            .AddToRow(transcript.NumberOfExonsOutsideDomain)
                            .PushRow();
                    }
                }

                var table = builder.ToTable();

                if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                string filename = outputPath;
                Logger.Info($"Writing table to {filename}");
                CsvWriter.WriteToFile(filename, table, new CsvDialect(',', '"', '\\'), '\n');
            }

            bool IsTranscript(string transcript)
            {
                return transcript.StartsWith("ENS", StringComparison.InvariantCultureIgnoreCase);
            }

            var transcriptArr = transcriptList.Rows.Where(v => v.Values.Length > 0).Select(v => v.Values.FirstOrDefault(v =>
            {
                return IsTranscript(v.ToString());
            })).Where(v => !v.Equals(default(CsvValue))).Select(v => v.ToString()).ToArray();
            TranscriptExonCounts?[] results = new TranscriptExonCounts?[transcriptArr.Length];

            await Parallel.ForAsync(0, transcriptArr.Length, async (i, cancel) =>
            {
                var transcript = transcriptArr[i].Trim();
                results[i] = await GetTranscriptCounts(transcript, CheckDomain);
            });

            try
            {
                BuildTable(results);
            }
            catch (Exception e)
            {
                Logger.Error("Error writing file " + outputPath + ": " + e.ToString());
            }

            return true;
        }
    }

    public class GenomeDomain(string id, string? interPro, int[] start, int[] end, int localStart, int localEnd, string? description)
    {
        public string Id { get; set; } = id;
        public string? InterPro { get; set; } = interPro;
        public int[] Start { get; set; } = start;
        public int[] End { get; set; } = end;
        public int LocalStart { get; set; } = localStart;
        public int LocalEnd { get; set; } = localEnd;
        public string? Description { get; set; } = description;
    }

    public class TranscriptExonCounts(string id, int numberOfExonsInsideDomain, int numberOfExonsOutsideDomain)
    {
        public string Id { get; set; } = id;
        public int NumberOfExonsInsideDomain { get; set; } = numberOfExonsInsideDomain;
        public int NumberOfExonsOutsideDomain { get; set; } = numberOfExonsOutsideDomain;
    }
}
