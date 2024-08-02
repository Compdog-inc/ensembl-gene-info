namespace GeneInfo
{
    public interface IModule
    {
        string Name { get; }
        string Description { get; }
        string Usage { get; }
        string[] Examples { get; }
        Task<bool> Run(string[] args);

        public static string FormatModuleName(string moduleName)
        {
            return moduleName.Trim().Replace(" ", "").Replace("_", "").Replace("-", "");
        }

        public static IModule? GetModule(string name)
        {
            name = FormatModuleName(name);
            return IncludedModules.FirstOrDefault(m => FormatModuleName(m.Name).Equals(name, StringComparison.InvariantCultureIgnoreCase));
        }

        public static void PrintModuleUsage(IModule module)
        {
            Console.WriteLine($"    {module.Name} - {module.Description}\n        Usage:   {module.Usage}");
            foreach (var example in module.Examples)
            {
                Console.WriteLine($"        Example: {example}");
            }
        }

        public static string ParseFileWildcard(string? outputDir, string wildcard, string path)
        {
            string wild = Path.GetFileName(wildcard);
            int nameIndex = wild.IndexOf('*');
            int extIndex = wild.LastIndexOf('*');

            bool longExt = false;
            if (extIndex > 0 && wild[extIndex - 1] == '.')
            {
                longExt = true;
                extIndex--;
            }

            if (nameIndex != -1 && extIndex > 0)
                wild = (nameIndex > 0 ? wild[..(nameIndex - 1)] : string.Empty) + Path.GetFileNameWithoutExtension(path) + (extIndex - nameIndex > 1 ? wild[(nameIndex + 1)..(extIndex - 1)] : string.Empty) + Path.GetExtension(path) + wild[(extIndex + (longExt ? 2 : 1))..];
            else if (nameIndex != -1)
                wild = (nameIndex > 0 ? wild[..(nameIndex - 1)] : string.Empty) + Path.GetFileNameWithoutExtension(path) + wild[(nameIndex + 1)..];

            return Path.Join(outputDir, wild);
        }

        public static readonly IModule[] IncludedModules = [
            new Help(),
            new GeneTraverse(),
            new ExonCounter(),
            new ExonCounterRatio(),
            new DomainFilter(),
            ];
    }

    public static class IModuleExtensions
    {
        public static void PrintUsage(this IModule module)
        {
            IModule.PrintModuleUsage(module);
        }
    }
}
