using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GeneInfo
{
    public static class Transcripts
    {
        private static bool IsDomainNear(int a, int b)
        {
            return Math.Abs(a - b) < 20;
        }

        private static string FormatDomainType(string type)
        {
            if (type.Equals("ortholog_one2one", StringComparison.InvariantCultureIgnoreCase))
            {
                return "1:1 ortholog";
            }
            else if (type.Equals("ortholog_one2many", StringComparison.InvariantCultureIgnoreCase))
            {
                return "1:many ortholog";
            }

            return type;
        }

        private static string FormatSpecies(string species)
        {
            species = species.Replace('_', ' ');
            species = char.ToUpperInvariant(species[0]) + species[1..]; // capitalize first letter
            return species;
        }

        public static async Task<GeneInfo?> GetGeneInfo(string gene, bool isSymbol, string species, string type, Func<string?, bool> domainMatch)
        {
            API.Gene? geneObj = await (isSymbol ? API.GetGeneWithSymbol(gene) : API.GetGene(gene));
            if (geneObj == null)
                return null;

            API.Transcript[]? transcripts = geneObj.Transcript;
            if (transcripts == null)
                return null;

            List<TranscriptInfo> infos = new List<TranscriptInfo>(transcripts.Length);

            API.Domain[]?[] requests = new API.Domain[]?[transcripts.Length];
            await Parallel.ForAsync(0, requests.Length, async (i, cancel) =>
            {
                var transcript = transcripts[i];
                if (transcript.Id != null && transcript.BioType == "protein_coding" && transcript.Translation != null && transcript.Translation.Id != null)
                {
                    requests[i] = await API.GetSmartDomains(transcript.Translation.Id);
                }
            });

            int i = 0;
            foreach (var transcript in transcripts)
            {
                if (transcript.Id != null && transcript.BioType == "protein_coding" && transcript.Translation != null && transcript.Translation.Id != null)
                {
                    int exonCount = transcript.Exon == null ? 0 : transcript.Exon.Length;
                    API.Domain[]? domains = requests[i];
                    int numberOfDomains = 0;
                    int numberOfMatchedDomains = 0;
                    List<string> uniqueDomains = new(domains?.Length ?? 0);
                    if (domains != null)
                    {
                        // Find overlapping domains and count domains
                        Dictionary<API.Domain, List<API.Domain>> overlappingDomains = [];
                        foreach (var domain in domains)
                        {
                            bool counted = true;
                            foreach (var domain2 in domains)
                            {
                                if (domain != domain2 &&
                                    IsDomainNear(domain.Start, domain2.Start) && IsDomainNear(domain.End, domain2.End))
                                {
                                    if (overlappingDomains.TryGetValue(domain, out List<API.Domain>? value))
                                    {
                                        value.Add(domain2);
                                    }
                                    else if (overlappingDomains.TryGetValue(domain2, out List<API.Domain>? value2))
                                    {
                                        counted = false;
                                        value2.Add(domain);
                                    }
                                    else
                                    {
                                        overlappingDomains.Add(domain, [domain2]);
                                        Logger.Info("Detected overlapped domain '" + domain.Id + "|" + domain2.Id + "'");
                                    }
                                }
                            }

                            if (counted)
                            {
                                numberOfDomains++;
                                if (domain.Description != null && !uniqueDomains.Contains(domain.Description))
                                {
                                    uniqueDomains.Add(domain.Description);
                                }
                                if (domainMatch(domain.Description))
                                {
                                    numberOfMatchedDomains++;
                                }
                            }
                        }

                        if (overlappingDomains.Count > 0)
                        {
                            Logger.Warn("Removed " + overlappingDomains.Count + " overlapping domains");
                        }
                    }
                    infos.Add(new TranscriptInfo(transcript.Id, exonCount, numberOfDomains, numberOfMatchedDomains, uniqueDomains.ToArray(), FormatSpecies(species), FormatDomainType(type)));
                }
                i++;
            }

            return new(isSymbol ? (geneObj.Id ?? gene) : gene, geneObj.DisplayName ?? gene, infos.ToArray());
        }

        public static async Task<(GeneInfo?, TranscriptInfo[])> GetTranscriptsInfoWithOrthologs(string gene, bool isSymbol, Func<string?, bool> speciesMatch, Func<string?, bool> domainMatch, string? species, string? type)
        {
            Logger.Info("Requesting transcript info for gene '" + gene + "'");

            List<TranscriptInfo> infos = [];

            GeneInfo? geneInfo = null;
            if (species != null)
            {
                geneInfo = await GetGeneInfo(gene, isSymbol, species, type ?? "unknown", domainMatch);
                infos.AddRangeNullable(geneInfo?.Transcripts);
            }

            var orthologs = geneInfo == null ? (await API.GetOrthologs(gene)) : (await API.GetOrthologs(geneInfo.Id));
            if (orthologs != null)
            {
                Logger.Info("Found " + orthologs.Length + " orthologs (unfiltered)");

                TranscriptInfo[]?[] requests = new TranscriptInfo[]?[orthologs.Length];
                await Parallel.ForAsync(0, requests.Length, async (i, cancel) =>
                {
                    var ortholog = orthologs[i];
                    if (ortholog.Target != null && ortholog.Target.Id != null && speciesMatch(ortholog.Target.Species))
                    {
                        if (ortholog.Type == "ortholog_one2one")
                        {
                            requests[i] = (await GetTranscriptsInfoWithOrthologs(ortholog.Target.Id, false, speciesMatch, domainMatch, ortholog.Target.Species, ortholog.Type)).Item2;
                        }
                    }
                });

                Dictionary<string, (int index, List<API.Homology> list)> one2ManyList = [];
                int i = 0;
                foreach (var ortholog in orthologs)
                {
                    if (ortholog.Target != null && ortholog.Target.Id != null && speciesMatch(ortholog.Target.Species))
                    {
                        if (ortholog.Type == "ortholog_one2one")
                        {
                            infos.AddRangeNullable(requests[i]);
                        }
                        else if (ortholog.Type == "ortholog_one2many")
                        {
                            if (ortholog.Target.Species != null && one2ManyList.TryGetValue(ortholog.Target.Species, out var list))
                            {
                                list.list.Add(ortholog);
                            }
                            else if (ortholog.Target.Species != null)
                            {
                                one2ManyList.Add(ortholog.Target.Species, (infos.Count, [ortholog]));
                            }
                        }
                    }
                    i++;
                }

                if (one2ManyList.Count > 0)
                {
                    Logger.Info("Found " + one2ManyList.Count + " 1-to-many orthologs");
                }

                var values = one2ManyList.Values.ToArray();
                TranscriptInfo[]?[] bestRequests = new TranscriptInfo[]?[values.Length];
                await Parallel.ForAsync(0, bestRequests.Length, async (i, cancel) =>
                {
                    (int index, var list) = values[i];
                    var best = list.OrderByDescending((homology) => (homology.Source?.PercentId ?? 0) + (homology.Target?.PercentId ?? 0)).FirstOrDefault();
                    if (best != null && best.Target != null && best.Target.Id != null)
                    {
                        bestRequests[i] = (await GetTranscriptsInfoWithOrthologs(best.Target.Id, false, speciesMatch, domainMatch, best.Target.Species, best.Type)).Item2;
                    }
                });

                int indexOffset = 0;
                i = 0;
                foreach ((int index, var list) in one2ManyList.Values)
                {
                    var best = list.OrderByDescending((homology) => (homology.Source?.PercentId ?? 0) + (homology.Target?.PercentId ?? 0)).FirstOrDefault();
                    if (best != null && best.Target != null && best.Target.Id != null)
                    {
                        var ret = bestRequests[i];
                        if (ret != null)
                        {
                            infos.InsertRange(index + indexOffset, ret);
                            indexOffset += ret.Length;
                        }
                    }
                    i++;
                }
            }
            return (geneInfo, infos.ToArray());
        }
    }

    public class TranscriptInfo(string id, int exonCount, int numberOfDomains, int numberOfMatchedDomains, string[] uniqueDomains, string species, string type)
    {
        public string Id { get; set; } = id;
        public int ExonCount { get; set; } = exonCount;
        public int NumberOfDomains { get; set; } = numberOfDomains;
        public int NumberOfMatchedDomains { get; set; } = numberOfMatchedDomains;
        public string[] UniqueDomains { get; set; } = uniqueDomains;
        public string Species { get; set; } = species;
        public string Type { get; set; } = type;
    }

    public class GeneInfo(string id, string displayName, TranscriptInfo[] transcripts)
    {
        public string Id { get; set; } = id;
        public string DisplayName { get; set; } = displayName;
        public TranscriptInfo[] Transcripts { get; set; } = transcripts;
    }
}
