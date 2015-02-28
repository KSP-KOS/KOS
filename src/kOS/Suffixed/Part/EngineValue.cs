using kOS.Safe.Encapsulation;
using System.Collections.Generic;
using kOS.Safe.Encapsulation.Part;
using kOS.Safe.Encapsulation.Suffixes;

namespace kOS.Suffixed.Part
{
    public class EngineValue : PartValue
    {
        private readonly IModuleEngine engine;

        public EngineValue(global::Part part, IModuleEngine engine, SharedObjects sharedObj)
            : base(part, sharedObj)
        {
            this.engine = engine;
            EngineInitializeSuffixes();
        }

        private void EngineInitializeSuffixes()
        {
            AddSuffix("ACTIVATE", new NoArgsSuffix(() => engine.Activate()));
            AddSuffix("SHUTDOWN", new NoArgsSuffix(() => engine.Shutdown()));
            AddSuffix("THRUSTLIMIT", new ClampSetSuffix<float>(() => engine.ThrustPercentage, value => engine.ThrustPercentage = value, 0, 100, 0.5f));
            AddSuffix("MAXTHRUST", new Suffix<float>(() => engine.MaxThrust));
            AddSuffix("THRUST", new Suffix<float>(() => engine.FinalThrust));
            AddSuffix("FUELFLOW", new Suffix<float>(() => engine.FuelFlow));
            AddSuffix("ISP", new Suffix<float>(() => engine.SpecificImpulse));
            AddSuffix(new[] {"VISP", "VACUUMISP"}, new Suffix<float>(() => engine.VacuumSpecificImpluse));
            AddSuffix(new[] {"SLISP", "SEALEVELISP"}, new Suffix<float>(() => engine.SeaLeveSpecificImpulse));
            AddSuffix("FLAMEOUT", new Suffix<bool>(() => engine.Flameout));
            AddSuffix("IGNITION", new Suffix<bool>(() => engine.Ignition));
            AddSuffix("ALLOWRESTART", new Suffix<bool>(() => engine.AllowRestart));
            AddSuffix("ALLOWSHUTDOWN", new Suffix<bool>(() => engine.AllowShutdown));
            AddSuffix("THROTTLELOCK", new Suffix<bool>(() => engine.ThrottleLock));
        }

        public static ListValue PartsToList(IEnumerable<global::Part> parts, SharedObjects sharedObj)
        {
            var toReturn = new ListValue();
            foreach (var part in parts)
            {
                foreach (PartModule module in part.Modules)
                {
                    var engineModule = module as ModuleEngines;
                    var engineModuleFx = module as ModuleEnginesFX;
                    if (engineModuleFx != null)
                    {
                        toReturn.Add(new EngineValue(part, new ModuleEngineAdapter(engineModuleFx), sharedObj));
                    }
                    else if (engineModule != null)
                    {
                        toReturn.Add(new EngineValue(part, new ModuleEngineAdapter(engineModule), sharedObj));
                    }
                }
            }
            return toReturn;
        }
    }
}