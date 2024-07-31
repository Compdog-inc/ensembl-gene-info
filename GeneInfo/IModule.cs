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

        public static readonly IModule[] IncludedModules = [
            new Help(),
            new GeneTraverse(),
            new ExonCounter()
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
