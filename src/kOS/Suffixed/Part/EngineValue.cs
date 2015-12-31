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
            AddSuffix("THRUSTLIMIT", new ClampSetSuffix<float>(() => engine.ThrustPercentage,
                                                          value => engine.ThrustPercentage = value,
                                                          0f, 100f, 0f,
                                                          "thrust limit percentage for this engine"));
            AddSuffix("MAXTHRUST", new Suffix<float>(() => engine.MaxThrust));
            AddSuffix("THRUST", new Suffix<float>(() => engine.FinalThrust));
            AddSuffix("FUELFLOW", new Suffix<float>(() => engine.FuelFlow));
            AddSuffix("ISP", new Suffix<float>(() => engine.SpecificImpulse));
            AddSuffix(new[] { "VISP", "VACUUMISP" }, new Suffix<float>(() => engine.VacuumSpecificImpluse));
            AddSuffix(new[] { "SLISP", "SEALEVELISP" }, new Suffix<float>(() => engine.SeaLevelSpecificImpulse));
            AddSuffix("FLAMEOUT", new Suffix<bool>(() => engine.Flameout));
            AddSuffix("IGNITION", new Suffix<bool>(() => engine.Ignition));
            AddSuffix("ALLOWRESTART", new Suffix<bool>(() => engine.AllowRestart));
            AddSuffix("ALLOWSHUTDOWN", new Suffix<bool>(() => engine.AllowShutdown));
            AddSuffix("THROTTLELOCK", new Suffix<bool>(() => engine.ThrottleLock));
            AddSuffix("ISPAT", new OneArgsSuffix<float, double>(GetIspAtAtm));
            AddSuffix("MAXTHRUSTAT", new OneArgsSuffix<float, double>(GetMaxThrustAtAtm));
            AddSuffix("AVAILABLETHRUST", new Suffix<float>(() => engine.AvailableThrust));
            AddSuffix("AVAILABLETHRUSTAT", new OneArgsSuffix<float, double>(GetAvailableThrustAtAtm));
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

        public float GetIspAtAtm(double atmPressure)
        {
            return engine.IspAtAtm(atmPressure);
        }

        public float GetMaxThrustAtAtm(double atmPressure)
        {
            return engine.MaxThrustAtAtm(atmPressure);
        }

        public float GetAvailableThrustAtAtm(double atmPressure)
        {
            return engine.AvailableThrustAtAtm(atmPressure);
        }
    }
}