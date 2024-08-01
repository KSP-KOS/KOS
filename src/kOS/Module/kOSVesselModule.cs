using kOS.AddOns.RemoteTech;
using kOS.Control;
using kOS.Binding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using kOS.Safe.Utilities;
using kOS.Communication;
using kOS.Suffixed;

namespace kOS.Module
{
    /// <summary>
    /// The kOSVesselModule class contains references to anything that is vessel specific rather than
    /// processor part specific.  For instance, flight controls and steering.  It gives a single location
    /// to manage instances where docking, staging, and similar events should be handled automatically.
    /// </summary>
    public class kOSVesselModule : VesselModule
    {
        private bool initialized = false;
        private bool flightParametersAdded = false;
        /// <summary>How often to re-attempt the autopilot hook, expressed as a number of physics updates</summary>
        private const int autopilotRehookPeriod = 25;
        private int autopilotRehookCounter = autopilotRehookPeriod - 2; // make sure it starts out ready to trigger soon
        private bool foundWrongVesselAutopilot = false;

        public Guid ID
        {
            get
            {
                if (Vessel != null) return Vessel.id;
                return Guid.Empty;
            }
        }

        private Dictionary<string, IFlightControlParameter> flightControlParameters = new Dictionary<string, IFlightControlParameter>();
        private int partCount = 0;
        private bool hasKOSProcessor = false;

        #region KSP Vessel Module Events
        /// <summary>
        /// OnAwake is called once when instatiating a new VesselModule.  This is the first method called
        /// by KSP after the VesselModule has been attached to the parent Vessel.  We use it to store
        /// the parent Vessel and track the kOSVesselModule instances.
        /// </summary>
        protected override void OnAwake()
        {
            base.OnAwake();
            initialized = false;
            flightParametersAdded = false;
            autopilotRehookCounter = autopilotRehookPeriod - 2; // make sure it starts out ready to trigger soon
            flightControlParameters = new Dictionary<string, IFlightControlParameter>();

            if (SafeHouse.Logger != null)
            {
                SafeHouse.Logger.SuperVerbose("kOSVesselModule OnAwake()!");
            }
        }

        /// <summary>
        /// Start is called after OnEnable activates the module.  This is the second method called by
        /// KSP after Awake.  All parts should be added to the vessel now, so it is safe to walk the
        /// parts tree to find the attached kOSProcessor modules.
        /// </summary>
        protected override void OnStart()
        {
            SafeHouse.Logger.SuperVerbose(string.Format("kOSVesselModule OnStart()!  On {0} ({1})", Vessel.vesselName, ID));
        }

        public override void OnLoadVessel()
        {
            base.OnLoadVessel();
            SafeHouse.Logger.SuperVerbose(string.Format("kOSVesselModule OnLoadVessel()!  On {0} ({1})", Vessel.vesselName, Vessel.id));

            // Vessel modules now load when the vessel is not loaded, including when not in the flight
            // scene.  So we now wait to attach to events and attempt to harvest parts until after
            // the vessel itself has loaded.

            AddDefaultParameters();
            HarvestParts();
            HookEvents();
            initialized = true;
        }

        public override void OnUnloadVessel()
        {
            base.OnUnloadVessel();
            SafeHouse.Logger.SuperVerbose(string.Format("kOSVesselModule OnUnloadVessel()!  On {0} ({1})", Vessel.vesselName, ID));

            ClearParts();
            UnHookEvents();
            initialized = false;
        }

        public override void OnGoOffRails()
        {
            base.OnGoOffRails();
            SafeHouse.Logger.SuperVerbose(string.Format("kOSVesselModule OnGoOffRails()!  On {0} ({1})", Vessel.vesselName, ID));

            HookEvents();
        }

        public override void OnGoOnRails()
        {
            base.OnGoOnRails();
            SafeHouse.Logger.SuperVerbose(string.Format("kOSVesselModule OnGoOnRails()!  On {0} ({1})", Vessel.vesselName, ID));
        }

        protected override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            SafeHouse.Logger.SuperVerbose(string.Format("kOSVesselModule OnLoad()!  On {0}", Vessel.vesselName));
        }

