using System;
using kOS.Communication;
using kOS.Module;

namespace kOS.AddOns.RemoteTech
{
    /// <summary>
    /// This instance of IConnectivityManager will respect the RemoteTech settings/logic when
    /// checking for the current connectivity status.
    /// </summary>
    public class RemoteTechConnectivityManager : IConnectivityManager
    {
        private readonly bool hookAvailable;

        public RemoteTechConnectivityManager()
        {
            // It is safe to only check the hook's availability once when loading because the
            // in game setting to disable RemoteTech function is only available at the space
            // center scene.  This manager is only created when initializing a new processor
            // in the flight scene.
            hookAvailable = RemoteTechHook.IsAvailable();
        }

        public bool IsEnabled
        {
            get
            {
                return RemoteTechHook.Instance != null;
            }
        }

        public bool NeedAutopilotResubscribe
        {
            get
            {
                return true;
            }
        }

        public double GetDelay(Vessel vessel1, Vessel vessel2)
        {
            if (!(RemoteTechHook.IsAvailable(vessel1.id) && RemoteTechHook.IsAvailable(vessel2.id)))
                return -1; // default to no connection if one of the vessels isn't configured for RT.
            double delay = RemoteTechHook.Instance.GetSignalDelayToSatellite(vessel1.id, vessel2.id);
            return delay != double.PositiveInfinity ? delay : -1;
        }

        public double GetDelayToControl(Vessel vessel)
        {
            if (!RemoteTechHook.IsAvailable(vessel.id))
                return -1; // default to no connection if the vessel isn't configured for RT.
            if (RemoteTechHook.Instance.HasLocalControl(vessel.id)) return 0d;
            return RemoteTechHook.Instance.GetShortestSignalDelay(vessel.id);
        }

        public double GetDelayToHome(Vessel vessel)
        {
            if (!RemoteTechHook.IsAvailable(vessel.id))
                return -1; // default to no connection if the vessel isn't configured for RT.
            double delay = RemoteTechHook.Instance.GetSignalDelayToKSC(vessel.id);
            return delay != double.PositiveInfinity ? delay : -1;
        }

        public bool HasConnection(Vessel vessel1, Vessel vessel2)
        {
            // Availability check handled in GetDelay
            return GetDelay(vessel1, vessel2) >= 0;
        }

        public bool HasConnectionToHome(Vessel vessel)
        {
            if (!RemoteTechHook.IsAvailable(vessel.id))
                return false; // default to no connection if the vessel isn't configured for RT.
            return RemoteTechHook.Instance.HasConnectionToKSC(vessel.id);
        }

        public bool HasConnectionToControl(Vessel vessel)
        {
            if (!RemoteTechHook.IsAvailable(vessel.id))
                return vessel.CurrentControlLevel >= Vessel.ControlLevel.PARTIAL_MANNED; // default to checking for local control if the vessel isn't configured for RT.
            return RemoteTechHook.Instance.HasAnyConnection(vessel.id) || RemoteTechHook.Instance.HasLocalControl(vessel.id);
        }

        public void AddAutopilotHook(Vessel vessel, FlightInputCallback hook)
        {
            if (RemoteTechHook.IsAvailable(vessel.id))
            {
                Action<FlightCtrlState> action;
                if (!callbacks.TryGetValue(hook, out action))
                {
                    action = new Action<FlightCtrlState>(hook);
                    callbacks[hook] = action;
                }
                RemoteTechHook.Instance.AddSanctionedPilot(vessel.id, action);
            }
            else // fallback to stock events when RT isn't available, this may have unexpected results if RT availability changes
            {
                // removing the callback if not already added doesn't throw an error
                // but adding it a 2nd time will result in 2 calls.  Remove to be safe.
                vessel.OnPreAutopilotUpdate -= hook;
                vessel.OnPreAutopilotUpdate += hook;
            }
        }

        private System.Collections.Generic.Dictionary<FlightInputCallback, Action<FlightCtrlState>> callbacks = new System.Collections.Generic.Dictionary<FlightInputCallback, Action<FlightCtrlState>>();

        public void RemoveAutopilotHook(Vessel vessel, FlightInputCallback hook)
        {
            Action<FlightCtrlState> action;
            if (RemoteTechHook.IsAvailable(vessel.id) && callbacks.TryGetValue(hook, out action))
            {
                RemoteTechHook.Instance.RemoveSanctionedPilot(vessel.id, action);
                callbacks.Remove(hook);
            }
            else // remove fallback event hook
            {
                vessel.OnPreAutopilotUpdate -= hook;
            }
        }
    }
}