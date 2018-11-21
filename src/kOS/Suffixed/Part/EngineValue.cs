using kOS.Safe;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using kOS.Safe.Utilities;
using kOS.Suffixed.PartModuleField;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace kOS.Suffixed.Part
{
    [kOS.Safe.Utilities.KOSNomenclature("Engine")]
    public class EngineValue : PartValue
    {
        public ModuleEngines Engine1 { get; }
        public ModuleEngines Engine2 { get; }
        public MultiModeEngine Multi { get; }
        public GimbalFields Gimbal { get; }

        public ModuleEngines Engine =>
            Engine2 == null || Multi.runningPrimary ? Engine1 : Engine2;
        public bool MultiMode => Engine2 != null;
        public bool HasGimbal => Gimbal != null;

        /// <summary>
        /// Do not call! VesselTarget.ConstructPart uses this, would use `friend VesselTarget` if this was C++!
        /// </summary>
        internal EngineValue(SharedObjects shared, global::Part part, PartValue parent, DecouplerValue decoupler)
            : base(shared, part, parent, decoupler)
        {
            foreach (var module in part.Modules)
            {
                var mme = module as MultiModeEngine;
                var e = module as ModuleEngines;
                if (mme != null)
                {
                    if (Multi != null)
                        Multi = mme;
                    else
                        SafeHouse.Logger.LogWarning("Multiple MultiModeEngine on {0}: {1}", part.name, part.partInfo.title);
                }
                else if (e != null)
                {
                    if (Engine1 == null)
                        Engine1 = e;
                    else if (Engine2 == null)
                        Engine2 = e;
                    else
                        SafeHouse.Logger.LogWarning("Third engine on {0}: {1}", part.name, part.partInfo.title);
                }
            }
            if (Engine1 == null)
                throw new KOSException("Attempted to build an Engine from part with no ModuleEngines on {0}: {1}", part.name, part.partInfo.title);
            if (Engine2 == null)
            {
                if (Multi != null)
                    SafeHouse.Logger.LogWarning("MultiModeEngine without second engine on {0}: {1}", part.name, part.partInfo.title);
            }
            else
            {
                if (Multi == null)
                {
                    Engine2 = null;
                    SafeHouse.Logger.LogWarning("Second engine without multi-mode on {0}: {1}", part.name, part.partInfo.title);
                }
                else
                {
                    if (Multi.primaryEngineID == Engine2.engineID)
                    {
                        var tmp = Engine1;
                        Engine1 = Engine2;
                        Engine2 = tmp;
                    }
                    else if (Multi.primaryEngineID != Engine1.engineID)
                        SafeHouse.Logger.LogWarning("Primary engine ID={0} does not match multi.e1={1} on {2}: {3}",
                            Engine1.engineID, Multi.primaryEngineID, part.name, part.partInfo.title);
                    if (Multi.secondaryEngineID != Engine2.engineID)
                        SafeHouse.Logger.LogWarning("Secondary engine ID={0} does not match multi.e2={1} on {2}: {3}",
                            Engine2.engineID, Multi.secondaryEngineID, part.name, part.partInfo.title);
                }
            }

            // if the part definition includes a ModuleGimbal, create GimbalFields and set HasGimbal to true
            var gimbalModule = Part.Modules.GetModules<ModuleGimbal>().FirstOrDefault();
            if (gimbalModule != null)
                Gimbal = new GimbalFields(gimbalModule, Shared);

            RegisterInitializer(InitializeSuffixes);
        }
        private void InitializeSuffixes()
        {
            AddSuffix("ACTIVATE", new NoArgsVoidSuffix(Activate));
            AddSuffix("SHUTDOWN", new NoArgsVoidSuffix(Shutdown));
            AddSuffix("THRUSTLIMIT", new ClampSetSuffix<ScalarValue>(() => Engine.thrustPercentage,
                                                          value => Engine.thrustPercentage = value,
                                                          0f, 100f, 0f,
                                                          "thrust limit percentage for this engine"));
            AddSuffix("MAXTHRUST", new Suffix<ScalarValue>(() => Engine.GetThrust()));
            AddSuffix("THRUST", new Suffix<ScalarValue>(() => Engine.finalThrust));
            AddSuffix("FUELFLOW", new Suffix<ScalarValue>(() => Engine.fuelFlowGui));
            AddSuffix("ISP", new Suffix<ScalarValue>(() => Engine.realIsp));
            AddSuffix(new[] { "VISP", "VACUUMISP" }, new Suffix<ScalarValue>(() => Engine.atmosphereCurve.Evaluate(0)));
            AddSuffix(new[] { "SLISP", "SEALEVELISP" }, new Suffix<ScalarValue>(() => Engine.atmosphereCurve.Evaluate(1)));
            AddSuffix("FLAMEOUT", new Suffix<BooleanValue>(() => Engine.flameout));
            AddSuffix("IGNITION", new Suffix<BooleanValue>(() => Engine.getIgnitionState));
            AddSuffix("ALLOWRESTART", new Suffix<BooleanValue>(() => Engine.allowRestart));
            AddSuffix("ALLOWSHUTDOWN", new Suffix<BooleanValue>(() => Engine.allowShutdown));
            AddSuffix("THROTTLELOCK", new Suffix<BooleanValue>(() => Engine.throttleLocked));
            AddSuffix("ISPAT", new OneArgsSuffix<ScalarValue, ScalarValue>(GetIspAtAtm));
            AddSuffix("MAXTHRUSTAT", new OneArgsSuffix<ScalarValue, ScalarValue>(GetMaxThrustAtAtm));
            AddSuffix("AVAILABLETHRUST", new Suffix<ScalarValue>(() => Engine.GetThrust(useThrustLimit: true)));
            AddSuffix("AVAILABLETHRUSTAT", new OneArgsSuffix<ScalarValue, ScalarValue>(GetAvailableThrustAtAtm));
            AddSuffix("POSSIBLETHRUST", new Suffix<ScalarValue>(() => Engine.GetThrust(useThrustLimit: true, operational: false)));
            AddSuffix("POSSIBLETHRUSTAT", new OneArgsSuffix<ScalarValue, ScalarValue>(GetPossibleThrustAtAtm));
            AddSuffix("MAXPOSSIBLETHRUST", new Suffix<ScalarValue>(() => Engine.GetThrust(operational: false)));
            AddSuffix("MAXPOSSIBLETHRUSTAT", new OneArgsSuffix<ScalarValue, ScalarValue>(atm => Engine.GetThrust(atm, operational: false)));
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
            var vessel = VesselTarget.CreateOrGetExisting(sharedObj);
            foreach (var part in parts)
            {
                foreach (var module in part.Modules)
                {
                    if (module is IEngineStatus)
                        toReturn.Add(vessel[part]);
                }
            }
            return toReturn;
        }

        public void Activate()
        {
            ThrowIfNotCPUVessel();
            Engine.Activate();
        }

        public void Shutdown()
        {
            ThrowIfNotCPUVessel();
            Engine.Shutdown();
        }

        public ScalarValue GetIspAtAtm(ScalarValue atmPressure) =>
            Engine.GetIsp(atmPressure.GetDoubleValue());
        public ScalarValue GetMaxThrustAtAtm(ScalarValue atmPressure) =>
            Engine.GetThrust(atmPressure);
        public ScalarValue GetAvailableThrustAtAtm(ScalarValue atmPressure) =>
            Engine.GetThrust(atmPressure, useThrustLimit: true);
        public ScalarValue GetPossibleThrustAtAtm(ScalarValue atmPressure) =>
            Engine.GetThrust(atmPressure, useThrustLimit: true, operational: false);

        public ListValue GetAllModes()
        {
            var toReturn = new ListValue();
            if (MultiMode)
            {
                toReturn.Add(new StringValue(Multi.primaryEngineID));
                toReturn.Add(new StringValue(Multi.secondaryEngineID));
            }
            else
            {
                toReturn.Add(new StringValue("Single mode"));
            }

            return toReturn;
        }

        public void ToggleMode()
        {
            ThrowIfNotCPUVessel();
            if (!MultiMode)
                throw new KOSException("Attempted to call the TOGGLEMODE suffix on a non-multi mode engine.");
            // Use Invoke to call ModeEvent, since the underlying method is private.
            // partModule.Invoke(eventName, 0) introduces 1-frame delay. Getting the event directly does it immediately.
            Multi.Events.First(kspEvent => kspEvent.name == "ModeEvent").Invoke();
        }

        public BooleanValue GetRunningPrimary()
        {
            if (!MultiMode)
                throw new KOSException("Attempted to get the PRIMARYMODE suffix on a non-multi mode engine.");
            return Multi.runningPrimary;
        }

        public void SetRunningPrimary(BooleanValue prim)
        {
            ThrowIfNotCPUVessel();
            if (!MultiMode)
                throw new KOSException("Attempted to set the PRIMARYMODE suffix on a non-multi mode engine.");
            // If runningPrimary does not match prim, call ToggleMode
            if (prim != Multi.runningPrimary)
                ToggleMode();
        }

        public BooleanValue GetAutoSwitch()
        {
            if (!MultiMode)
                throw new KOSException("Attempted to get the AUTOSWITCH suffix on a non-multi mode engine.");
            return Multi.autoSwitch;
        }

        public void SetAutoswitch(BooleanValue auto)
        {
            ThrowIfNotCPUVessel();
            if (!MultiMode)
                throw new KOSException("Attempted to set the AUTOSWITCH suffix on a non-multi mode engine.");
            // if autoSwitch doesn't equal auto, use invoke to call the autoswitch method because the method is private
            if (Multi.autoSwitch != auto)
            {
                if (auto)
                    Multi.Events.First(kspEvent => kspEvent.name == "EnableAutoSwitch").Invoke();
                else
                    Multi.Events.First(kspEvent => kspEvent.name == "DisableAutoSwitch").Invoke();
            }
        }

        public StringValue GetCurrentMode()
        {
            if (!MultiMode)
                throw new KOSException("Attempted to get the MODE suffix on a non-multi mode engine.");
            return Multi.mode;
        }

        public GimbalFields GetGimbal()
        {
            if (Gimbal != null)
                return Gimbal;
            throw new KOSException("Attempted to get the GIMBAL suffix on an engine that does not have a gimbal.");
        }
    }

    public static class ModuleEnginesExtensions
    {
        public static float GetThrust(this ModuleEngines engine, bool useThrustLimit = false, float throttle = 1.0f, bool operational = true) =>
            GetThrust(engine, engine.part.staticPressureAtm, useThrustLimit, throttle, operational);
        public static float GetThrust(this ModuleEngines engine, double atmPressure, bool useThrustLimit = false, float throttle = 1.0f, bool operational = true)
        {
            if (engine == null || operational && !engine.isOperational)
                return 0f;
            if (useThrustLimit)
                throttle = throttle * engine.thrustPercentage / 100.0f;
            float flowMod = 1.0f;
            float velMod = 1.0f;
            if (engine.atmChangeFlow)
                flowMod = (float)(engine.part.atmDensity / 1.225f);
            if (engine.useAtmCurve && engine.atmCurve != null)
                flowMod = engine.atmCurve.Evaluate(flowMod);
            if (engine.useVelCurve && engine.velCurve != null)
                velMod = velMod * engine.velCurve.Evaluate((float)engine.vessel.mach);
            // thrust is modified fuel flow rate times isp time g times the velocity modifier for jet engines (as of KSP 1.0)
            return Mathf.Lerp(engine.minFuelFlow, engine.maxFuelFlow, throttle) * flowMod * GetIsp(engine, atmPressure) * engine.g * velMod;
        }

        public static float GetIsp(this ModuleEngines engine) =>
            GetIsp(engine, engine.part.staticPressureAtm);
        public static float GetIsp(this ModuleEngines engine, double staticPressureAtm) =>
            engine == null ? 0f : engine.atmosphereCurve.Evaluate((float)staticPressureAtm);

        public static float GetVacuumSpecificImpluse(this ModuleEngines engine) =>
            engine.atmosphereCurve.Evaluate(0);
        public static float GetSeaLevelSpecificImpulse(this ModuleEngines engine) =>
            engine.atmosphereCurve.Evaluate(1);
    }
}
