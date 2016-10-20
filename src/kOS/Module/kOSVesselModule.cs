using kOS.AddOns.RemoteTech;
using kOS.Control;
using kOS.Binding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using kOS.Safe.Utilities;
using kOS.Communication;

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
        private Vessel parentVessel;
        /// <summary>How often to re-attempt the autopilot hook, expressed as a number of physics updates</summary>
        private const int autopilotRehookPeriod = 25;
        private int autopilotRehookCounter = autopilotRehookPeriod - 2; // make sure it starts out ready to trigger soon

        public Guid ID
        {
            get
            {
                if (parentVessel != null) return parentVessel.id;
                return Guid.Empty;
            }
        }

        private Guid baseId = Guid.Empty;
        public Guid BaseId { get { return baseId; } }

        private static Dictionary<Guid, kOSVesselModule> allInstances = new Dictionary<Guid, kOSVesselModule>();
        private static Dictionary<uint, kOSVesselModule> partLookup = new Dictionary<uint, kOSVesselModule>();
        private Dictionary<string, IFlightControlParameter> flightControlParameters = new Dictionary<string, IFlightControlParameter>();
        private List<uint> childParts = new List<uint>();
        private int partCount = 0;

        #region KSP Vessel Module Events
        /// <summary>
        /// OnAwake is called once when instatiating a new VesselModule.  This is the first method called
        /// by KSP after the VesselModule has been attached to the parent Vessel.  We use it to store
        /// the parent Vessel and track the kOSVesselModule instances.
        /// </summary>
        protected override void OnAwake()
        {
            base.OnAwake();
            if (SafeHouse.Logger != null)
            {
                SafeHouse.Logger.SuperVerbose("kOSVesselModule Awake()!");
                parentVessel = GetComponent<Vessel>();
                if (parentVessel != null)
                {
                    allInstances[ID] = this;
                    AddDefaultParameters();
                }
                SafeHouse.Logger.SuperVerbose(string.Format("kOSVesselModule Awake() finished on {0} ({1})", parentVessel.vesselName, ID));
            }
        }

        /// <summary>
        /// Start is called after OnEnable activates the module.  This is the second method called by
        /// KSP after Awake.  All parts should be added to the vessel now, so it is safe to walk the
        /// parts tree to find the attached kOSProcessor modules.
        /// </summary>
        protected override void OnStart()
        {
            SafeHouse.Logger.SuperVerbose(string.Format("kOSVesselModule Start()!  On {0} ({1})", parentVessel.vesselName, ID));
            // Vessel modules now load when the vessel is not loaded, including when not in the flight
            // scene.  So we now wait to attach to events and attempt to harvest parts until after
            // the vessel itself has loaded.
            if (parentVessel.loaded && parentVessel.vesselType != VesselType.SpaceObject)
            {
                HarvestParts();
                HookEvents();
                initialized = true;
            }
        }

        protected override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
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
                if (initialized)
                {
                    UnHookEvents();
                    ClearParts();
                }
                if (parentVessel != null && allInstances.ContainsKey(ID))
                {
                    allInstances.Remove(ID);
                }
                foreach (var key in flightControlParameters.Keys.ToList())
                {
                    RemoveFlightControlParameter(key);
                }
                parentVessel = null;
                if (allInstances.Count == 0)
                {
                    partLookup.Clear();
                }
                initialized = false;
            }
        }

        /// <summary>
        /// FixedUpdate is called once per physics tick.
        /// </summary>
        public void FixedUpdate()
        {
            if (initialized)
            {
                if (parentVessel.Parts.Count != partCount)
                {
                    ClearParts();
                    HarvestParts();
                    partCount = parentVessel.Parts.Count;
                }
                if (parentVessel.loaded)
                {
                    UpdateParameterState();
                }
                CheckRehookAutopilot();
            }
            else if (parentVessel.loaded && parentVessel.vesselType != VesselType.SpaceObject)
            {
                // if not initialized, check to see if the vessel has loaded.  See note in OnStart()
                HarvestParts();
                HookEvents();
                initialized = true;
            }
        }

        /// <summary>
        /// Update is called once per UI tick.
        /// </summary>
        public void Update()
        {
        }

        /// <summary>
        /// LastUpdate is called at the end of the update frame.  Useful if something needs to be
        /// done after the physics and UI updates have been completed, but before scene or gui rendering.
        /// </summary>
        public void LastUpdate()
        {
        }

        /// <summary>
        /// OnGUI is where any vessel specific interface rendering should be done.
        /// </summary>
        public void OnGUI()
        {
        }
        #endregion

        /// <summary>
        /// Generates a list of all processorModules included on the parentVessel.
        /// </summary>
        private void HarvestParts()
        {
            var proccessorModules = parentVessel.FindPartModulesImplementing<kOSProcessor>();
            foreach (var proc in proccessorModules)
            {
                childParts.Add(proc.part.flightID);
                uint id = proc.part.flightID;
                if (partLookup.ContainsKey(id) && partLookup[id].ID != ID)
                {
                    // If the part is already known and not associated with this module, then
                    // it is appearing as a result of a staging or undocking event (or something
                    // similar).  As such, we need to copy the information from the flight parameters
                    // on the originating module.
                    foreach (string key in flightControlParameters.Keys)
                    {
                        if (partLookup[id].HasFlightControlParameter(key))
                        {
                            // Only copy the data if the previous vessel module still has the parameter.
                            IFlightControlParameter paramDestination = flightControlParameters[key];
                            IFlightControlParameter paramOrigin = partLookup[id].GetFlightControlParameter(key);
                            // We only want to copy the parameters themselves once (because they are vessel dependent
                            // not part dependent) but we still need to iterate through each part since "control"
                            // itself is part dependent.
                            if (partLookup[id].ID != BaseId) paramDestination.CopyFrom(paramOrigin);
                            if (paramOrigin.Enabled && paramOrigin.ControlPartId == id)
                            {
                                // If this parameter was previously controlled by this part, re-enable
                                // control, copy it's Value setpoint, and disable control on the old parameter.
                                SharedObjects shared = paramOrigin.GetShared();
                                paramDestination.EnableControl(shared);
                                paramDestination.UpdateValue(paramOrigin.GetValue(), shared);
                                paramOrigin.DisableControl();
                            }
                        }
                    }
                    baseId = partLookup[id].ID; // Keep track of which vessel the parameters are based on.
                }
                partLookup[id] = this;
            }
            partCount = parentVessel.Parts.Count;
        }

        /// <summary>
        /// Clear the childParts list.  This is used when refreshing the parts list, or when destroying the module.
        /// </summary>
        private void ClearParts()
        {
            childParts.Clear();
            partCount = 0;
        }

        /// <summary>
        /// Setup the default flight parameters.
        /// </summary>
        private void AddDefaultParameters()
        {
            AddFlightControlParameter("steering", new SteeringManager(parentVessel));
        }

        /// <summary>
        /// Hook up to any vessel/game events that might be needed.
        /// </summary>
        private void HookEvents()
        {
            ConnectivityManager.AddAutopilotHook(parentVessel, UpdateAutopilot);

            // HACK: The following events and their methods are a hack to work around KSP's limitation that
            // blocks our autopilot from having control of the vessel if out of signal and the setting
            // "Require Signal for Control" is enabled.  It's very hacky, and my have unexpected results.
            if (Vessel.vesselType != VesselType.Unknown && Vessel.vesselType != VesselType.SpaceObject)
            {
                TimingManager.FixedUpdateAdd(TimingManager.TimingStage.ObscenelyEarly, cacheControllable);
                TimingManager.FixedUpdateAdd(TimingManager.TimingStage.BetterLateThanNever, resetControllable);
            }
        }

        /// <summary>
        /// Unhook from events we previously hooked up to.
        /// </summary>
        private void UnHookEvents()
        {
            ConnectivityManager.RemoveAutopilotHook(parentVessel, UpdateAutopilot);

            if (Vessel.vesselType != VesselType.Unknown && Vessel.vesselType != VesselType.SpaceObject)
            {
                TimingManager.FixedUpdateRemove(TimingManager.TimingStage.ObscenelyEarly, cacheControllable);
                TimingManager.FixedUpdateRemove(TimingManager.TimingStage.BetterLateThanNever, resetControllable);
            }
        }

        #region Hack to fix "Require Signal for Control"
        // TODO: Delete this hack if it ever gets fixed.

        // Note: I am purposfully putting these variable declarations in this region instead of at the
        // top of the file as our normal convention dictates.  These are specific to the hack and when
        // we remove the hack the diff will make more sense if we wipe out a single contiguous block.
        private bool workAroundControllable = false;
        private CommNet.CommNetParams commNetParams;
        private System.Reflection.FieldInfo isControllableField;

        private void resetControllable()
        {
            if (workAroundControllable && isControllableField != null)
            {
                isControllableField.SetValue(parentVessel, false);
            }
        }

        private void cacheControllable()
        {
            if (commNetParams == null)
                commNetParams = HighLogic.CurrentGame.Parameters.CustomParams<CommNet.CommNetParams>();
            if (!parentVessel.IsControllable && commNetParams != null && commNetParams.requireSignalForControl)
            {
                // HACK: Get around inability to affect throttle if connection is lost and require
                if (isControllableField == null)
                    isControllableField = parentVessel.GetType().GetField("isControllable", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                isControllableField.SetValue(parentVessel, true);
                workAroundControllable = true;
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
                    ConnectivityManager.AddAutopilotHook(parentVessel, UpdateAutopilot);
                }
            }
            else
            {
                autopilotRehookCounter = autopilotRehookPeriod - 2;
            }
        }

        /// <summary>
        /// Update the current stte of the each IFlightControlParameter.  This is called during FixedUpdate
        /// </summary>
        private void UpdateParameterState()
        {
            foreach (var parameter in flightControlParameters.Values)
            {
                if (parameter.Enabled) parameter.UpdateState();
            }
        }

        /// <summary>
        /// Call to each IFlightControlParameter to update the flight controls.  This is called during OnPreAutopilotUpdate
        /// </summary>
        /// <param name="c"></param>
        private void UpdateAutopilot(FlightCtrlState c)
        {
            if (childParts.Count > 0)
            {
                foreach (var parameter in flightControlParameters.Values)
                {
                    if (parameter.Enabled && parameter.IsAutopilot)
                    {
                        parameter.UpdateAutopilot(c);
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
            name = name.ToLower();
            if (!flightControlParameters.ContainsKey(name))
                throw new Exception(string.Format("kOSVesselModule on {0} does not contain a parameter named {1}", parentVessel.vesselName, name));
            return flightControlParameters[name];
        }

        /// <summary>
        /// Returns true if the named IFlightControlParameter already exists
        /// </summary>
        /// <param name="name">the bound name of the parameter, this will be converted to lower case internally</param>
        /// <returns></returns>
        public bool HasFlightControlParameter(string name)
        {
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
                IDisposable dispose = fc as IDisposable;
                if (dispose != null) dispose.Dispose();
            }
        }

        /// <summary>
        /// Return the kOSVesselModule instance associated with the given Vessel object
        /// </summary>
        /// <param name="vessel">the vessel for which the module should be returned</param>
        /// <returns></returns>
        public static kOSVesselModule GetInstance(Vessel vessel)
        {
            kOSVesselModule ret;
            if (!allInstances.TryGetValue(vessel.id, out ret))
            {
                ret = vessel.GetComponent<kOSVesselModule>();
                if (ret == null)
                    throw new kOS.Safe.Exceptions.KOSException("Cannot find kOSVesselModule on vessel " + vessel.name);
                allInstances.Add(vessel.id, ret);
            }
            return ret;
        }
    }
}
