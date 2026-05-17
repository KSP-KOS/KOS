using System;
using System.Text.RegularExpressions;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Suffixed.Part;

namespace kOS.Suffixed.PartModuleField
{
    [kOS.Safe.Utilities.KOSNomenclature("ModuleLexicon")]
    public class ModuleLexicon : Structure
    {
        private readonly PartValue part;
        private readonly SharedObjects shared;
        
        public ModuleLexicon(PartValue part, SharedObjects shared)
        {
            this.part = part;
            this.shared = shared;
            
            AddSuffix("PART", new NoArgsSuffix<PartValue>(() => part));
            AddSuffix("KEYS", new Suffix<ListValue>(GetKeys));
            AddSuffix("HASKEY", new OneArgsSuffix<BooleanValue, StringValue>(HasKey));
            AddSuffix("LENGTH", new Suffix<ScalarIntValue>(() => part.Part.Modules.Count));
        }
        
        private static string TrimModuleName(string moduleName) => Regex.Replace(moduleName, "^Module", "");

        private ListValue GetKeys()
        {
            var list = new ListValue();
            foreach (PartModule module in part.Part.Modules)
            {
                string trimmedName = TrimModuleName(module.moduleName);
                list.Add(new StringValue(module.moduleName));
                if (trimmedName != module.moduleName)
                    list.Add(new StringValue(trimmedName));
            }
            return list;
        }

        private BooleanValue HasKey(StringValue key)
        {
            foreach (PartModule module in part.Part.Modules)
            {
                if (string.Equals(module.moduleName, key, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(TrimModuleName(module.moduleName), key, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        public override string ToString()
        {
            string str = "ModuleLexicon, containing keys:";
            foreach (PartModule module in part.Part.Modules)
            {
                str += "\n" + Regex.Replace(module.moduleName, "^Module", "[Module]");
            }
            return str;
        } 

        public override ISuffixResult GetSuffix(string suffixName, bool failOkay = false)
        {
            // For convenience its possible to access a module without "Module" at the start of its name
            // Here we need to make sure module named "Anything" cant be hidden by module "ModuleAnything"
            PartModule matchedModule = null;
            foreach (PartModule module in part.Part.Modules)
            {
                if (string.Equals(module.moduleName, suffixName, StringComparison.OrdinalIgnoreCase))
                {
                    matchedModule = module;
                    break;
                }
                if (string.Equals(TrimModuleName(module.moduleName), suffixName, StringComparison.OrdinalIgnoreCase))
                {
                    matchedModule = module;
                }
            }

            if (matchedModule != null)
                return new SuffixResult(PartModuleFieldsFactory.Construct(matchedModule, shared));
            
            var baseResult = base.GetSuffix(suffixName, true);
            if (baseResult != null) return baseResult;

            if (failOkay) return null;
            throw new Exception("No key or suffix \""+suffixName+"\" found on "+ToString());
        }

        public override bool SetSuffix(string suffixName, object value, bool failOkay = false)
        {
            if (failOkay) return false;
            throw new Exception("Cannot set suffixes on ModuleLexicon.");
        }
    }
}
