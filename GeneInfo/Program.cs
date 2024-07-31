using GeneInfo;

void PrintModuleList()
{
    Console.WriteLine("GeneInfo - Retrieves information about a gene through Ensembl\n");

    Console.WriteLine("This is a collection of different tools called modules that each\n" +
                      "perform different tasks using the Ensembl API. The API implementation\n" +
                      "supports rate limiting and optimizes speed by performing parallel\n" +
                      "requests for large amounts of data.\n");

    Console.WriteLine("Usage: GeneInfo [module] [arg0] [arg1] ... [argn]\n");

    Console.WriteLine("Each module has its own set of arguments that can\n" +
                      "be passed after specifying the module name.\n");

    Console.WriteLine("Included modules:\n");
    foreach (var module in IModule.IncludedModules)
    {
        module.PrintUsage();
    }
}

if(args.Length == 0)
{
    PrintModuleList();
    return;
}

string moduleInput = args[0];
IModule? module = IModule.GetModule(moduleInput);

if(module == null)
{
    Logger.Error($"Module '{moduleInput}' was not found.");
    PrintModuleList();
    return;
}

if(!await module.Run(args[1..]))
{
    module.PrintUsage();
}