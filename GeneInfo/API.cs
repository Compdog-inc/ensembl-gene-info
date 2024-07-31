using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using static GeneInfo.API;

namespace GeneInfo
{
    internal static class API
    {
        internal class HttpRequestExceptionExt : HttpRequestException
        {
            public HttpResponseMessage? HttpMessage { get; }

            public HttpRequestExceptionExt(string? message, Exception? inner, HttpStatusCode? statusCode, HttpResponseMessage? msg) : base(message, inner, statusCode)
            {
                HttpMessage = msg;
            }
        }

        const int MAX_RETRY_DELAY = 16000;
        const int RETRY_INCREMENT = 2000;
        const int INITIAL_RETRY_DELAY = 2000;

        internal const string HOST = "https://rest.ensembl.org";
        private static HttpClient http = new();
        private static TokenBucketRateLimiter httpRateLimiter = new(new()
        {
            AutoReplenishment = true,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            ReplenishmentPeriod = TimeSpan.FromSeconds(0.1),
            TokenLimit = 10,
            QueueLimit = int.MaxValue,
            TokensPerPeriod = 1
        });

        private static void HandleError(string name, ref int retry, string url, HttpRequestExceptionExt e)
        {
            if (e.StatusCode == HttpStatusCode.TooManyRequests)
            {
                if (e.HttpMessage != null && e.HttpMessage.Headers.RetryAfter != null && e.HttpMessage.Headers.RetryAfter.Delta.HasValue)
                {
                    Logger.Warn($"{name}: Rate limited " + url);
                    retry = (int)e.HttpMessage.Headers.RetryAfter.Delta.Value.TotalMilliseconds;
                } else if(e.HttpMessage == null)
                {
                    Logger.Warn($"{name}: Rate limited {e.Message} " + url);
                }
                else
                {
                    Logger.Warn($"{name}: Rate limited " + url);
                }
            }
            else if (e.StatusCode == HttpStatusCode.InternalServerError)
            {
                Logger.Warn($"{name}: Internal server error " + url);
                retry += 2 * RETRY_INCREMENT;
            }
            else
            {
                Logger.Error($"{name}: ERROR " + url + " " + e.ToString());
            }
            Logger.Info($"{name}: Retrying request in {retry / 1000} seconds");
        }

        internal static async Task<Gene?> GetGene(string geneId)
        {
            string url = HOST + $"/lookup/id/{geneId}?expand=1;content-type=application/json";
            Logger.Trace("GetGene: GET " + url);
            Gene? gene = null;
            int retry = INITIAL_RETRY_DELAY;
            while (true)
            {
                try
                {
                    using var rate = await httpRateLimiter.AcquireAsync();
                    if (rate.IsAcquired)
                    {
                        var res = await http.GetAsync(url);
                        if (res.IsSuccessStatusCode)
                        {
                            gene = await res.Content.ReadFromJsonAsync<Gene>(SourceGenerationContext.Default.Gene);
                            break;
                        }
                        else
                        {
                            throw new HttpRequestExceptionExt(res.ReasonPhrase, null, res.StatusCode, res);
                        }
                    }
                    else
                    {
                        throw new HttpRequestExceptionExt("Local bucket rate limit reached", null, HttpStatusCode.TooManyRequests, null);
                    }
                }
                catch (HttpRequestExceptionExt e)
                {
                    HandleError("GetGene", ref retry, url, e);
                    await Task.Delay(retry);
                    retry = Math.Min(MAX_RETRY_DELAY, retry + RETRY_INCREMENT);
                }
            }
            return gene;
        }

        internal static async Task<Gene?> GetGeneWithSymbol(string symbol)
        {
            string url = HOST + $"/lookup/symbol/homo_sapiens/{symbol}?expand=1;content-type=application/json";
            Logger.Trace("GetGeneWithSymbol: GET " + url);
            Gene? gene = null;
            int retry = INITIAL_RETRY_DELAY;
            while (true)
            {
                try
                {
                    using var rate = await httpRateLimiter.AcquireAsync();
                    if (rate.IsAcquired)
                    {
                        var res = await http.GetAsync(url);
                        if (res.IsSuccessStatusCode)
                        {
                            gene = await res.Content.ReadFromJsonAsync<Gene>(SourceGenerationContext.Default.Gene);
                            break;
                        }
                        else
                        {
                            throw new HttpRequestExceptionExt(res.ReasonPhrase, null, res.StatusCode, res);
                        }
                    }
                    else
                    {
                        throw new HttpRequestExceptionExt("Local bucket rate limit reached", null, HttpStatusCode.TooManyRequests, null);
                    }
                }
                catch (HttpRequestExceptionExt e)
                {
                    HandleError("GetGeneWithSymbol", ref retry, url, e);
                    await Task.Delay(retry);
                    retry = Math.Min(MAX_RETRY_DELAY, retry + RETRY_INCREMENT);
                }
            }
            return gene;
        }

        internal static async Task<Transcript?> GetTranscript(string transcriptId)
        {
            string url = HOST + $"/lookup/id/{transcriptId}?expand=1;content-type=application/json";
            Logger.Trace("GetTranscript: GET " + url);
            Transcript? transcript = null;
            int retry = INITIAL_RETRY_DELAY;
            while (true)
            {
                try
                {
                    using var rate = await httpRateLimiter.AcquireAsync();
                    if (rate.IsAcquired)
                    {
                        var res = await http.GetAsync(url);
                        if (res.IsSuccessStatusCode)
                        {
                            transcript = await res.Content.ReadFromJsonAsync<Transcript>(SourceGenerationContext.Default.Transcript);
                            break;
                        }
                        else
                        {
                            throw new HttpRequestExceptionExt(res.ReasonPhrase, null, res.StatusCode, res);
                        }
                    }
                    else
                    {
                        throw new HttpRequestExceptionExt("Local bucket rate limit reached", null, HttpStatusCode.TooManyRequests, null);
                    }
                }
                catch (HttpRequestExceptionExt e)
                {
                    HandleError("GetTranscript", ref retry, url, e);
                    await Task.Delay(retry);
                    retry = Math.Min(MAX_RETRY_DELAY, retry + RETRY_INCREMENT);
                }
            }
            return transcript;
        }