        /// <summary>
        /// OnDestroy is called when the Vessel object is destroyed.  This is the last method called
        /// by KSP.  We can remove the instance tracking and unhook events here.
        /// </summary>
        public void OnDestroy()
        {
            if (SafeHouse.Logger != null)
            {
                SafeHouse.Logger.SuperVerbose("kOSVesselModule OnDestroy()!");
            }
            UnHookEvents();
            if (initialized)
            {
                ClearParts();
            }
            foreach (var key in flightControlParameters.Keys.ToList())
            {
                RemoveFlightControlParameter(key);
            }
            flightParametersAdded = false;
            initialized = false;
        }

        /// <summary>
        /// FixedUpdate is called once per physics tick.
        /// </summary>
        public void FixedUpdate()
        {
            if (initialized)
            {
                if (foundWrongVesselAutopilot || Vessel.Parts.Count != partCount)
                {
                    ClearParts();
                    HarvestParts();
                    partCount = Vessel.Parts.Count;
                    ResetPhysicallyDetachedParameters();
                }
                CheckRehookAutopilot();
            }
        }

        // ///<summary>
        // ///Update is called once per UI tick.
        // ///</summary>
        // public void Update()
        // {
        // }

        // ///<summary>
        // ///LastUpdate is called at the end of the update frame.  Useful if something needs to be
        // ///done after the physics and UI updates have been completed, but before scene or gui rendering.
        // ///</summary>
        // public void LastUpdate()
        // {
        // }

        // ///<summary>
        // ///OnGUI is where any vessel specific interface rendering should be done.
        // ///</summary>
        // public void OnGUI()
        // {
        // }
        #endregion

        /// <summary>
        /// Generates a list of all processorModules included on the parentVessel.
        /// </summary>
        private void HarvestParts()
        {
            hasKOSProcessor = vessel.FindPartModuleImplementing<kOSProcessor>() != null;
            partCount = Vessel.Parts.Count;
        }

        /// <summary>
        /// Clear the childParts list.  This is used when refreshing the parts list, or when destroying the module.
        /// </summary>
        private void ClearParts()
        {
            hasKOSProcessor = false;
            partCount = 0;
        }

        /// <summary>
        /// Setup the default flight parameters.
        /// </summary>
        private void AddDefaultParameters()
        {
            if (flightControlParameters != null)
            {
                flightControlParameters.Clear();
            }
            flightControlParameters = new Dictionary<string, IFlightControlParameter>();
            AddFlightControlParameter("steering", new SteeringManager(Vessel));
            AddFlightControlParameter("throttle", new ThrottleManager(Vessel));
            AddFlightControlParameter("wheelsteering", new WheelSteeringManager(Vessel));
            AddFlightControlParameter("wheelthrottle", new WheelThrottleManager(Vessel));
            AddFlightControlParameter("flightcontrol", new FlightControl(Vessel));
            flightParametersAdded = true;
            SafeHouse.Logger.SuperVerbose(string.Format("kOSVesselModule AddDefaultParameters()!  On {0}({1}", Vessel.vesselName, Vessel.id));
        }

        /// <summary>
        /// After a decouple or part explosion, it's possible for this vessel to
        /// still have an assigned flight control parameter that is coming from
        /// a kOS core that is no longer on this vessel but is instead on the newly
        /// branched vessel we left behind.  If so, that parameter needs to be
        /// removed from this vessel.  The new kOSVesselModule will take care of
        /// making a new parameter on the new vessel, but this kOSVesselModule needs
        /// to detach it from this one.
        /// </summary>
        private void ResetPhysicallyDetachedParameters()
        {
            List<string> removeKeys = new List<string>();
            foreach (string key in flightControlParameters.Keys)
            {
                IFlightControlParameter p = flightControlParameters[key];
                if (p.GetShared() != null && p.GetShared().Vessel != null && Vessel != null &&
                    p.GetShared().Vessel.id != Vessel.id)
                    removeKeys.Add(key);
            }
            foreach (string key in removeKeys)
            {
                SafeHouse.Logger.SuperVerbose(string.Format(
                    "kOSVesselModule: re-defaulting parameter \"{0}\" because it's on a detached part of the vessel.", key));
                RemoveFlightControlParameter(key);
                IFlightControlParameter p = null;
                if (key.Equals("steering"))
                    p = new SteeringManager(Vessel);
                else if (key.Equals("throttle"))
                    p = new ThrottleManager(Vessel);
                else if (key.Equals("wheelsteering"))
                    p = new WheelSteeringManager(Vessel);
                else if (key.Equals("wheelthrottle"))
                    p = new WheelThrottleManager(Vessel);
                else if (key.Equals("flightcontrol"))
                    p = new FlightControl(Vessel);

                if (p != null)
                    AddFlightControlParameter(key, p);
            }
            foundWrongVesselAutopilot = false;
        }

