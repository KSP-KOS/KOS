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
            AddSuffix("THRUSTLIMIT", new SetSuffix<IModuleEngine,float>(engine, model => model.ThrustPercentage, (model, value) => model.ThrustPercentage = value));
            AddSuffix("MAXTHRUST", new Suffix<IModuleEngine,float>(engine, model => model.MaxThrust));
            AddSuffix("THRUST", new Suffix<IModuleEngine,float>(engine, model => model.FinalThrust));
            AddSuffix("FUELFLOW", new Suffix<IModuleEngine,float>(engine, model => model.FuelFlow));
            AddSuffix("ISP", new Suffix<IModuleEngine,float>(engine, model => model.SpecificImpulse));
            AddSuffix("FLAMEOUT", new Suffix<IModuleEngine,bool>(engine, model => model.Flameout));
            AddSuffix("IGNITION", new Suffix<IModuleEngine,bool>(engine, model => model.Ignition));
            AddSuffix("ALLOWRESTART", new Suffix<IModuleEngine,bool>(engine, model => model.AllowRestart));
            AddSuffix("ALLOWSHUTDOWN", new Suffix<IModuleEngine,bool>(engine, model => model.AllowShutdown));
            AddSuffix("THROTTLELOCK", new Suffix<IModuleEngine,bool>(engine, model => model.ThrottleLock));
        }

        public new static ListValue PartsToList(IEnumerable<global::Part> parts, SharedObjects sharedObj)
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