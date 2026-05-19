using System;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;

namespace kOS.Suffixed.PartModuleField
{
    [kOS.Safe.Utilities.KOSNomenclature("EventsLexicon")]
    public class EventsLexicon : Structure
    {
        private readonly PartModuleFields partModuleFields;
        private readonly PartModule partModule;
        
        public EventsLexicon(PartModuleFields partModuleFields, PartModule partModule)
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
            foreach (var ev in partModule.Events)
            {
                if (PartModuleFields.EventIsVisible(ev))
                {
                    list.Add(new StringValue(ev.name));
                }
            }
            return list;
        }

        private BooleanValue HasKey(StringValue key)
        {
            foreach (var ev in partModule.Events)
            {
                if (PartModuleFields.EventIsVisible(ev) && string.Equals(ev.name, key, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }
        
        public override string ToString()
        {
            string str = "EventsLexicon of " + partModule.moduleName + ", containing visible keys:";
            foreach (var ev in partModule.Events)
            {
                if (PartModuleFields.EventIsVisible(ev))
                {
                    str += "\n" + ev.name + " (" + ev.guiName.ToLower() + ")";
                }
            }
            return str;
        }
        
        public override ISuffixResult GetSuffix(string suffixName, bool failOkay = false)
        {
            foreach (var ev in partModule.Events)
            {
                if (PartModuleFields.EventIsVisible(ev) &&
                    string.Equals(ev.name, suffixName, StringComparison.CurrentCultureIgnoreCase))
                {
                    return new NoArgsVoidSuffix(() => partModuleFields.CallKSPEventProper(ev, suffixName)).Get();
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
            throw new Exception("Cannot set suffixes on EventsLexicon.");
        }
    }
}