        /// <summary>
        /// Hook up to any vessel/game events that might be needed.
        /// </summary>
        private void HookEvents()
        {
            ConnectivityManager.AddAutopilotHook(Vessel, UpdateAutopilot);

            // HACK: The following events and their methods are a hack to work around KSP's limitation that
            // blocks our autopilot from having control of the vessel if out of signal and the setting
            // "Require Signal for Control" is enabled.  It's very hacky, and my have unexpected results.
            if (Vessel.vesselType != VesselType.Unknown && Vessel.vesselType != VesselType.SpaceObject && !workAroundEventsEnabled)
            {
                TimingManager.FixedUpdateAdd(TimingManager.TimingStage.ObscenelyEarly, CacheControllable);
                TimingManager.FixedUpdateAdd(TimingManager.TimingStage.BetterLateThanNever, resetControllable);
                workAroundEventsEnabled = true;
            }
        }

        /// <summary>
        /// Unhook from events we previously hooked up to.
        /// </summary>
        private void UnHookEvents()
        {
            if (ConnectivityManager.Instance != null)
            {
                ConnectivityManager.RemoveAutopilotHook(Vessel, UpdateAutopilot);
            }

            if (workAroundEventsEnabled)
            {
                TimingManager.FixedUpdateRemove(TimingManager.TimingStage.ObscenelyEarly, CacheControllable);
                TimingManager.FixedUpdateRemove(TimingManager.TimingStage.BetterLateThanNever, resetControllable);
                workAroundEventsEnabled = false;
            }

            if (AutopilotMsgManager.Instance != null)
            {
                AutopilotMsgManager.Instance.TurnOffSuppressMessage(this);
                AutopilotMsgManager.Instance.TurnOffSasMessage(this);
            }
        }

        #region Hack to fix "Require Signal for Control"
        // TODO: Delete this hack if it ever gets fixed.

        // Note: I am purposfully putting these variable declarations in this region instead of at the
        // top of the file as our normal convention dictates.  These are specific to the hack and when
        // we remove the hack the diff will make more sense if we wipe out a single contiguous block.
        private bool workAroundControllable = false;
        private bool workAroundEventsEnabled = false; // variable to track the extra event hooks used by the work around
        private CommNet.CommNetParams commNetParams;
        private System.Reflection.FieldInfo isControllableField;

        private void resetControllable()
        {
            if (workAroundControllable && isControllableField != null)
            {
                isControllableField.SetValue(Vessel, false);
            }
        }

