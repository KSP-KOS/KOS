using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using System.Collections.Generic;
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
            AddSuffix(new[] { "CONVERTERMODULES", "CONVMODS" }, new Suffix<ListValue<ConverterFields>>(() => Converters, ""));
            AddSuffix(new[] { "HASCONVERTER", "HASCONV" }, new OneArgsSuffix<bool, string>(HasConverterModule));
            AddSuffix(new[] { "GETCONVERTER", "GETCONV" }, new OneArgsSuffix<ConverterFields, string>(GetConverterModule));

            // all suffixes

            AddSuffix(new[] { "CONVERTERNAMES", "CONVNAMES" }, new Suffix<ListValue>(() =>
            {
                var toReturn = new ListValue();
                foreach (ConverterFields conv in Converters) { toReturn.Add(conv.Converter.ConverterName); }
                return toReturn;
            }));

            //AddSuffix(new[] { "CONVERTERNAME", "CONVNAME" }, new Suffix<string>(() => converter.ConverterName));

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
            AddSuffix("STATUSALL", new Suffix<ListValue>(() =>
            {
                var toReturn = new ListValue();
                foreach (ConverterFields conv in Converters) { toReturn.Add(conv.Converter.status); }
                return toReturn;
            }));
            //AddSuffix("STATUS", new Suffix<string>(() => converter.status));
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
                if (Converters.Count == 0) { return 0; }
                return Converters[0].Converter.GetCoreTemperature();
            })); //same for all (from another module)
            AddSuffix(new[] { "GOALTEMPERATURE", "GOALTEMP" }, new Suffix<double>(() => {
                if (Converters.Count == 0) { return 0; }
                return Converters[0].Converter.GetGoalTemperature();
            })); //same for all (from another module)
            //AddSuffix("FILLAMOUNT", new Suffix<float>(() => converter.FillAmount));  //stops when output exceeds this part of storage capacity (fuel cell stop condition).
            //AddSuffix("TAKEAMOUNT", new Suffix<float>(() => converter.TakeAmount));
            //AddSuffix("THERMALEFFICIENCY", new Suffix<float>(() => converter.ThermalEfficiency.Evaluate((float)(converter.GetCoreTemperature()))));
            //          AddSuffix("GETINFO", new Suffix<string>(() => converter.GetInfo()));  //overwrite without extra formatting?
            //AddSuffix("INPUT", new Suffix<Lexicon<string, double>>(() => ResListLex(converter.inputList, 1))); //maximal input rate
            //AddSuffix("OUTPUT", new Suffix<Lexicon<string, double>>(() => ResListLex(converter.outputList, 1)));

            //AddSuffix(new[] { "CONVERTERLOAD", "CONVLOAD" }, new Suffix<float>(() => conversionLoadParse(converter.status))); //actual converter load (fraction of maximal)
            //AddSuffix("CONSUME", new Suffix<Lexicon<string, double>>(() => ResListLex(converter.inputList, conversionLoadParse(converter.status)))); //actual consumption rate
            //AddSuffix("PRODUCE", new Suffix<Lexicon<string, double>>(() => ResListLex(converter.outputList, conversionLoadParse(converter.status)))); //actual production rate

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
    }


}
