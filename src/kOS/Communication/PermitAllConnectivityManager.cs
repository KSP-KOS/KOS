using System;
using CommNet;
using kOS.Module;

namespace kOS.Communication
{
    /// <summary>
    /// This instance of IConnectivityManager will allways permit connectivity, and will ignore any
    /// connectivity settings or mods.
    /// </summary>
    public class PermitAllConnectivityManager : IConnectivityManager
    {
        /// <summary>
        /// Always returns true, because this manager is always available.
        /// </summary>
        public bool IsEnabled
        {
            get
            {
                return true;
            }
        }

        public bool NeedAutopilotResubscribe
        {
            get
            {
                return false;
            }
        }

        public double GetDelay(Vessel vessel1, Vessel vessel2)
        {
            return 0;
        }

        public double GetDelayToHome(Vessel vessel)
        {
            return 0;
        }

        public double GetDelayToControl(Vessel vessel)
        {
            return 0;
        }

        public bool HasConnectionToHome(Vessel vessel)
        {
            return true;
        }

        public bool HasConnectionToControl(Vessel vessel)
        {
            return true;
        }

        public bool HasConnection(Vessel vessel1, Vessel vessel2)
        {
            return true;
        }

        public void AddAutopilotHook(Vessel vessel, FlightInputCallback hook)
        {
            if (vessel != null)
            {
                // removing the callback if not already added doesn't throw an error
                // but adding it a 2nd time will result in 2 calls.  Remove to be safe.
                vessel.OnPreAutopilotUpdate -= hook;
                vessel.OnPreAutopilotUpdate += hook;
            }
        }

        public void RemoveAutopilotHook(Vessel vessel, FlightInputCallback hook)
        {
            vessel.OnPreAutopilotUpdate -= hook;
        }
    }
}