        private void CacheControllable()
        {
            if (commNetParams == null && HighLogic.CurrentGame != null && HighLogic.CurrentGame.Parameters != null)
                commNetParams = HighLogic.CurrentGame.Parameters.CustomParams<CommNet.CommNetParams>();
            if (Vessel == null)
            {
                SafeHouse.Logger.LogError("kOSVesselModule.CacheControllable called with null parentVessel, contact kOS developers");
                workAroundControllable = false;
            }
            else if (!Vessel.packed && !Vessel.IsControllable && commNetParams != null && commNetParams.requireSignalForControl)
            {
                // HACK: Get around inability to affect throttle if connection is lost and require
                if (isControllableField == null)
                    isControllableField = Vessel.GetType().GetField("isControllable", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (isControllableField != null) // given the above line, this shouldn't be null unless the KSP version changed.
                {
                    isControllableField.SetValue(Vessel, true);
                    workAroundControllable = true;
                }
            }
            else
            {
                workAroundControllable = false;
            }
        }
        #endregion

        /// <summary>
        /// A race condition exists where KSP can load the kOS module onto the vessel
        /// before it loaded the RemoteTech module.  That makes it so that kOS may not
        /// see the existence of RT yet when kOS is first initialized.<br/>
        /// <br/>
        /// This fixes that case by continually re-querying for RT post-loading, and
        /// re-initializing kOS's RT-related behaviors if it seems that the RT module
        /// now exists when it didn't before (or visa versa).
        /// <br/>
        /// With the update for KSP 1.2, this function was renamed and made more generic
        /// so that it may support future autopilot connectivity configurations.
        /// </summary>
        private void CheckRehookAutopilot()
        {
            if (ConnectivityManager.NeedAutopilotResubscribe)
            {
                if (++autopilotRehookCounter > autopilotRehookPeriod)
                {
                    ConnectivityManager.AddAutopilotHook(Vessel, UpdateAutopilot);
                    autopilotRehookCounter = 0;
                }
            }
            else
            {
                autopilotRehookCounter = autopilotRehookPeriod - 2;
            }
        }

        /// <summary>
        /// Call to each IFlightControlParameter to update the flight controls.  This is called during OnPreAutopilotUpdate
        /// </summary>
        /// <param name="c"></param>
        private void UpdateAutopilot(FlightCtrlState c)
        {
            ControlTypes ctrlLock = InputLockManager.GetControlLock("RP0ControlLocker");
            FlightCtrlState originalCtrl = c;

            bool isSuppressing = SafeHouse.Config.SuppressAutopilot;

            AutopilotMsgManager.Instance.TurnOffSuppressMessage(this);
            AutopilotMsgManager.Instance.TurnOffSasMessage(this);

            if (Vessel != null)
            {
                if (hasKOSProcessor)
                {
                    foreach (var parameter in flightControlParameters.Values)
                    {
                        if (parameter.Enabled && parameter.IsAutopilot)
                        {
                            Vessel ves = parameter.GetResponsibleVessel();                                
                            if (ves != null && ves.id != Vessel.id)
                            {
                                // This is a "should never see this" error - being logged in case a user
                                // has problems and reports a bug.
                                SafeHouse.Logger.LogError(string.Format("kOS Autopilot on wrong vessel: {0} != {1}",
                                    parameter.GetShared().Vessel.id, Vessel.id));
                                foundWrongVesselAutopilot = true;
                            }
                            if (isSuppressing)
                            {
                                if (parameter.SuppressAutopilot(c))
                                    AutopilotMsgManager.Instance.TurnOnSuppressMessage(this);
                            }
                            else
                            {
                                parameter.UpdateAutopilot(c, ctrlLock);

                                if (parameter.FightsWithSas && vessel.ActionGroups[KSPActionGroup.SAS])
                                    AutopilotMsgManager.Instance.TurnOnSasMessage(this);
                            }
                        }
                    }

                    // Lock out controls if insufficient avionics in RP-0.
                    ControlTypes RP0Lock = InputLockManager.GetControlLock("RP0ControlLocker");
                    if (ctrlLock != 0)
                    {
                        if ((ctrlLock & ControlTypes.THROTTLE) != 0)
                        {
                            // Throttle locked to full or off (effectively the same as allowing z/x).
                            c.mainThrottle = c.mainThrottle > 0 ? 1 : 0;
                            // Fore/Aft locked to full or off
                            c.Z = c.Z > 0 ? 1 : (c.Z < 0 ? -1 : 0);
                            // Star / top disabled.
                            c.Y = originalCtrl.Y;
                            c.X = originalCtrl.X;
                        }
                        if ((ctrlLock & ControlTypes.WHEEL_STEER) != 0)
                        {
                            // wheel steering disabled
                            c.wheelThrottleTrim = originalCtrl.wheelThrottleTrim;
                            c.wheelSteerTrim = originalCtrl.wheelSteerTrim;
                            c.wheelSteer = originalCtrl.wheelSteer;
                        }
                        if ((ctrlLock & ControlTypes.WHEEL_THROTTLE) != 0)
                        {
                            c.wheelThrottle = originalCtrl.wheelThrottle;
                        }
                        if ((ctrlLock & ControlTypes.PITCH) != 0)
                        {
                            c.pitchTrim = originalCtrl.pitchTrim;
                            c.pitch = originalCtrl.pitch;
                        }
                        if ((ctrlLock & ControlTypes.YAW) != 0)
                        {
                            c.yawTrim = originalCtrl.yawTrim;
                            c.yaw = originalCtrl.yaw;
                        }
                        if ((ctrlLock & ControlTypes.ROLL) != 0)
                        {
                            c.rollTrim = originalCtrl.rollTrim;
                            c.roll = originalCtrl.roll;
                        }
                    }
                }
            }
        }

        private void HandleRemoteTechSanctionedPilot(FlightCtrlState c)
        {
            UpdateAutopilot(c);
        }

        private void HandleOnPreAutopilotUpdate(FlightCtrlState c)
        {
            UpdateAutopilot(c);
        }

        /// <summary>
        /// Adds an IFlightControlParameter to the reference dictionary, and throws an error if it already exists.
        /// </summary>
        /// <param name="name">the bound name of the parameter, this will be converted to lower case internally</param>
        /// <param name="parameter"></param>
        public void AddFlightControlParameter(string name, IFlightControlParameter parameter)
        {
            name = name.ToLower();
            if (flightControlParameters.ContainsKey(name))
                throw new Exception("Flight parameter by the name " + name + " already exists.");
            flightControlParameters[name] = parameter;
        }

        /// <summary>
        /// Returns the named IFlightControlParameter
        /// </summary>
        /// <param name="name">the bound name of the parameter, this will be converted to lower case internally</param>
        /// <returns></returns>
        public IFlightControlParameter GetFlightControlParameter(string name)
        {
            if (flightControlParameters == null || !flightParametersAdded)
            {
                AddDefaultParameters();
            }
            name = name.ToLower();
            if (!flightControlParameters.ContainsKey(name))
                throw new Exception(string.Format("kOSVesselModule on {0} does not contain a parameter named {1}", Vessel.vesselName, name));
            return flightControlParameters[name];
        }

        /// <summary>
        /// Returns true if the named IFlightControlParameter already exists
        /// </summary>
        /// <param name="name">the bound name of the parameter, this will be converted to lower case internally</param>
        /// <returns></returns>
        public bool HasFlightControlParameter(string name)
        {
            if (flightControlParameters == null || !flightParametersAdded)
            {
                return false;
            }
            name = name.ToLower();
            return flightControlParameters.ContainsKey(name);
        }

        /// <summary>
        /// Removes the named IFlightControlParameter from the reference dictionary.
        /// </summary>
        /// <param name="name">the bound name of the parameter, this will be converted to lower case internally</param>
        public void RemoveFlightControlParameter(string name)
        {
            name = name.ToLower();
            if (flightControlParameters.ContainsKey(name))
            {
                var fc = flightControlParameters[name];
                flightControlParameters.Remove(name);
                fc.DisableControl();
                IDisposable dispose = fc as IDisposable;
                if (dispose != null) dispose.Dispose();
            }
        }

        /// <summary>
        /// True if there is any kOSProcessor on the vessel in READY state.
        /// It's a slow O(n) operation (n = count of all PartModules on the vessel)
        /// so don't be calling this frequently.
        /// </summary>
        public bool AnyProcessorReady()
        {
            IEnumerable<PartModule> processorModules = Vessel.parts
                .SelectMany(p => p.Modules.Cast<PartModule>()
                .Where(pMod => pMod is kOSProcessor));
            foreach (kOSProcessor processor in processorModules)
            {
                if (processor.ProcessorMode == Safe.Module.ProcessorModes.READY)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Disables the controls
        /// </summary>
        public void OnAllProcessorsStarved()
        {
            foreach (IFlightControlParameter f in flightControlParameters.Values)
            {
                f.DisableControl();
            }
        }

        /// <summary>
        /// Return the kOSVesselModule instance associated with the given Vessel object
        /// </summary>
        /// <param name="vessel">the vessel for which the module should be returned</param>
        /// <returns></returns>
        public static kOSVesselModule GetInstance(Vessel vessel)
        {
            if (!vessel.isActiveAndEnabled)
                throw new Safe.Exceptions.KOSException("Vessel is no longer active or enabled " + vessel.name);
            return vessel.FindVesselModuleImplementing<kOSVesselModule>();
        }
    }
}
