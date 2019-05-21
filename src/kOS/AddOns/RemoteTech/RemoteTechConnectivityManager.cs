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
            if (!(RemoteTechHook.IsAvailable()))
                return -1; // default to no connection if RT itself isn't available.
            double delay = RemoteTechHook.Instance.GetSignalDelayToSatellite(vessel1.id, vessel2.id);
            return Double.IsPositiveInfinity(delay) ? -1 : delay;
        }

        public double GetDelayToControl(Vessel vessel)
        {
            if (!RemoteTechHook.IsAvailable())
                return -1; // default to no connection if RT itself isn't available.
            if (RemoteTechHook.Instance.HasLocalControl(vessel.id)) return 0d;
            double delay = RemoteTechHook.Instance.GetShortestSignalDelay(vessel.id);
            return Double.IsPositiveInfinity(delay) ? -1 : delay;
        }

        public double GetDelayToHome(Vessel vessel)
        {
            if (!RemoteTechHook.IsAvailable())
                return -1; // default to no connection if RT itself isn't available.
            double delay = RemoteTechHook.Instance.GetSignalDelayToKSC(vessel.id);
            return Double.IsPositiveInfinity(delay) ? -1 : delay;
        }

        public bool HasConnection(Vessel vessel1, Vessel vessel2)
        {
            // Availability check handled in GetDelay
            return GetDelay(vessel1, vessel2) >= 0;
        }

        public bool HasConnectionToHome(Vessel vessel)
        {
            if (!RemoteTechHook.IsAvailable())
                return false; // default to no connection if RT itself isn't available.
            return RemoteTechHook.Instance.HasConnectionToKSC(vessel.id);
        }

        public bool HasConnectionToControl(Vessel vessel)
        {
            if (!RemoteTechHook.IsAvailable())
                return vessel.CurrentControlLevel >= Vessel.ControlLevel.PARTIAL_MANNED; // default to checking for local control if RT itself isn't available.
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
                // removing the callback from stock if not already added doesn't throw an error
                vessel.OnPreAutopilotUpdate -= hook;
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
                // removing the callback from stock if not already added doesn't throw an error
                vessel.OnPreAutopilotUpdate -= hook;
            }
            else // remove fallback event hook
            {
                vessel.OnPreAutopilotUpdate -= hook;
            }
        }
    }
}