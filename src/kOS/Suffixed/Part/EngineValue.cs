using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Part;
using kOS.Safe.Encapsulation.Suffixes;
using System.Collections.Generic;

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
            AddSuffix("THRUSTLIMIT", new ClampSetSuffix<ScalarDoubleValue>(() => engine.ThrustPercentage,
                                                          value => engine.ThrustPercentage = value,
                                                          0f, 100f, 0f,
                                                          "thrust limit percentage for this engine"));
            AddSuffix("MAXTHRUST", new Suffix<ScalarDoubleValue>(() => engine.MaxThrust));
            AddSuffix("THRUST", new Suffix<ScalarDoubleValue>(() => engine.FinalThrust));
            AddSuffix("FUELFLOW", new Suffix<ScalarDoubleValue>(() => engine.FuelFlow));
            AddSuffix("ISP", new Suffix<ScalarDoubleValue>(() => engine.SpecificImpulse));
            AddSuffix(new[] { "VISP", "VACUUMISP" }, new Suffix<ScalarDoubleValue>(() => engine.VacuumSpecificImpluse));
            AddSuffix(new[] { "SLISP", "SEALEVELISP" }, new Suffix<ScalarDoubleValue>(() => engine.SeaLevelSpecificImpulse));
            AddSuffix("FLAMEOUT", new Suffix<BooleanValue>(() => engine.Flameout));
            AddSuffix("IGNITION", new Suffix<BooleanValue>(() => engine.Ignition));
            AddSuffix("ALLOWRESTART", new Suffix<BooleanValue>(() => engine.AllowRestart));
            AddSuffix("ALLOWSHUTDOWN", new Suffix<BooleanValue>(() => engine.AllowShutdown));
            AddSuffix("THROTTLELOCK", new Suffix<BooleanValue>(() => engine.ThrottleLock));
            AddSuffix("ISPAT", new OneArgsSuffix<ScalarDoubleValue, ScalarDoubleValue>(GetIspAtAtm));
            AddSuffix("MAXTHRUSTAT", new OneArgsSuffix<ScalarDoubleValue, ScalarDoubleValue>(GetMaxThrustAtAtm));
            AddSuffix("AVAILABLETHRUST", new Suffix<ScalarDoubleValue>(() => engine.AvailableThrust));
            AddSuffix("AVAILABLETHRUSTAT", new OneArgsSuffix<ScalarDoubleValue, ScalarDoubleValue>(GetAvailableThrustAtAtm));
        }

        public static ListValue PartsToList(IEnumerable<global::Part> parts, SharedObjects sharedObj)
        {
            var toReturn = new ListValue();
            foreach (var part in parts)
            {
                foreach (PartModule module in part.Modules)
                {
                    var engines = module as ModuleEngines;
                    if (engines != null)
                    {
                        toReturn.Add(new EngineValue(part, new ModuleEngineAdapter(engines), sharedObj));
                    }
                }
            }
            return toReturn;
        }

        public ScalarDoubleValue GetIspAtAtm(ScalarDoubleValue atmPressure)
        {
            return engine.IspAtAtm(atmPressure);
        }

        public ScalarDoubleValue GetMaxThrustAtAtm(ScalarDoubleValue atmPressure)
        {
            return engine.MaxThrustAtAtm(atmPressure);
        }

        public ScalarDoubleValue GetAvailableThrustAtAtm(ScalarDoubleValue atmPressure)
        {
            return engine.AvailableThrustAtAtm(atmPressure);
        }
    }
}