        internal static async Task<Domain[]?> GetSmartDomains(string translationId)
        {
            List<Domain> domainList = new List<Domain>();
            string url = HOST + $"/overlap/translation/{translationId}?type=Smart&content-type=application/json";
            Logger.Trace("GetSmartDomains: GET " + url);
            int retry = INITIAL_RETRY_DELAY;
            while (true)
            {
                try
                {
                    using var rate = await httpRateLimiter.AcquireAsync();
                    if (rate.IsAcquired)
                    {
                        var res = await http.GetAsync(url);
                        if (res.IsSuccessStatusCode)
                        {
                            var domains = res.Content.ReadFromJsonAsAsyncEnumerable<Domain>(SourceGenerationContext.Default.Domain);
                            await foreach (Domain? domain in domains)
                            {
                                if (domain != null)
                                {
                                    domainList.Add(domain);
                                }
                            }
                            break;
                        }
                        else
                        {
                            throw new HttpRequestExceptionExt(res.ReasonPhrase, null, res.StatusCode, res);
                        }
                    }
                    else
                    {
                        throw new HttpRequestExceptionExt("Local bucket rate limit reached", null, HttpStatusCode.TooManyRequests, null);
                    }
                }
                catch (HttpRequestExceptionExt e)
                {
                    HandleError("GetSmartDomains", ref retry, url, e);
                    await Task.Delay(retry);
                    retry = Math.Min(MAX_RETRY_DELAY, retry + RETRY_INCREMENT);
                    domainList.Clear();
                }
            }

            return domainList.ToArray();
        }

        internal static async Task<Homology[]?> GetOrthologs(string geneId)
        {
            string url = HOST + $"/homology/id/human/{geneId}?type=orthologues&content-type=application/json";
            Logger.Trace("GetOrthologs: GET " + url);

            HomologyResponse? response = null;
            int retry = INITIAL_RETRY_DELAY;
            while (true)
            {
                try
                {
                    using var rate = await httpRateLimiter.AcquireAsync();
                    if (rate.IsAcquired)
                    {
                        var res = await http.GetAsync(url);
                        if (res.IsSuccessStatusCode)
                        {
                            response = await res.Content.ReadFromJsonAsync<HomologyResponse>(SourceGenerationContext.Default.HomologyResponse);
                            break;
                        }
                        else
                        {
                            throw new HttpRequestExceptionExt(res.ReasonPhrase, null, res.StatusCode, res);
                        }
                    }
                    else
                    {
                        throw new HttpRequestExceptionExt("Local bucket rate limit reached", null, HttpStatusCode.TooManyRequests, null);
                    }
                }
                catch (HttpRequestExceptionExt e)
                {
                    HandleError("GetOrthologs", ref retry, url, e);
                    await Task.Delay(retry);
                    retry = Math.Min(MAX_RETRY_DELAY, retry + RETRY_INCREMENT);
                }
            }

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

        internal static async Task<MapEntry[]?> MapTranslation(string translationId, int start, int end)
        {
            string url = HOST + $"/map/translation/{translationId}/{start}..{end}?content-type=application/json";
            Logger.Trace("MapTranslation: GET " + url);

            MapResponse? response = null;
            int retry = INITIAL_RETRY_DELAY;
            while (true)
            {
                try
                {
                    using var rate = await httpRateLimiter.AcquireAsync();
                    if (rate.IsAcquired)
                    {
                        var res = await http.GetAsync(url);
                        if (res.IsSuccessStatusCode)
                        {
                            response = await res.Content.ReadFromJsonAsync<MapResponse>(SourceGenerationContext.Default.MapResponse);
                            break;
                        }
                        else
                        {
                            throw new HttpRequestExceptionExt(res.ReasonPhrase, null, res.StatusCode, res);
                        }
                    }
                    else
                    {
                        throw new HttpRequestExceptionExt("Local bucket rate limit reached", null, HttpStatusCode.TooManyRequests, null);
                    }
                }
                catch (HttpRequestExceptionExt e)
                {
                    HandleError("MapTranslation", ref retry, url, e);
                    await Task.Delay(retry);
                    retry = Math.Min(MAX_RETRY_DELAY, retry + RETRY_INCREMENT);
                }
            }

            if (response == null)
                return null;

            if (response.Mappings == null)
                return null;

            return response.Mappings;
        }

        internal class MapEntry
        {
            [JsonPropertyName("start")]
            public int Start { get; set; }
            [JsonPropertyName("end")]
            public int End { get; set; }
        }

        internal class MapResponse
        {
            [JsonPropertyName("mappings")]
            public MapEntry[]? Mappings { get; set; }
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
            [JsonPropertyName("start")]
            public int Start { get; set; }
            [JsonPropertyName("end")]
            public int End { get; set; }
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
            [JsonPropertyName("id")]
            public string? Id { get; set; }
            [JsonPropertyName("display_name")]
            public string? DisplayName { get; set; }
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
    [JsonSerializable(typeof(MapResponse))]
    [JsonSerializable(typeof(MapEntry))]
    internal partial class SourceGenerationContext : JsonSerializerContext
    {
    }
}
