using kOS.AddOns.RemoteTech;
using kOS.Control;
using kOS.Binding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using kOS.Safe.Utilities;

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
        public override void OnAwake()
        {
            if (kOS.Safe.Utilities.SafeHouse.Logger != null)
            {
                kOS.Safe.Utilities.SafeHouse.Logger.LogError("kOSVesselModule Awake()!");
                parentVessel = GetComponent<Vessel>();
                if (parentVessel != null)
                {
                    allInstances[ID] = this;
                    addDefaultParameters();
                }
                kOS.Safe.Utilities.SafeHouse.Logger.LogError(string.Format("kOSVesselModule Awake() finished on {0} ({1})", parentVessel.name, ID));
            }
        }

        /// <summary>
        /// Start is called after OnEnable activates the module.  This is the second method called by
        /// KSP after Awake.  All parts should be added to the vessel now, so it is safe to walk the
        /// parts tree to find the attached kOSProcessor modules.
        /// </summary>
        public void Start()
        {
            kOS.Safe.Utilities.SafeHouse.Logger.LogError(string.Format("kOSVesselModule Start()!  On {0} ({1})", parentVessel.name, ID));
            harvestParts();
            hookEvents();
            initialized = true;
        }

        /// <summary>
        /// OnDestroy is called when the Vessel object is destroyed.  This is the last method called
        /// by KSP.  We can remove the instance tracking and unhook events here.
        /// </summary>
        public void OnDestroy()
        {
            if (kOS.Safe.Utilities.SafeHouse.Logger != null)
            {
                kOS.Safe.Utilities.SafeHouse.Logger.LogError("kOSVesselModule OnDestroy()!");
                unHookEvents();
                clearParts();
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
                    clearParts();
                    harvestParts();
                    partCount = parentVessel.Parts.Count;
                }
                if (parentVessel.loaded)
                {
                    updateParameterState();
                }
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
        private void harvestParts()
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

        private void clearParts()
        {
            childParts.Clear();
            partCount = 0;
        }

        /// <summary>
        /// Setup the default flight parameters.
        /// </summary>
        private void addDefaultParameters()
        {
            AddFlightControlParameter("steering", new SteeringManager(parentVessel));
        }

        /// <summary>
        /// Hook up to any vessel/game events that might be needed.
        /// </summary>
        private void hookEvents()
        {
            if (RemoteTechHook.IsAvailable(parentVessel.id))
            {
                RemoteTechHook.Instance.AddSanctionedPilot(parentVessel.id, updateAutopilot);
            }
            else
            {
                parentVessel.OnPreAutopilotUpdate += updateAutopilot;
            }
        }

        /// <summary>
        /// Unhook from events we previously hooked up to.
        /// </summary>
        private void unHookEvents()
        {
            if (RemoteTechHook.IsAvailable(parentVessel.id))
            {
                RemoteTechHook.Instance.RemoveSanctionedPilot(parentVessel.id, updateAutopilot);
            }
            else
            {
                parentVessel.OnPreAutopilotUpdate -= updateAutopilot;
            }
        }

        private void updateParameterState()
        {
            foreach (var parameter in flightControlParameters.Values)
            {
                if (parameter.Enabled) parameter.UpdateState();
            }
        }

        private void updateAutopilot(FlightCtrlState c)
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

        public void AddFlightControlParameter(string name, IFlightControlParameter parameter)
        {
            if (flightControlParameters.ContainsKey(name))
                throw new Exception("Flight parameter by the name " + name + " already exists.");
            flightControlParameters[name] = parameter;
        }

        public IFlightControlParameter GetFlightControlParameter(string name)
        {
            if (!flightControlParameters.ContainsKey(name))
                throw new Exception(string.Format("kOSVesselModule on {0} does not contain a parameter named {1}", parentVessel.name, name));
            return flightControlParameters[name];
        }

        public bool HasFlightControlParameter(string name)
        {
            return flightControlParameters.ContainsKey(name);
        }

        public void RemoveFlightControlParameter(string name)
        {
            if (flightControlParameters.ContainsKey(name))
            {
                var fc = flightControlParameters[name];
                flightControlParameters.Remove(name);
                IDisposable dispose = fc as IDisposable;
                if (dispose != null) dispose.Dispose();
            }
        }

        public static kOSVesselModule GetInstance(Vessel vessel)
        {
            return allInstances[vessel.id];
        }
    }
}
