﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneInfo
{
    public class Help : IModule
    {
        public string Name => "help";

        public string Description => "Shows description and usage examples for a module.";

        public string Usage => "help [module]";

        public string[] Examples => ["help gene_traverse"];

        public async Task<bool> Run(string[] args)
        {
            if (args.Length == 0)
                return false;

            string moduleInput = args[0];
            IModule? module = IModule.GetModule(moduleInput);

            if (module == null)
            {
                Logger.Error($"Module '{moduleInput}' was not found.");
                return true;
            }

            module.PrintUsage();

            await Task.Yield();
            return true;
        }
    }
}
