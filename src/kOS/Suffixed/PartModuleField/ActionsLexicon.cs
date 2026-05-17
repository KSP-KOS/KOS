using System;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;

namespace kOS.Suffixed.PartModuleField
{
    [kOS.Safe.Utilities.KOSNomenclature("ActionsLexicon", KOSToCSharp = false)]
    public class ActionsLexicon : Structure
    {
        private readonly PartModuleFields partModuleFields;
        private readonly PartModule partModule;
        
        public ActionsLexicon(PartModuleFields partModuleFields, PartModule partModule)
        {
            this.partModuleFields = partModuleFields;
            this.partModule = partModule;
            
            AddSuffix("MODULE", new NoArgsSuffix<PartModuleFields>(() => partModuleFields));
            AddSuffix("KEYS", new Suffix<ListValue>(Keys));
            AddSuffix("HASKEY", new OneArgsSuffix<BooleanValue, StringValue>(HasKey));
            AddSuffix("LENGTH", new NoArgsSuffix<ScalarValue>(() => Keys().Count));
        }

        private ListValue Keys()
        {
            var list = new ListValue();
            foreach (var action in partModule.Actions)
            {
                list.Add(new StringValue(action.name));
            }
            return list;
        }

        private BooleanValue HasKey(StringValue key)
        {
            foreach (var action in partModule.Actions)
            {
                if (string.Equals(action.name, key, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }
        
        public override string ToString()
        {
            string str = "ActionsLexicon of " + partModule.moduleName + ", containing visible keys:";
            foreach (var actions in partModule.Actions)
            {
                str += "\n" + actions.name + " (" + actions.guiName.ToLower() + ")";
            }
            return str;
        }
        
        public override ISuffixResult GetSuffix(string suffixName, bool failOkay = false)
        {
            foreach (var action in partModule.Actions)
            {
                if (string.Equals(action.name, suffixName, StringComparison.CurrentCultureIgnoreCase))
                {
                    return new OneArgsSuffix<BooleanValue>(value => partModuleFields.CallKSPActionProper(action, value)).Get();
                }
            }
            
            var baseResult = base.GetSuffix(suffixName, true);
            if (baseResult != null) return baseResult;

            if (failOkay) return null;
            throw new Exception("No key or suffix \""+suffixName+"\" found on "+ToString());
        }
        
        public override bool SetSuffix(string suffixName, object value, bool failOkay = false)
        {
            if (failOkay) return false;
            throw new Exception("Cannot set suffixes on ActionsLexicon.");
        }
    }
}
