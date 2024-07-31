# GeneInfo

This is a collection of different tools called modules that each
perform different tasks using the Ensembl API. The API implementation
supports rate limiting and optimizes speed by performing parallel
requests for large amounts of data.

### Usage:
`GeneInfo [module] [arg0] [arg1] ... [argn]`

Each module has its own set of arguments that can
be passed after specifying the module name.

### Included modules:

- #### help - Shows description and usage examples for a module.
    - Usage:   `help [module]`
    - Example: `help gene_traverse`
- #### gene_traverse - Collects information about transcripts in a gene and its orthologs.
    - Usage:   `gene_traverse [path_to_gene_list] [path_to_ortholog_list] [path_to_domain_list] [output_dir]`
    - Example: `gene_traverse genes.txt orthologs.txt domains.txt tables/`
- #### exon_counter - Counts the number of exons that start inside a domain of a transcript.
    - Usage:   `exon_counter [path_to_transcript_list or wildcard] [path_to_domain_list] [path_of_output_file.csv or wildcard (first * - name, second * - extension)]`
    - Example: `exon_counter transcripts.txt domains.txt exoncounts.csv`
    - Example: `exon_counter tables/*.csv domains.txt tables/*.exoncounts.*`
