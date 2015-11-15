using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Part;
using kOS.Safe.Encapsulation.Suffixes;
using System.Collections.Generic;
using kOS.Safe.Exceptions;

namespace kOS.Suffixed.Part
{
    public class EngineValue : PartValue
    {
        private IModuleEngine engine 
        {get {
                if ((!MultiMode)||(MMengine.runningPrimary)) { return engine1; }
                else { return engine2; }
            }
         }

        private readonly IModuleEngine engine1;
        private readonly IModuleEngine engine2; 
        private readonly MultiModeEngine MMengine; //multimodeengine module (null if not multimode)
        private readonly bool MultiMode;
        private readonly GimbalValue gimbal;
        private readonly bool HasGimbal;


        public EngineValue(global::Part part, IModuleEngine engine, SharedObjects sharedObj)
            : base(part, sharedObj)
        {
            MMengine = null;
            engine1 = engine;
            MultiMode = false;
            ModuleGimbal gimbalModule = findGimbal();
            if (gimbalModule != null) { HasGimbal = true; gimbal = new GimbalValue(gimbalModule, sharedObj); }
            else { HasGimbal = false; };
            EngineInitializeSuffixes();

        }

        public EngineValue(global::Part part, MultiModeEngine engine, SharedObjects sharedObj)
    : base(part, sharedObj)
        {
            MMengine = engine;

            foreach (PartModule module in this.Part.Modules)
            {
                var engines = module as ModuleEngines;
                if ((engines != null) && (engines.engineID == MMengine.primaryEngineID))
                {
                    engine1 = new ModuleEngineAdapter(engines);
                }
                if ((engines != null) && (engines.engineID == MMengine.secondaryEngineID))
                {
                    engine2 = new ModuleEngineAdapter(engines);
                }
                var enginesFX = module as ModuleEnginesFX;
                if ((enginesFX != null) && (enginesFX.engineID == MMengine.primaryEngineID))
                {
                    engine1 = new ModuleEngineAdapter(enginesFX);
                }
                if ((enginesFX != null) && (enginesFX.engineID == MMengine.secondaryEngineID))
                {
                    engine2 = new ModuleEngineAdapter(enginesFX);
                }

            } 
            // throw exception if not found
            if (engine1 == null) { throw new KOSException("Engine module error " + MMengine.primaryEngineID); }
            if (engine2 == null) { throw new KOSException("Engine module error " + MMengine.secondaryEngineID); }
            MultiMode = true;

            ModuleGimbal gimbalModule = findGimbal();
            if (gimbalModule != null) { HasGimbal = true; gimbal = new GimbalValue(gimbalModule, sharedObj); }
            else { HasGimbal = false; };

            EngineInitializeSuffixes();
        }


        private ModuleGimbal findGimbal()
        {
            foreach (PartModule module in Part.Modules)
            {
                var gimbalModule = module as ModuleGimbal;
                if (gimbalModule != null)
                {
                    return gimbalModule;
                }
            }
            return null;
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
            //MultiMode features
            AddSuffix("MULTIMODE", new Suffix<bool>(() => MultiMode));
            AddSuffix("MODES", new Suffix<ListValue>(GetAllModes, "A List of all modes of this engine"));
            if (MultiMode)
            {
                AddSuffix("MODE", new Suffix<string>(() => MMengine.mode));
                AddSuffix("TOGGLEMODE", new NoArgsSuffix(() => ToggleMode() ));
                AddSuffix("PRIMARYMODE", new SetSuffix<bool>(() => MMengine.runningPrimary, value =>     ToggleSetMode(value)));
                AddSuffix("AUTOSWITCH", new SetSuffix<bool>(() => MMengine.autoSwitch, value => SetAutoswitch(value)));  
            }
            //gimbal interface
            AddSuffix("HASGIMBAL", new Suffix<bool>(() => HasGimbal));
            if (HasGimbal)
            {
                AddSuffix("GIMBAL", new Suffix<GimbalValue>(() => gimbal));
            }

        }

        public static ListValue PartsToList(IEnumerable<global::Part> parts, SharedObjects sharedObj)
        {
            var toReturn = new ListValue();
            foreach (var part in parts)
            {
                bool ismultimode = false;
                foreach (PartModule module in part.Modules)
                {
                    var enginesMM = module as MultiModeEngine;

                    if (enginesMM != null)
                    {
                        toReturn.Add(new EngineValue(part, enginesMM, sharedObj));
                        ismultimode = true;
                    }
                }
                if (!ismultimode) {
                    foreach (PartModule module in part.Modules)
                    {
                        var engines = module as ModuleEngines;
                        if (engines != null)
                        {
                            toReturn.Add(new EngineValue(part, new ModuleEngineAdapter(engines), sharedObj));
                        }
                        else
                        {
                            var enginesFX = module as ModuleEnginesFX;
                            if (engines != null)
                            {
                                toReturn.Add(new EngineValue(part, new ModuleEngineAdapter(enginesFX), sharedObj));
                            }
                        }
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

        public ListValue GetAllModes()
        {
            var toReturn = new ListValue();
            if (MultiMode) { toReturn.Add(MMengine.primaryEngineID); toReturn.Add(MMengine.secondaryEngineID); }
            else { toReturn.Add("Single mode"); }

            return toReturn;
        }

        public void ToggleMode()
        {
            MMengine.Invoke("ModeEvent", 0);
        }

        public void ToggleSetMode(bool prim)
        {
            if (prim != MMengine.runningPrimary) { ToggleMode(); }
        }

        public void SetAutoswitch(bool auto)
        {
            if (auto) { MMengine.Invoke("EnableAutoSwitch", 0); }
            else { MMengine.Invoke("DisableAutoSwitch", 0); }
        }

    }
}
