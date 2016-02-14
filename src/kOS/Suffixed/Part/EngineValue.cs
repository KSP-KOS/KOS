using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Part;
using kOS.Safe.Encapsulation.Suffixes;
using System.Collections.Generic;

namespace kOS.Suffixed.Part
{
    [kOS.Safe.Utilities.KOSNomenclature("Engine")]
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
            AddSuffix("ACTIVATE", new NoArgsVoidSuffix(() => engine.Activate()));
            AddSuffix("SHUTDOWN", new NoArgsVoidSuffix(() => engine.Shutdown()));
            AddSuffix("THRUSTLIMIT", new ClampSetSuffix<ScalarValue>(() => engine.ThrustPercentage,
                                                          value => engine.ThrustPercentage = value,
                                                          0f, 100f, 0f,
                                                          "thrust limit percentage for this engine"));
            AddSuffix("MAXTHRUST", new Suffix<ScalarValue>(() => engine.MaxThrust));
            AddSuffix("THRUST", new Suffix<ScalarValue>(() => engine.FinalThrust));
            AddSuffix("FUELFLOW", new Suffix<ScalarValue>(() => engine.FuelFlow));
            AddSuffix("ISP", new Suffix<ScalarValue>(() => engine.SpecificImpulse));
            AddSuffix(new[] { "VISP", "VACUUMISP" }, new Suffix<ScalarValue>(() => engine.VacuumSpecificImpluse));
            AddSuffix(new[] { "SLISP", "SEALEVELISP" }, new Suffix<ScalarValue>(() => engine.SeaLevelSpecificImpulse));
            AddSuffix("FLAMEOUT", new Suffix<BooleanValue>(() => engine.Flameout));
            AddSuffix("IGNITION", new Suffix<BooleanValue>(() => engine.Ignition));
            AddSuffix("ALLOWRESTART", new Suffix<BooleanValue>(() => engine.AllowRestart));
            AddSuffix("ALLOWSHUTDOWN", new Suffix<BooleanValue>(() => engine.AllowShutdown));
            AddSuffix("THROTTLELOCK", new Suffix<BooleanValue>(() => engine.ThrottleLock));
            AddSuffix("ISPAT", new OneArgsSuffix<ScalarValue, ScalarValue>(GetIspAtAtm));
            AddSuffix("MAXTHRUSTAT", new OneArgsSuffix<ScalarValue, ScalarValue>(GetMaxThrustAtAtm));
            AddSuffix("AVAILABLETHRUST", new Suffix<ScalarValue>(() => engine.AvailableThrust));
            AddSuffix("AVAILABLETHRUSTAT", new OneArgsSuffix<ScalarValue, ScalarValue>(GetAvailableThrustAtAtm));
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

        public ScalarValue GetIspAtAtm(ScalarValue atmPressure)
        {
            return engine.IspAtAtm(atmPressure);
        }

        public ScalarValue GetMaxThrustAtAtm(ScalarValue atmPressure)
        {
            return engine.MaxThrustAtAtm(atmPressure);
        }

        public ScalarValue GetAvailableThrustAtAtm(ScalarValue atmPressure)
        {
            return engine.AvailableThrustAtAtm(atmPressure);
        }
    }
}