﻿using kOS.AddOns.RemoteTech;
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

        public Guid ID
        {
            get
            {
                if (Vessel != null) return Vessel.id;
                return Guid.Empty;
            }
        }


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
            initialized = false;
            flightParametersAdded = false;
            autopilotRehookCounter = autopilotRehookPeriod - 2; // make sure it starts out ready to trigger soon
            flightControlParameters = new Dictionary<string, IFlightControlParameter>();
            childParts = new List<uint>();

            if (SafeHouse.Logger != null)
            {
                SafeHouse.Logger.SuperVerbose("kOSVesselModule OnAwake()!");
                if (Vessel != null)
                {
                    allInstances[ID] = this;
                }
                SafeHouse.Logger.SuperVerbose(string.Format("kOSVesselModule Awake() finished on {0} ({1})", Vessel.vesselName, ID));
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
            SafeHouse.Logger.SuperVerbose(string.Format("kOSVesselModule OnLoadVessel()!  On {0} ({1})", Vessel.vesselName, ID));

            // Vessel modules now load when the vessel is not loaded, including when not in the flight
            // scene.  So we now wait to attach to events and attempt to harvest parts until after
            // the vessel itself has loaded.


            allInstances[ID] = this;
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
                if (initialized)
                {
                    UnHookEvents();
                    ClearParts();
                }
                if (Vessel != null && allInstances.ContainsKey(ID) && ReferenceEquals(allInstances[ID], this))
                {
                    allInstances.Remove(ID);
                }
                foreach (var key in flightControlParameters.Keys.ToList())
                {
                    RemoveFlightControlParameter(key);
                }
                flightParametersAdded = false;
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
                if (Vessel.Parts.Count != partCount)
                {
                    ClearParts();
                    HarvestParts();
                    partCount = Vessel.Parts.Count;
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
            var proccessorModules = Vessel.FindPartModulesImplementing<kOSProcessor>();
            foreach (var proc in proccessorModules)
            {
                uint id = proc.part.flightID;
                childParts.Add(id);
                partLookup[id] = this;
            }
            partCount = Vessel.Parts.Count;
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
            SafeHouse.Logger.SuperVerbose(string.Format("kOSVesselModule AddDefaultParameters()!  On {0}", Vessel.vesselName));
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
            ConnectivityManager.RemoveAutopilotHook(Vessel, UpdateAutopilot);

            if (workAroundEventsEnabled)
            {
                TimingManager.FixedUpdateRemove(TimingManager.TimingStage.ObscenelyEarly, CacheControllable);
                TimingManager.FixedUpdateRemove(TimingManager.TimingStage.BetterLateThanNever, resetControllable);
                workAroundEventsEnabled = false;
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
