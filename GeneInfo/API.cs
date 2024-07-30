using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static GeneInfo.API;

namespace GeneInfo
{
    internal static class API
    {
        internal const string HOST = "https://rest.ensembl.org";
        private static HttpClient http = new();

        internal static async Task<Transcript[]?> GetTranscripts(string geneId)
        {
            string url = HOST + $"/lookup/id/{geneId}?expand=1;content-type=application/json";
            Logger.Trace("GetTranscripts: GET " + url);
            Gene? gene = await http.GetFromJsonAsync<Gene>(url, SourceGenerationContext.Default.Gene);
            return gene?.Transcript;
        }

        internal static async Task<Domain[]?> GetSmartDomains(string translationId)
        {
            List<Domain> domainList = new List<Domain>();
            string url = HOST + $"/overlap/translation/{translationId}?type=Smart&content-type=application/json";
            Logger.Trace("GetSmartDomains: GET " + url);
            var domains = http.GetFromJsonAsAsyncEnumerable<Domain>(url, SourceGenerationContext.Default.Domain);
            await foreach (Domain? domain in domains)
            {
                if (domain != null)
                {
                    domainList.Add(domain);
                }
            }

            return domainList.ToArray();
        }

        internal static async Task<Homology[]?> GetOrthologs(string geneId)
        {
            string url = HOST + $"/homology/id/human/{geneId}?type=orthologues&content-type=application/json";
            Logger.Trace("GetOrthologs: GET " + url);
            HomologyResponse? response = await http.GetFromJsonAsync<HomologyResponse>(url, SourceGenerationContext.Default.HomologyResponse);
            if (response == null)
                return null;

            if (response.Data == null)
                return null;

            List<Homology> homologies = [];
            foreach (var data in response.Data)
            {
                if (data.Homologies != null)
                {
                    homologies.AddRange(data.Homologies);
                }
            }
            return homologies.ToArray();
        }

        internal class HomologyReference
        {
            [JsonPropertyName("id")]
            public string? Id { get; set; }
            [JsonPropertyName("species")]
            public string? Species { get; set; }
            [JsonPropertyName("perc_id")]
            public double PercentId { get; set; }
        }

        internal class Homology
        {
            [JsonPropertyName("source")]
            public HomologyReference? Source { get; set; }
            [JsonPropertyName("target")]
            public HomologyReference? Target { get; set; }
            [JsonPropertyName("type")]
            public string? Type { get; set; }
        }

        internal class HomologyData
        {
            [JsonPropertyName("id")]
            public string? Id { get; set; }
            [JsonPropertyName("homologies")]
            public Homology[]? Homologies { get; set; }
        }

        internal class HomologyResponse
        {
            [JsonPropertyName("data")]
            public HomologyData[]? Data { get; set; }
        }

        internal class Domain
        {
            [JsonPropertyName("id")]
            public string? Id { get; set; }
            [JsonPropertyName("interpro")]
            public string? InterPro { get; set; }
            [JsonPropertyName("start")]
            public int Start { get; set; }
            [JsonPropertyName("end")]
            public int End { get; set; }
            [JsonPropertyName("description")]
            public string? Description { get; set; }
        }

        internal class Exon
        {
            [JsonPropertyName("id")]
            public string? Id { get; set; }
        }

        internal class Translation
        {
            [JsonPropertyName("id")]
            public string? Id { get; set; }
        }

        internal class Transcript
        {
            [JsonPropertyName("id")]
            public string? Id { get; set; }
            [JsonPropertyName("biotype")]
            public string? BioType { get; set; }
            public Exon[]? Exon { get; set; }
            public Translation? Translation { get; set; }
        }

        internal class Gene
        {
            public Transcript[]? Transcript { get; set; }
        }
    }

    [JsonSourceGenerationOptions(WriteIndented = false)]
    [JsonSerializable(typeof(Gene))]
    [JsonSerializable(typeof(Transcript))]
    [JsonSerializable(typeof(Exon))]
    [JsonSerializable(typeof(Translation))]
    [JsonSerializable(typeof(Domain))]
    [JsonSerializable(typeof(HomologyResponse))]
    [JsonSerializable(typeof(HomologyData))]
    [JsonSerializable(typeof(Homology))]
    [JsonSerializable(typeof(HomologyReference))]
    internal partial class SourceGenerationContext : JsonSerializerContext
    {
    }
}
