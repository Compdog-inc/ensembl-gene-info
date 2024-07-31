namespace GeneInfo
{
    public interface IModule
    {
        string Name { get; }
        string Description { get; }
        string Usage { get; }
        string Example { get; }
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

        public static readonly IModule[] IncludedModules = [
            new Help(),
            new GeneTraverse(),
            new ExonCounter()
            ];
    }
}
