using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Part;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using kOS.Suffixed.PartModuleField;
using System.Collections.Generic;
using System.Linq;

namespace kOS.Suffixed.Part
{
    public class EngineValue : PartValue
    {
        private IModuleEngine engine
        {
            get
            {
                if ((!MultiMode) || (MMengine.runningPrimary)) { return engine1; }
                else { return engine2; }
            }
        }

        private readonly IModuleEngine engine1;
        private readonly IModuleEngine engine2;
        private readonly MultiModeEngine MMengine;
        private readonly bool MultiMode = false;
        private GimbalFields gimbal;
        private bool HasGimbal = false;

        public EngineValue(global::Part part, IModuleEngine engine, SharedObjects sharedObj)
            : base(part, sharedObj)
        {
            engine1 = engine;

            findGimbal();

            EngineInitializeSuffixes();
        }

        public EngineValue(global::Part part, MultiModeEngine engine, SharedObjects sharedObj)
            : base(part, sharedObj)
        {
            MMengine = engine;

            var moduleEngines = part.Modules.GetModules<ModuleEngines>();
            if (moduleEngines.Count == 2)
            {
                var modEngine1 = moduleEngines.Where(e => e.engineID == MMengine.primaryEngineID).FirstOrDefault();
                if (modEngine1 != null)
                    engine1 = new ModuleEngineAdapter(modEngine1);
                else
                    throw new KOSException("Attempted to build a MultiModeEngine with no engine matching Primary ID");
                var modEngine2 = moduleEngines.Where(e => e.engineID == MMengine.secondaryEngineID).FirstOrDefault();
                if (modEngine2 != null)
                    engine2 = new ModuleEngineAdapter(modEngine2);
                else
                    throw new KOSException("Attempted to build a MultiModeEngine with no engine matching Secondary ID");
            }
            else
            {
                throw new KOSException(string.Format("Attempted to build a MultiModeEngine with {0} engine modules defined instead of 2", moduleEngines.Count));
            }

            MultiMode = true;

            findGimbal();

            EngineInitializeSuffixes();
        }

        private void findGimbal()
        {
            // if the part definition includes a ModuleGimbal, create GimbalFields and set HasGimbal to true
            var gimbalModule = Part.Modules.GetModules<ModuleGimbal>().FirstOrDefault();
            if (gimbalModule != null)
            {
                HasGimbal = true;
                gimbal = new GimbalFields(gimbalModule, Shared);
            }
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
            //MultiMode features
            AddSuffix("MULTIMODE", new Suffix<BooleanValue>(() => MultiMode));
            AddSuffix("MODES", new Suffix<ListValue>(GetAllModes, "A List of all modes of this engine"));
            AddSuffix("MODE", new Suffix<StringValue>(GetCurrentMode));
            AddSuffix("TOGGLEMODE", new NoArgsVoidSuffix(ToggleMode));
            AddSuffix("PRIMARYMODE", new SetSuffix<BooleanValue>(GetRunningPrimary, SetRunningPrimary));
            AddSuffix("AUTOSWITCH", new SetSuffix<BooleanValue>(GetAutoSwitch, SetAutoswitch));
            //gimbal interface
            AddSuffix("HASGIMBAL", new Suffix<BooleanValue>(() => HasGimbal));
            AddSuffix("GIMBAL", new Suffix<GimbalFields>(GetGimbal));
        }

        public static ListValue PartsToList(IEnumerable<global::Part> parts, SharedObjects sharedObj)
        {
            var toReturn = new ListValue();
            foreach (var part in parts)
            {
                var multiModeEngines = part.Modules.GetModules<MultiModeEngine>();
                if (multiModeEngines.Count > 0)
                    toReturn.Add(new EngineValue(part, multiModeEngines.First(), sharedObj));
                else
                {
                    var moduleEngines = part.Modules.GetModules<ModuleEngines>();
                    if (moduleEngines.Count > 0)
                        toReturn.Add(new EngineValue(part, new ModuleEngineAdapter(moduleEngines.First()), sharedObj));
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

        public ListValue GetAllModes()
        {
            var toReturn = new ListValue();
            if (MultiMode)
            {
                toReturn.Add(new StringValue(MMengine.primaryEngineID));
                toReturn.Add(new StringValue(MMengine.secondaryEngineID));
            }
            else
            {
                toReturn.Add(new StringValue("Single mode"));
            }

            return toReturn;
        }

        public void ToggleMode()
        {
            if (!MultiMode)
                throw new KOSException("Attempted to call the TOGGLEMODE suffix on a non-multi mode engine.");
            // Use Invoke to call ModeEvent, since the underlying method is private.
            MMengine.Invoke("ModeEvent", 0);
        }

        public BooleanValue GetRunningPrimary()
        {
            if (!MultiMode)
                throw new KOSException("Attempted to get the PRIMARYMODE suffix on a non-multi mode engine.");
            return MMengine.runningPrimary;
        }

        public void SetRunningPrimary(BooleanValue prim)
        {
            if (!MultiMode)
                throw new KOSException("Attempted to set the PRIMARYMODE suffix on a non-multi mode engine.");
            // If runningPrimary does not match prim, call ToggleMode
            if (prim != MMengine.runningPrimary)
                ToggleMode();
        }

        public BooleanValue GetAutoSwitch()
        {
            if (!MultiMode)
                throw new KOSException("Attempted to get the AUTOSWITCH suffix on a non-multi mode engine.");
            return MMengine.autoSwitch;
        }

        public void SetAutoswitch(BooleanValue auto)
        {
            if (!MultiMode)
                throw new KOSException("Attempted to set the AUTOSWITCH suffix on a non-multi mode engine.");
            // if autoSwitch doesn't equal auto, use invoke to call the autoswitch method because the method is private
            if (MMengine.autoSwitch != auto)
            {
                if (auto)
                    MMengine.Invoke("EnableAutoSwitch", 0);
                else
                    MMengine.Invoke("DisableAutoSwitch", 0);
            }
        }

        public StringValue GetCurrentMode()
        {
            if (!MultiMode)
                throw new KOSException("Attempted to get the MODE suffix on a non-multi mode engine.");
            return MMengine.mode;
        }

        public GimbalFields GetGimbal()
        {
            if (gimbal != null)
                return gimbal;
            throw new KOSException("Attempted to get the GIMBAL suffix on an engine that does not have a gimbal.");
        }
    }
}