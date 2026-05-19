using System;
using System.Text.RegularExpressions;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Suffixed.Part;

namespace kOS.Suffixed.PartModuleField
{
    [kOS.Safe.Utilities.KOSNomenclature("ModuleLexicon")]
    public class ModuleLexicon : Structure, IIndexable
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
                list.Add(new StringValue(module.moduleName));
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
            int i = 0;
            foreach (PartModule module in part.Part.Modules)
            {
                str += "\n[" + i++ + "] " + module.moduleName;
                string trimmedName = TrimModuleName(module.moduleName);
                if (trimmedName != module.moduleName)
                    str += " / " + trimmedName;
            }
            return str;
        }

        private PartModuleFields GetModule(string moduleName)
        {
            // For convenience its possible to access a module without "Module" at the start of its name
            // Here we need to make sure module named "Anything" cant be hidden by module "ModuleAnything"
            PartModule matchedModule = null;
            foreach (PartModule module in part.Part.Modules)
            {
                if (string.Equals(module.moduleName, moduleName, StringComparison.OrdinalIgnoreCase))
                {
                    matchedModule = module;
                    break;
                }
                if (string.Equals(TrimModuleName(module.moduleName), moduleName, StringComparison.OrdinalIgnoreCase))
                {
                    matchedModule = module;
                }
            }

            if (matchedModule == null)
                return null;
            return PartModuleFieldsFactory.Construct(matchedModule, shared);
        }

        public override ISuffixResult GetSuffix(string suffixName, bool failOkay = false)
        {
            PartModuleFields module = GetModule(suffixName);

            if (module != null)
                return new SuffixResult(module);
            
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

        public Structure GetIndex(Structure index)
        {
            if (index is StringValue str)
            {
                PartModuleFields module = GetModule(str);
                if (module != null)
                    return module;
            }
            else if (index is ScalarValue scalar)
            {
                int intIndex = (int)scalar;
                if (intIndex >= 0 && intIndex < part.Part.Modules.Count)
                {
                    return PartModuleFieldsFactory.Construct(part.Part.Modules[intIndex], shared);
                }
                throw new Exception("Index out of range for " + ToString());
            }
            throw new Exception("No key \"" + index + "\" found on " + ToString());
        }

        public void SetIndex(Structure index, Structure value)
        {
            throw new Exception("Cannot set indexes on ModuleLexicon.");
        }

        public Structure GetIndex(int index) => GetIndex((Structure)new ScalarIntValue(index));

        public void SetIndex(int index, Structure value) => SetIndex(null, null);
    }
}
