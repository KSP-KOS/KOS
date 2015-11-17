using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Encapsulation;
using System.Collections.Generic;

namespace kOS.Suffixed.PartModuleField
{
    class ConverterFields : PartModuleFields
    {
        private readonly ModuleResourceConverter converter;

        public ModuleResourceConverter Converter
        { get { return converter; } }

        public ConverterFields(ModuleResourceConverter ConverterModule, SharedObjects sharedObj)
            : base(ConverterModule, sharedObj)
        {
            converter = ConverterModule;
            ConverterModuleInitializeSuffixes();
        }

        private void ConverterModuleInitializeSuffixes()
        {
            AddSuffix(new[] { "CONVERTERNAME", "CONVNAME" }, new Suffix<string>(() => converter.ConverterName));
            AddSuffix("START", new NoArgsSuffix(() => converter.StartResourceConverter()));
            AddSuffix("STOP", new NoArgsSuffix(() => converter.StopResourceConverter()));
            AddSuffix("STATUS", new Suffix<string>(() => converter.status));
            AddSuffix("ISACTIVATED", new SetSuffix<bool>(() => converter.IsActivated, value => toggle(value)));
            AddSuffix("ALWAYSACTIVE", new Suffix<bool>(() => converter.AlwaysActive));
            AddSuffix("GENERATESHEAT", new Suffix<bool>(() => converter.GeneratesHeat));
            AddSuffix(new[] { "CORETEMPERATURE", "CORETEMP" }, new Suffix<double>(() => converter.GetCoreTemperature()));
            AddSuffix(new[] { "GOALTEMPERATURE", "GOALTEMP" }, new Suffix<double>(() => converter.GetGoalTemperature())); //optimal temp (returns current temp, if temp independent)
            AddSuffix("FILLAMOUNT", new Suffix<float>(() => converter.FillAmount));  //stops when output exceeds this part of storage capacity (fuel cell stop condition).
            AddSuffix("TAKEAMOUNT", new Suffix<float>(() => converter.TakeAmount));
            AddSuffix("THERMALEFFICIENCY", new Suffix<float>(() => converter.ThermalEfficiency.Evaluate((float)(converter.GetCoreTemperature()))));
            //          AddSuffix("GETINFO", new Suffix<string>(() => converter.GetInfo()));  //overwrite without extra formatting?
            AddSuffix("INPUT", new Suffix<Lexicon<string, double>>(() => ResListLex(converter.inputList, 1))); //maximal input rate
            AddSuffix("OUTPUT", new Suffix<Lexicon<string, double>>(() => ResListLex(converter.outputList, 1)));

            AddSuffix(new[] { "CONVERTERLOAD", "CONVLOAD" }, new Suffix<float>(() => conversionLoadParse(converter.status))); //actual converter load (fraction of maximal)
            AddSuffix("CONSUME", new Suffix<Lexicon<string, double>>(() => ResListLex(converter.inputList, conversionLoadParse(converter.status)))); //actual consumption rate
            AddSuffix("PRODUCE", new Suffix<Lexicon<string, double>>(() => ResListLex(converter.outputList, conversionLoadParse(converter.status)))); //actual production rate
        }

        public void toggle(bool value)
        {
            if (value) { converter.StartResourceConverter(); } else { converter.StopResourceConverter(); }
        }

        public Lexicon<string, double> ResListLex(List<ResourceRatio> resList, float rate)
        {
            var toReturn = new Lexicon<string, double>();
            foreach (ResourceRatio rr in resList)
            {
                toReturn.Add(rr.ResourceName, rr.Ratio * rate);
            }
            return toReturn;
        }

        public float conversionLoadParse(string status)
        {
            if (status.EndsWith("% load"))
            {
                try { return float.Parse(status.Remove(status.Length - 6)) / 100; }
                catch { return 0; }
            }
            else { return 0; }
        }



    }
}
