using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneInfo
{
    public static class Transcripts
    {
        private static bool IsDomainNear(int a, int b)
        {
            return Math.Abs(a - b) < 20;
        }

        public static async Task<TranscriptInfo[]?> GetTranscriptsInfo(string geneId)
        {
            API.Transcript[]? transcripts = await API.GetTranscripts(geneId);
            if (transcripts == null)
                return null;

            List<TranscriptInfo> infos = new List<TranscriptInfo>(transcripts.Length);

            foreach (var transcript in transcripts)
            {
                if (transcript.Id != null && transcript.BioType == "protein_coding" && transcript.Translation != null && transcript.Translation.Id != null)
                {
                    int exonCount = transcript.Exon == null ? 0 : transcript.Exon.Length;
                    API.Domain[]? domains = await API.GetSmartDomains(transcript.Translation.Id);
                    int numberOfDomains = 0;
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
                            }
                        }

                        if (overlappingDomains.Count > 0)
                        {
                            Logger.Warn("Removed " + overlappingDomains.Count + " overlapping domains");
                        }
                    }
                    infos.Add(new TranscriptInfo(transcript.Id, exonCount, numberOfDomains, uniqueDomains.ToArray()));
                }
            }

            return infos.ToArray();
        }

        public static async Task<TranscriptInfo[]> GetTranscriptsInfoWithOrthologs(string geneId)
        {
            Logger.Info("Requesting transcript info for gene '" + geneId + "'");

            List<TranscriptInfo> infos = [];
            infos.AddRangeNullable(await GetTranscriptsInfo(geneId));
            var orthologs = await API.GetOrthologs(geneId);
            if (orthologs != null)
            {
                Logger.Info("Found " + orthologs.Length + " orthologs (unfiltered)");
                foreach (var ortholog in orthologs)
                {
                    if (ortholog.Species == "cercocebus_atys" && ortholog.Id != null)
                    {
                        if (ortholog.Type == "ortholog_one2one")
                        {
                            infos.AddRangeNullable(await GetTranscriptsInfoWithOrthologs(ortholog.Id));
                        }
                        else if (ortholog.Type == "ortholog_one2many")
                        {
                            infos.AddRangeNullable(await GetTranscriptsInfoWithOrthologs(ortholog.Id));
                        }
                    }
                }
            }
            return infos.ToArray();
        }
    }

    public class TranscriptInfo(string id, int exonCount, int numberOfDomains, string[] uniqueDomains)
    {
        public string Id { get; set; } = id;
        public int ExonCount { get; set; } = exonCount;
        public int NumberOfDomains { get; set; } = numberOfDomains;
        public string[] UniqueDomains { get; set; } = uniqueDomains;
    }
}
