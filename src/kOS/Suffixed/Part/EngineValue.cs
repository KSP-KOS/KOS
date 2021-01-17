using kOS.Safe;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using kOS.Safe.Utilities;
using kOS.Suffixed.PartModuleField;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace kOS.Suffixed.Part
{
    [kOS.Safe.Utilities.KOSNomenclature("Engine")]
    public class EngineValue : PartValue
    {
        /// <summary>All the Engine Modules in the part regardless of "engine mode".
        /// If this is a multi-mode engine, then all its variations are present
        /// in the list even though they can't all be in use at the same time.</summary>
        private List<ModuleEngines> RawEngineList { get; set; }
        public MultiModeEngine Multi { get; private set; }
        public GimbalFields Gimbal { get; private set; }
        // +For RealFuels
        private readonly bool HasRealFuels;
        private readonly PartModule EngineConfig;
        // -For RealFuels
        /// <summary>Only those Engine Modules that the part's current engine mode allows to work.
        /// (i.e. if an engine has a wet mode and a dry mode, then you should see either the wet module
        /// or the dry module in this list, but not both at once.)</summary>
        public IEnumerable<ModuleEngines> FilteredEngineList {
            get {
                if (RawEngineList.Count > 0 && Multi != null)
                {
                    return RawEngineList.Count < 2 || Multi.runningPrimary ? RawEngineList.GetRange(0, 1) : RawEngineList.GetRange(1, 1);
                }
                else
                {
                    return RawEngineList;
                }
            }
        }
        public bool MultiMode { get { return Multi != null; } }
        public bool HasGimbal { get { return Gimbal != null; } }

        /// <summary>
        /// Do not call! VesselTarget.ConstructPart uses this, would use `friend VesselTarget` if this was C++!
        /// </summary>
        internal EngineValue(SharedObjects shared, global::Part part, PartValue parent, DecouplerValue decoupler)
            : base(shared, part, parent, decoupler)
        {
            RawEngineList = new List<ModuleEngines>();
            foreach (var module in part.Modules)
            {
                var mme = module as MultiModeEngine;
                var e = module as ModuleEngines;
                if (mme != null)
                {
                    if (Multi == null)
                        Multi = mme;
                    else
                        SafeHouse.Logger.LogWarning("Multiple MultiModeEngine on {0}: {1}", part.name, part.partInfo.title);
                }
                else if (e != null)
                {
                    RawEngineList.Add(e);
                }

                if (e != null)
                {
                    HasRealFuels = false;
                    for (System.Type t = module.GetType(); t != null; t = t.BaseType)
                    {
                        if (t.Name.Contains("ModuleEnginesRF"))
                        {
                            HasRealFuels = true;
                            break;
                        }
                    }
                }

                if (module.GetType().Name.Contains("ModuleEngineConfigs"))
                    EngineConfig = module;
            }
            if (RawEngineList.Count < 1)
                throw new KOSException("Attempted to build an Engine from part with no ModuleEngines on {0}: {1}", part.name, part.partInfo.title);
            if (RawEngineList.Count < 2)
            {
                if (Multi != null)
                    SafeHouse.Logger.LogWarning("MultiModeEngine without second engine on {0}: {1}", part.name, part.partInfo.title);
            }
            else
            {
                if (Multi != null)
                {
                    if (Multi.primaryEngineID == RawEngineList[1].engineID)
                    {
                        RawEngineList.Reverse();
                    }
                    else if (Multi.primaryEngineID != RawEngineList[0].engineID)
                        SafeHouse.Logger.LogWarning("Primary engine ID={0} does not match multi.e1={1} on {2}: {3}",
                            RawEngineList[0].engineID, Multi.primaryEngineID, part.name, part.partInfo.title);
                    if (Multi.secondaryEngineID != RawEngineList[1].engineID)
                        SafeHouse.Logger.LogWarning("Secondary engine ID={0} does not match multi.e2={1} on {2}: {3}",
                            RawEngineList[1].engineID, Multi.secondaryEngineID, part.name, part.partInfo.title);
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
            AddSuffix("THRUSTLIMIT", new ClampSetSuffix<ScalarValue>(() => FilteredEngineList.Average(e => e.thrustPercentage),
                                                          value => RawEngineList.ForEach(e => e.thrustPercentage = value),
                                                          0f, 100f, 0f,
                                                          "thrust limit percentage for this engine"));
            AddSuffix("MAXTHRUST", new Suffix<ScalarValue>(() => FilteredEngineList.Sum(e => e.GetThrust())));
            AddSuffix("THRUST", new Suffix<ScalarValue>(() => FilteredEngineList.Sum(e => e.finalThrust)));
            AddSuffix("FUELFLOW", new Suffix<ScalarValue>(GetFuelFlow));
            AddSuffix("MAXFUELFLOW", new Suffix<ScalarValue>(GetMaxFuelFlow));
            AddSuffix("MASSFLOW", new Suffix<ScalarValue>(GetMassFlow));
            AddSuffix("MAXMASSFLOW", new Suffix<ScalarValue>(() => FilteredEngineList.Sum(e => e.maxFuelFlow)));
            AddSuffix("ISP", new Suffix<ScalarValue>(GetIsp));
            AddSuffix(new[] { "VISP", "VACUUMISP" }, new Suffix<ScalarValue>(GetVacuumIsp));
            AddSuffix(new[] { "SLISP", "SEALEVELISP" }, new Suffix<ScalarValue>(GetSeaLevelIsp));
            AddSuffix("FLAMEOUT", new Suffix<BooleanValue>(() => FilteredEngineList.Any(e => e.flameout)));
            AddSuffix("IGNITION", new Suffix<BooleanValue>(() => FilteredEngineList.Any(e => e.getIgnitionState)));
            AddSuffix("ALLOWRESTART", new Suffix<BooleanValue>(() => FilteredEngineList.Any(e => e.allowRestart)));
            AddSuffix("ALLOWSHUTDOWN", new Suffix<BooleanValue>(() => FilteredEngineList.Any(e => e.allowShutdown)));
            AddSuffix("THROTTLELOCK", new Suffix<BooleanValue>(() => FilteredEngineList.All(e => e.throttleLocked)));
            AddSuffix("ISPAT", new OneArgsSuffix<ScalarValue, ScalarValue>(GetIspAtAtm));
            AddSuffix("MAXTHRUSTAT", new OneArgsSuffix<ScalarValue, ScalarValue>(GetMaxThrustAtAtm));
            AddSuffix("AVAILABLETHRUST", new Suffix<ScalarValue>(() => FilteredEngineList.Sum(e => e.GetThrust(useThrustLimit: true))));
            AddSuffix("AVAILABLETHRUSTAT", new OneArgsSuffix<ScalarValue, ScalarValue>(GetAvailableThrustAtAtm));
            AddSuffix("POSSIBLETHRUST", new Suffix<ScalarValue>(() => FilteredEngineList.Sum(e => e.GetThrust(useThrustLimit: true, operational: false))));
            AddSuffix("POSSIBLETHRUSTAT", new OneArgsSuffix<ScalarValue, ScalarValue>(GetPossibleThrustAtAtm));
            AddSuffix("MAXPOSSIBLETHRUST", new Suffix<ScalarValue>(() => FilteredEngineList.Sum(e => e.GetThrust(operational: false))));
            AddSuffix("MAXPOSSIBLETHRUSTAT", new OneArgsSuffix<ScalarValue, ScalarValue>(atm => FilteredEngineList.Sum(e => e.GetThrust(atm, operational: false))));
            AddSuffix("CONSUMEDRESOURCES", new Suffix<Lexicon>(GetConsumedResources, "A List of all resources consumed by this engine"));
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

            // RealFuels stuff
            AddSuffix("ULLAGE", new Suffix<BooleanValue>(GetUllage));
            AddSuffix("FUELSTABILITY", new Suffix<ScalarValue>(GetFuelStability));
            AddSuffix("PRESSUREFED", new Suffix<BooleanValue>(GetPressureFed));
            AddSuffix("IGNITIONS", new Suffix<ScalarValue>(GetIgnitions));
            AddSuffix("MINTHROTTLE", new Suffix<ScalarValue>(GetMinThrottle));
            AddSuffix("CONFIG", new Suffix<StringValue>(GetEngineConfig));
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
                    {
                        toReturn.Add(vessel[part]);
                        // Only add each part once
                        break;
                    }
                }
            }
            return toReturn;
        }

        public void Activate()
        {
            ThrowIfNotCPUVessel();
            foreach (ModuleEngines e in FilteredEngineList)
            {
                e.Activate();
            }
        }

        public void Shutdown()
        {
            ThrowIfNotCPUVessel();
            foreach (ModuleEngines e in FilteredEngineList)
            {
                e.Shutdown();
            }
        }

        // The following functions calculate the correct Isp for multiple engines, regardless of individual performance.
        // Isps cannot be simply summed or averaged if the engines have different thrust output,
        // however mass flow and thrust can be summed, so these are used to derive a combined Isp.
        // This is based on the identity MassFlow = Thrust / (g0 * Isp), with g0 factored out.
        public ScalarValue GetIsp()
        {
            // Get the combined flow rate of all engines
            double flowRate = FilteredEngineList.Sum(e => e.GetThrust(operational: false) / e.realIsp);
            // Divide combined thrust by combined flow rate to get a correct Isp for all engines combined
            return flowRate > 0 ? FilteredEngineList.Sum(e => e.GetThrust(operational: false)) / flowRate : 0;
        }
        public ScalarValue GetVacuumIsp()
        {
            // Get the combined flow rate of all engines
            double flowRate = FilteredEngineList.Sum(e => e.GetThrust(0, operational: false) / e.atmosphereCurve.Evaluate(0));
            // Divide combined thrust by combined flow rate to get a correct Isp for all engines combined
            return flowRate > 0 ? FilteredEngineList.Sum(e => e.GetThrust(0, operational: false)) / flowRate : 0;
        }
        public ScalarValue GetSeaLevelIsp()
        {
            // Get the combined flow rate of all engines
            double flowRate = FilteredEngineList.Sum(e => e.GetThrust(1, operational: false) / e.atmosphereCurve.Evaluate(1));
            // Divide combined thrust by combined flow rate to get a correct Isp for all engines combined
            return flowRate > 0 ? FilteredEngineList.Sum(e => e.GetThrust(1, operational: false)) / flowRate : 0;
        }
        public ScalarValue GetIspAtAtm(ScalarValue atmPressure)
        {
            // Get the combined flow rate of all engines
            double flowRate = FilteredEngineList.Sum(e => e.GetThrust(atmPressure, operational: false) / e.GetIsp(atmPressure.GetDoubleValue()));
            // Divide combined thrust by combined flow rate to get a correct Isp for all engines combined
            return flowRate > 0 ? FilteredEngineList.Sum(e => e.GetThrust(atmPressure, operational: false)) / flowRate : 0;
        }

        public ScalarValue GetMaxThrustAtAtm(ScalarValue atmPressure)
        {
            return FilteredEngineList.Sum(e => e.GetThrust(atmPressure));
        }
        public ScalarValue GetAvailableThrustAtAtm(ScalarValue atmPressure)
        {
            return FilteredEngineList.Sum(e => e.GetThrust(atmPressure, useThrustLimit: true));
        }
        public ScalarValue GetPossibleThrustAtAtm(ScalarValue atmPressure)
        {
            return FilteredEngineList.Sum(e => e.GetThrust(atmPressure, useThrustLimit: true, operational: false));
        }

        public Lexicon GetConsumedResources()
        {
            var resources = new Lexicon();

            foreach (ModuleEngines e in FilteredEngineList)
            {
                foreach (Propellant p in e.propellants)
                {
                    resources.Add(new StringValue(p.displayName), new ConsumedResourceValue(e, p, Shared));
                }
            }

            return resources;
        }

        public StringValue GetEngineConfig()
        {
            if (EngineConfig != null)
            {
                var configField = EngineConfig.GetType().GetField("configuration");
                if (configField != null)
                    return (string)configField.GetValue(EngineConfig);
            }

            return Part.partInfo.title;
        }

        public ScalarValue GetFuelFlow()
        {
            // fuelFlowGui is overidden by RealFuels to display massflow, so work around it.
            if (HasRealFuels)
            {
                double fuelFlow = 0;

                foreach (ModuleEngines e in FilteredEngineList)
                {
                    // currentAmount does not update when there is no fuel flow, so only update when fuel is flowing.
                    if (e.fuelFlowGui > 0.0f)
                    {
                        foreach (Propellant p in e.propellants)
                        {
                            fuelFlow += p.currentAmount / Time.fixedDeltaTime;
                        }
                    }
                }

                return fuelFlow;
            }
            else
            {
                // Stock just shows flow.
                return FilteredEngineList.Sum(e => e.fuelFlowGui);
            }
        }

        // Unfortunately mixtureDensity is unreliable so there isn't a quick way to get this.
        public ScalarValue GetMaxFuelFlow()
        {
            double maxFuelFlow = 0;

            foreach (ModuleEngines e in FilteredEngineList)
            {
                foreach (Propellant p in e.propellants)
                {
                    maxFuelFlow += e.getMaxFuelFlow(p);
                }
            }

            return maxFuelFlow;
        }

        public ScalarValue GetMassFlow()
        {
            double massFlow = 0;

            foreach (ModuleEngines e in FilteredEngineList)
            {
                // currentAmount does not update when there is no fuel flow, so only update when fuel is flowing.
                if (e.fuelFlowGui > 0.0f)
                {
                    foreach (Propellant p in e.propellants)
                    {
                        massFlow += p.currentAmount * p.resourceDef.density / Time.fixedDeltaTime;
                    }
                }
            }

            return massFlow;
        }

        public BooleanValue GetUllage()
        {
            if (HasRealFuels)
            {
                foreach (ModuleEngines e in FilteredEngineList)
                {
                    var ullageField = e.GetType().GetField("ullage");
                    if (ullageField != null)
                    {
                        // Return immediately if the module has ullage.
                        if ((bool)ullageField.GetValue(e))
                            return true;
                    }
                }
            }

            return false;
        }

        public ScalarValue GetFuelStability()
        {
            // Default to full stability
            float stability = 1.0f;

            if (HasRealFuels)
            {
                foreach (ModuleEngines e in FilteredEngineList)
                {
                    var ullageField = e.GetType().GetField("ullage");
                    var pressureFedField = e.GetType().GetField("pressureFed");
                    var ullageSetField = e.GetType().GetField("ullageSet");

                    if (ullageField != null && pressureFedField != null && ullageSetField != null)
                    {
                        var ullageSet = ullageSetField.GetValue(e);

                        bool pressureOK = !(bool)pressureFedField.GetValue(e);
                        if (!pressureOK)
                        {
                            var pressureOKMethod = ullageSet.GetType().GetMethod("PressureOK", BindingFlags.Public | BindingFlags.Instance);
                            pressureOK = pressureOKMethod != null ? System.Convert.ToBoolean(pressureOKMethod.Invoke(ullageSet, new object[] { })) : true;
                        }

                        if (!pressureOK)
                        {
                            // No feed pressure
                            stability = 0;
                        }
                        else if ((bool)ullageField.GetValue(e))
                        {
                            // Use minimum value if multiple engines
                            var ullageStabilityMethod = ullageSet.GetType().GetMethod("GetUllageStability", BindingFlags.Public | BindingFlags.Instance);
                            if (ullageStabilityMethod != null)
                                stability = Mathf.Min(stability, System.Convert.ToSingle(ullageStabilityMethod.Invoke(ullageSet, new object[] { })));
                        }
                    }
                }
            }

            return stability;
        }

        public BooleanValue GetPressureFed()
        {
            if (HasRealFuels)
            {
                foreach (ModuleEngines e in FilteredEngineList)
                {
                    var pressureFedField = e.GetType().GetField("pressureFed");
                    if (pressureFedField != null)
                    {
                        // Return immediately if the module is pressure fed.
                        if ((bool)pressureFedField.GetValue(e))
                            return true;
                    }
                }
            }

            return false;
        }

        public ScalarValue GetIgnitions()
        {
            int ignitions = -1;

            if (HasRealFuels)
            {
                foreach (ModuleEngines e in FilteredEngineList)
                {
                    var ignitionsField = e.GetType().GetField("ignitions");
                    if (ignitionsField != null)
                    {
                        int engIngitions = (int)ignitionsField.GetValue(e);
                        if (engIngitions >= 0)
                        {
                            if (ignitions < 0)
                                ignitions = engIngitions;
                            else
                                ignitions = Mathf.Min(ignitions, engIngitions);
                        }
                    }
                }
            }

            return ignitions;
        }

        public ScalarValue GetMinThrottle()
        {
            float minThrottle = 0;

            if (HasRealFuels)
            {
                foreach (ModuleEngines e in FilteredEngineList)
                {
                    minThrottle = Mathf.Max(minThrottle, e.minFuelFlow / e.maxFuelFlow);
                }
            }

            return minThrottle;
        }

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
        /// <summary>
        /// Get engine thrust
        /// </summary>
        /// <param name="engine">The engine (can be null - returns zero in that case)</param>
        /// <param name="atmPressure">
        ///   Atmospheric pressure (defaults to pressure at current location if omitted/null,
        ///   1.0 means Earth/Kerbin sea level, 0.0 is vacuum)</param>
        /// <param name="useThrustLimit">Use current thrust limit (assume 100% if false)</param>
        /// <param name="throttle">Throttle (full if omitted)</param>
        /// <param name="operational">Return zero if this is true and engine is not operational (enabled/staged)</param>
        /// <returns>The thrust</returns>
        public static float GetThrust(this ModuleEngines engine, double? atmPressure = null, bool useThrustLimit = false, float throttle = 1.0f, bool operational = true)
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
        /// <summary>
        /// Get engine ISP
        /// </summary>
        /// <param name="engine">The engine (can be null - returns zero in that case)</param>
        /// <param name="atmPressure">
        ///   Atmospheric pressure (defaults to pressure at current location if omitted/null,
        ///   1.0 means Earth/Kerbin sea level, 0.0 is vacuum)</param>
        /// <returns></returns>
        public static float GetIsp(this ModuleEngines engine, double? atmPressure = null)
        {
            return engine == null ? 0f : engine.atmosphereCurve.Evaluate(Mathf.Max(0f,
                (float)(atmPressure ?? engine.part.staticPressureAtm)));
        }
    }
}
