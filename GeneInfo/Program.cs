using GeneInfo;

Logger.MinLevel = Logger.LogLevel.Info;
CsvTable orthologList = CsvReader.ReadFile("orthologs.txt", ['\n', ','], 256, 2);
CsvTable domainList = CsvReader.ReadFile("domains.txt", ['\n', ','], 256, 2);
CsvTable geneList = CsvReader.ReadFile("genes.txt", ['\n', ','], 256, 2);
Logger.MinLevel = Logger.LogLevel.Trace;

bool CheckSpecies(string? species)
{
    if (species == null) return false;

    foreach (var row in orthologList.Rows)
    {
        if (row.Values.Length > 0)
        {
            string val = row.Values[0].ToString().Trim();
            string formatted = val.Replace(' ', '_').ToLowerInvariant();
            if (species.Contains(formatted, StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }
        }
    }

    return false;
}

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

void BuildTable(GeneInfo.GeneInfo? geneInfo, TranscriptInfo[] transcripts)
{
    var builder = new CsvBuilder()
    .AddColumn("Organsim", CsvType.String)
    .AddColumn("Transcript Ensembl ID", CsvType.String)
    .AddColumn("# of Exons", CsvType.Number)
    .AddColumn("# of Domains", CsvType.Number)
    .AddColumn("# of Repeat domains", CsvType.Number)
    .AddColumn("# of different domain types", CsvType.Number)
    .AddColumn("list of domains present", CsvType.String)
    .AddColumn("type", CsvType.String);

    foreach (var transcript in transcripts)
    {
        builder
            .AddToRow(transcript.Species)
            .AddToRow(transcript.Id)
            .AddToRow(transcript.ExonCount)
            .AddToRow(transcript.NumberOfDomains)
            .AddToRow(transcript.NumberOfMatchedDomains)
            .AddToRow(transcript.UniqueDomains.Length)
            .AddToRow(transcript.UniqueDomains.Length == 0 ? " " : transcript.UniqueDomains.Aggregate((a, b) => a + ", " + b))
            .AddToRow(transcript.Type)
            .PushRow();
    }

    var table = builder.ToTable();

    if (!Directory.Exists("tables/"))
    {
        Directory.CreateDirectory("tables/");
    }

    string filename = "tables/" + (geneInfo?.DisplayName ?? geneInfo?.Id ?? DateTimeOffset.Now.ToString("s")) + ".csv";
    Logger.Info($"Writing table for gene {geneInfo?.DisplayName} ({geneInfo?.Id}) to {filename}");
    CsvWriter.WriteToFile(filename, table, new CsvDialect(',', '"', '\\'), '\n');
}

bool IsGeneSymbol(string gene)
{
    return !(gene.StartsWith("ENSG", StringComparison.InvariantCultureIgnoreCase) && gene[4..].All(char.IsNumber));
}

var geneArr = geneList.Rows.Where(v => v.Values.Length > 0).Select(v => v.Values[0].ToString()).ToArray();
(GeneInfo.GeneInfo? geneInfo, TranscriptInfo[] transcripts)[] results = new (GeneInfo.GeneInfo? geneInfo, TranscriptInfo[] transcripts)[geneArr.Length];

await Parallel.ForAsync(0, geneArr.Length, async (i, cancel) =>
{
    var gene = geneArr[i].Trim();
    results[i] = await Transcripts.GetTranscriptsInfoWithOrthologs(gene, IsGeneSymbol(gene), CheckSpecies, CheckDomain, "homo_sapiens", "human");
});

for (int i = 0; i < results.Length; i++)
{
    try
    {
        BuildTable(results[i].geneInfo, results[i].transcripts);
    }
    catch (Exception e)
    {
        string filename = "tables/" + (results[i].geneInfo?.DisplayName ?? results[i].geneInfo?.Id ?? "<time>") + ".csv";
        Logger.Error("Error writing file " + filename + ": " + e.ToString());
    }
}