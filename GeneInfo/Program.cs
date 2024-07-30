using GeneInfo;

TranscriptInfo[]? transcripts = await Transcripts.GetTranscriptsInfoWithOrthologs("ENSG00000176871", (species) =>
{
    return species == "pan_troglodytes";
}, (domain) =>
{
    return domain?.Contains("wd40", StringComparison.InvariantCultureIgnoreCase) ?? false;
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
        .AddToRow(transcript.NumberOfMatchedDomains)
        .PushRow();
}

var table = builder.ToTable();
CsvWriter.WriteToTextWriter(Console.Out, table, new CsvDialect('|', '"', '\\'), '\n');