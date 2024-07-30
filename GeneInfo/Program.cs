using GeneInfo;

Logger.MinLevel = Logger.LogLevel.Info;
CsvTable orthologList = CsvReader.ReadFile("orthologs.txt", ['\n', ','], 256, 2);
CsvTable domainList = CsvReader.ReadFile("domains.txt", ['\n', ','], 256, 2);
Logger.MinLevel = Logger.LogLevel.Trace;

TranscriptInfo[]? transcripts = await Transcripts.GetTranscriptsInfoWithOrthologs("ENSG00000176871", (species) =>
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
}, (domain) =>
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
}, "Homo sapiens", "human");

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
        .AddToRow(transcript.UniqueDomains.Aggregate((a, b) => a + ", " + b))
        .AddToRow(transcript.Type)
        .PushRow();
}

var table = builder.ToTable();
CsvWriter.WriteToTextWriter(Console.Out, table, new CsvDialect('|', '"', '\\'), '\n');