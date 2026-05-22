using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using kOS.Suffixed.PartModuleField;
using System;

namespace kOS.Suffixed.Part
{


    class ConverterValue : PartValue
    {
        private ListValue<ConverterFields> Converters;

        public ConverterValue(global::Part part, SharedObjects sharedObj)
            : base(part, sharedObj)
        {
            Converters = new ListValue<ConverterFields>();

            foreach (PartModule module in Part.Modules)
            {
                var ConverterModule = module as ModuleResourceConverter;
                if (ConverterModule != null)
                {
                    Converters.Add(new ConverterFields(ConverterModule, sharedObj));
                }
            }

            ConverterInitializeSuffixes();

        }

        private void ConverterInitializeSuffixes()
        {
            

            AddSuffix(new[] { "CONVERTERMODULES", "CONVMODS" }, new Suffix<ListValue<ConverterFields>>(() => Converters));
            AddSuffix(new[] { "HASCONVERTER", "HASCONV" }, new OneArgsSuffix<bool, string>(HasConverterModule));
            AddSuffix(new[] { "GETCONVERTER", "GETCONV" }, new OneArgsSuffix<ConverterFields, string>(GetConverterModule));

            AddSuffix(new[] { "CONVERTERCOUNT", "CONVCOUNT" }, new Suffix<int>(() => Converters.Count));
            // all suffixes

            AddSuffix(new[] { "CONVERTERNAMES", "CONVNAMES" }, new Suffix<ListValue>(() =>
            {
                var toReturn = new ListValue();
                foreach (ConverterFields conv in Converters) { toReturn.Add(conv.Converter.ConverterName); }
                return toReturn;
            }));

            AddSuffix(new[] { "CONVERTERNAME", "CONVNAME" }, new Suffix<string>(() => 
            {
                string toReturn = "";
                foreach (ConverterFields conv in Converters) { toReturn=StringAdd(toReturn, conv.Converter.ConverterName, ", "); }
                return toReturn;

            }));

            AddSuffix("START", new NoArgsSuffix(() =>
            {
                foreach (ConverterFields conv in Converters)
                {
                    conv.Converter.StartResourceConverter();
                }
            }));
            AddSuffix("STOP", new NoArgsSuffix(() =>
            {
                foreach (ConverterFields conv in Converters)
                {
                    conv.Converter.StopResourceConverter();
                }
            }));
            AddSuffix("STATUSLIST", new Suffix<ListValue>(() =>
            {
                var toReturn = new ListValue();
                foreach (ConverterFields conv in Converters) { toReturn.Add(conv.Converter.status); }
                return toReturn;
            }));
            AddSuffix("STATUSFULL", new Suffix<ListValue>(() =>
            {
                var toReturn = new ListValue();
                foreach (ConverterFields conv in Converters) { toReturn.Add(conv.Converter.ConverterName+": "+conv.Converter.status); }
                return toReturn;
            }));

            AddSuffix("STATUS", new Suffix<string>(() => 
            {
                if (Converters.Count == 1) { return Converters[0].Converter.status; }
                else
                {
                    string toReturn = "";
                    foreach (ConverterFields conv in Converters) { toReturn= LineAdd(toReturn, (conv.Converter.ConverterName + ": " + conv.Converter.status)); }
                    return toReturn;
                }
            }));

            AddSuffix("ISACTIVATED", new SetSuffix<bool>(() => 
            {
                foreach (ConverterFields conv in Converters)
                {
                    if (conv.Converter.IsActivated) { return true; }
                }
                return false;
            }, value => {
                foreach (ConverterFields conv in Converters)
                {
                    conv.toggle(value);
                }
            }));
            AddSuffix("ISRUNNING", new Suffix<bool>(() => 
            {
                foreach (ConverterFields conv in Converters)
                {
                    if (conv.Converter.status.EndsWith("% load")) { return true; }
                }
                return false;
            }));
            AddSuffix("ACTIVATED", new Suffix<int>(() =>
            {
                int toReturn = 0;
                foreach (ConverterFields conv in Converters)
                {
                    if (conv.Converter.IsActivated) { toReturn++; }
                }
                return toReturn;
            })); //count of 
            AddSuffix("RUNNING", new Suffix<int>(() =>
            {
                int toReturn = 0;
                foreach (ConverterFields conv in Converters)
                {
                    if (conv.Converter.status.EndsWith("% load")) { toReturn++; }
                }
                return toReturn;
            }));


            AddSuffix("ALWAYSACTIVE", new Suffix<bool>(() => {
                foreach (ConverterFields conv in Converters)
                {
                    if (conv.Converter.AlwaysActive) { return true; }
                }
                return false;
            }));
            AddSuffix("GENERATESHEAT", new Suffix<bool>(() => {
                foreach (ConverterFields conv in Converters)
                {
                    if (conv.Converter.GeneratesHeat) { return true; }
                }
                return false;
            }));
            AddSuffix(new[] { "CORETEMPERATURE", "CORETEMP" }, new Suffix<double>(() => {
                if (Converters.Count == 0) { return Part.temperature; } //it seems to give that if there's no coreHeat module as well
                return Converters[0].Converter.GetCoreTemperature();
            })); //same for all (from another module)
            AddSuffix(new[] { "GOALTEMPERATURE", "GOALTEMP" }, new Suffix<double>(() => {
                if (Converters.Count == 0) { return Part.temperature; }
                return Converters[0].Converter.GetGoalTemperature(); 
            })); //same for all (from another module)

            AddSuffix("FILLAMOUNT", new Suffix<float>(() => 
            {
                if (Converters.Count == 0) { return 0; }
                float sum = 0; float act = 0; int Nact = 0;
                foreach (ConverterFields conv in Converters)
                {
                    float t = conv.Converter.FillAmount;
                    sum = sum + t;
                    if (conv.Converter.IsActivated) { act = act + t; Nact++; }
                }
                if (Nact > 0) { return act / Nact; } else { return sum / Converters.Count; }
            }));

            AddSuffix("TAKEAMOUNT", new Suffix<float>(() => 
            {
                if (Converters.Count == 0) { return 0; }
                float sum = 0; float act = 0; int Nact = 0;
                foreach (ConverterFields conv in Converters)
                {
                    float t = conv.Converter.TakeAmount;
                    sum = sum + t;
                    if (conv.Converter.IsActivated) { act = act + t; Nact++; }
                }
                if (Nact > 0) { return act / Nact; } else { return sum / Converters.Count; }
            }));

            AddSuffix(new[] { "THERMALEFFICIENCY", "THERMEFF" }, new Suffix<float>(() => 
            {
                if (Converters.Count == 0) { return 0; }
                float sum = 0; float act = 0; int Nact = 0;
                foreach (ConverterFields conv in Converters)
                {
                    float te = conv.ThermEff();
                    sum = sum + te;
                    if (conv.Converter.IsActivated) { act = act + te; Nact++; }
                }
                if (Nact > 0) { return act / Nact; } else { return sum / Converters.Count; }
            }));

            AddSuffix("GETINFO", new Suffix<string>(() => 
            {
                string toReturn = "";
                foreach (ConverterFields conv in Converters) { toReturn = LineAdd(toReturn, conv.writeInfo()); }
                return toReturn;
            }));


            AddSuffix(new[] { "CONVERTERLOAD", "CONVLOAD" }, new Suffix<float>(() =>
            {
                float sum = 0; int Nact = 0;
                foreach (ConverterFields conv in Converters)
                {
                    if (conv.Converter.IsActivated) { sum = sum + conv.conversionLoad(); Nact++; }
                }
                if (Nact == 0) { return 0; } else { return sum / Nact; }
            })); //actual converter load (average for active converters)


            AddSuffix("INPUT", new Suffix<Lexicon<string, double>>(() => {
                var toReturn = new Lexicon<string, double>();
                foreach (ConverterFields conv in Converters)
                {
                    MergeResLex(toReturn, conv.InputLex());
                }
                return toReturn;
                })); //maximal input rate
            AddSuffix("OUTPUT", new Suffix<Lexicon<string, double>>(() => {
                var toReturn = new Lexicon<string, double>();
                foreach (ConverterFields conv in Converters)
                {
                    MergeResLex(toReturn, conv.OutputLex());
                }
                return toReturn;
            })); //maximal output rate

            AddSuffix("CONSUME", new Suffix<Lexicon<string, double>>(() => {
                var toReturn = new Lexicon<string, double>();
                foreach (ConverterFields conv in Converters)
                {
                    MergeResLex(toReturn, conv.ConsumeLex());
                }
                return toReturn;
            })); //actual consumption rate
            
            AddSuffix("PRODUCE", new Suffix<Lexicon<string, double>>(() => {
                var toReturn = new Lexicon<string, double>();
                foreach (ConverterFields conv in Converters)
                {
                    MergeResLex(toReturn, conv.ProduceLex());
                }
                return toReturn;
            })); //actual production rate

        }

        private bool HasConverterModule(string convName)
        {
            foreach (ConverterFields module in Converters)
            {
                if (string.Equals(module.Converter.ConverterName, convName, StringComparison.OrdinalIgnoreCase))  { return true; }
            }
            return false;
        }

        private ConverterFields GetConverterModule(string convName)
        {
            foreach (ConverterFields module in Converters)
            {
                if (string.Equals(module.Converter.ConverterName, convName, StringComparison.OrdinalIgnoreCase)) { return module; } 
            }
            throw new KOSException("Resource Converter Module not found: " + convName); //if not found
        }

        private void MergeResLex(Lexicon<string, double> Lex, Lexicon<string, double> toAdd)
        {
            foreach (string res in toAdd.Keys)
            {
                if (Lex.ContainsKey(res)) { Lex[res] = Lex[res] + toAdd[res]; }
                else { Lex.Add(res, toAdd[res]); }
            }
        }

        private string StringAdd(string s, string toAdd, string Sep)
        {
            if (s == "") { return toAdd; }
            else { return  (s + Sep + toAdd); }
        }
        private string LineAdd(string s, string toAdd)
        {
            return StringAdd(s, toAdd, "\n");
        }

    }


}
