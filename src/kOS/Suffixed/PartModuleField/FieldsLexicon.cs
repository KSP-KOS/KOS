using System;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;

namespace kOS.Suffixed.PartModuleField
{
    [kOS.Safe.Utilities.KOSNomenclature("FieldsLexicon", KOSToCSharp = false)]
    public class FieldsLexicon : Structure
    {
        private readonly PartModule partModule;
        
        public FieldsLexicon(PartModuleFields partModuleFields, PartModule partModule)
        {
            this.partModule = partModule;
            
            AddSuffix("MODULE", new NoArgsSuffix<PartModuleFields>(() => partModuleFields));
            AddSuffix("KEYS", new Suffix<ListValue>(Keys));
            AddSuffix("HASKEY", new OneArgsSuffix<BooleanValue, StringValue>(HasKey));
            AddSuffix("LENGTH", new NoArgsSuffix<ScalarValue>(() => Keys().Count));
        }

        private ListValue Keys()
        {
            var list = new ListValue();
            foreach (var field in partModule.Fields)
            {
                if (PartModuleFields.FieldIsVisible(field))
                {
                    list.Add(new StringValue(field.name));
                }
            }
        
            return list;
        }

        private BooleanValue HasKey(StringValue key)
        {
            foreach (var field in partModule.Fields)
            {
                if (PartModuleFields.FieldIsVisible(field) && string.Equals(field.name, key, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }
        
        public override string ToString()
        {
            string str = "FieldsLexicon of " + partModule.moduleName + ", containing visible keys:";
            foreach (var field in partModule.Fields)
            {
                if (PartModuleFields.FieldIsVisible(field))
                {
                    partModule.Fields.TryGetFieldUIControl(field.name, out UI_Control control);
                    str += "\n" + (control.controlEnabled && !(control is UI_Label) ? "(settable) " : "(get-only) ") +
                           field.name + " (" + field.guiName.ToLower() + "), is " +
                           Utilities.Utils.KOSType(field.FieldInfo.FieldType);
                }
            }
            return str;
        }
        
        public override ISuffixResult GetSuffix(string suffixName, bool failOkay = false)
        {
            // Allows getting "hidden" fields
            foreach (var field in partModule.Fields)
            {
                if (string.Equals(field.name, suffixName, StringComparison.CurrentCultureIgnoreCase))
                {
                    return new SuffixResult(FromPrimitiveWithAssert(field.GetValue(partModule)));
                }
            }
            
            var baseResult = base.GetSuffix(suffixName, true);
            if (baseResult != null) return baseResult;

            if (failOkay) return null;
            throw new Exception("No key or suffix \""+suffixName+"\" found on "+ToString());
        }
        
        public override bool SetSuffix(string suffixName, object value, bool failOkay = false)
        {
            foreach (var field in partModule.Fields)
            {
                if (string.Equals(field.name, suffixName, StringComparison.CurrentCultureIgnoreCase))
                {
                    PartModuleFields.SetFieldProper(partModule, field, FromPrimitiveWithAssert(value));
                    return true;
                }
            }
            
            if (failOkay) return false;
            throw new Exception("No key \""+suffixName+"\" found on "+ToString());
        }
    }
}
