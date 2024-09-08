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
            AddSuffix("ISRUNNING", new Suffix<bool>(() => converter.status.EndsWith("% load"))); //That's the only indication they give us
            AddSuffix("ALWAYSACTIVE", new Suffix<bool>(() => converter.AlwaysActive));
            AddSuffix("GENERATESHEAT", new Suffix<bool>(() => converter.GeneratesHeat));
            AddSuffix(new[] { "CORETEMPERATURE", "CORETEMP" }, new Suffix<double>(() => converter.GetCoreTemperature()));
            AddSuffix(new[] { "GOALTEMPERATURE", "GOALTEMP" }, new Suffix<double>(() => converter.GetGoalTemperature())); //optimal temp (returns current temp, if temp independent)
            AddSuffix("FILLAMOUNT", new Suffix<float>(() => converter.FillAmount));  //stops when output exceeds this part of storage capacity (fuel cell stop condition).
            AddSuffix("TAKEAMOUNT", new Suffix<float>(() => converter.TakeAmount));
            AddSuffix(new[] { "THERMALEFFICIENCY", "THERMEFF" }, new Suffix<float>(() => ThermEff()));
            AddSuffix("GETINFO", new Suffix<string>(() => writeInfo()));  
            AddSuffix("INPUT", new Suffix<Lexicon<string, double>>(() => InputLex())); //maximal input rate
            AddSuffix("OUTPUT", new Suffix<Lexicon<string, double>>(() => OutputLex()));

            AddSuffix(new[] { "CONVERTERLOAD", "CONVLOAD" }, new Suffix<float>(() => conversionLoad())); //actual converter load (fraction of maximal)
            AddSuffix("CONSUME", new Suffix<Lexicon<string, double>>(() => ConsumeLex())); //actual consumption rate
            AddSuffix("PRODUCE", new Suffix<Lexicon<string, double>>(() => ProduceLex())); //actual production rate
        }

        public void toggle(bool value)
        {
            if (value) { converter.StartResourceConverter(); } else { converter.StopResourceConverter(); }
        }

        private Lexicon<string, double> ResListLex(List<ResourceRatio> resList, float rate)
        {
            var toReturn = new Lexicon<string, double>();
            foreach (ResourceRatio rr in resList)
            {
                toReturn.Add(rr.ResourceName, rr.Ratio * rate);
            }
            return toReturn;
        }

        public float ThermEff()
        {
            return converter.ThermalEfficiency.Evaluate((float)(converter.GetCoreTemperature()));
        }

        public Lexicon<string, double> InputLex()
        {
            return ResListLex(converter.inputList, 1);
        }

        public Lexicon<string, double> OutputLex()
        {
            return ResListLex(converter.outputList, 1);
        }

        public Lexicon<string, double> ConsumeLex()
        {
            return ResListLex(converter.inputList, conversionLoad()); 
        }

        public Lexicon<string, double> ProduceLex()
        {
            return ResListLex(converter.outputList, conversionLoad());
        }

        public float conversionLoad()
        {
            return conversionLoadParse(converter.status);
        }
        private float conversionLoadParse(string status)
        {
            if (status.EndsWith("% load"))
            {
                try { return float.Parse(status.Remove(status.Length - 6)) / 100; }
                catch { return 0; }
            }
            else { return 0; }
        }

        public string writeInfo()
        {
            string toReturn = converter.ConverterName + ": "+ converter.status;
            float CL = conversionLoad();
            if (CL == 0)
            {
                toReturn = LineAdd(toReturn, "  Consumes:");
                foreach (ResourceRatio rr in converter.inputList)
                {
                    toReturn = LineAdd(toReturn, "    "+ rr.ResourceName+"  ("+ (rr.Ratio).ToString("F2")+"/sec)");
                }
                toReturn = LineAdd(toReturn, "  Produces:");
                foreach (ResourceRatio rr in converter.outputList)
                {
                    toReturn = LineAdd(toReturn, "    " + rr.ResourceName + "  (" + (rr.Ratio).ToString("F2") + "/sec)");
                }
            }
            else
            {
                toReturn = LineAdd(toReturn, "  Consumes:");
                foreach (ResourceRatio rr in converter.inputList)
                {
                    toReturn = LineAdd(toReturn, "    " + rr.ResourceName + ": " + (rr.Ratio*CL).ToString("F2") + "/sec  (" + (rr.Ratio).ToString("F2") + "/sec)");
                }
                toReturn = LineAdd(toReturn, "  Produces:");
                foreach (ResourceRatio rr in converter.outputList)
                {
                    toReturn = LineAdd(toReturn, "    " + rr.ResourceName + ": " + (rr.Ratio * CL).ToString("F2") + "/sec  (" + (rr.Ratio).ToString("F2") + "/sec)");
                }
            }
            return toReturn;
        }
        private string StringAdd(string s, string toAdd, string Sep)
        {
            if (s == "") { return toAdd; }
            else { return (s + Sep + toAdd); }
        }
        private string LineAdd(string s, string toAdd)
        {
            return StringAdd(s, toAdd, "\n");
        }
    }
}
