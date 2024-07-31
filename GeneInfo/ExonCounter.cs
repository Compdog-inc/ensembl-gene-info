using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneInfo
{
    public class ExonCounter : IModule
    {
        public string Name => "exon_counter";

        public string Description => "Counts the number of exons that start inside a domain of a transcript.";

        public string Usage => "exon_counter [path_to_transcript_list] [path_to_domain_list] [path_of_output_file.csv]";

        public string Example => "exon_counter transcripts.txt domains.txt exoncounts.csv";

        public async Task<bool> Run(string[] args)
        {
            if (args.Length < 3)
                return false;

            string geneListPath = args[0];
            string domainListPath = args[1];
            string outputPath = args[2];

            bool validationError = false;
            if (!File.Exists(geneListPath))
            {
                Logger.Error($"File '{geneListPath}' does not exist.");
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
            } catch
            {
                Logger.Error($"File '{outputPath}' is not accessible.");
                validationError = true;
            }

            if (validationError)
                return true;

            return true;
        }
    }
}
