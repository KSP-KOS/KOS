using System;
using CommNet;
using kOS.Module;

namespace kOS.Communication
{
    /// <summary>
    /// This instance of IConnectivityManager will respect the CommNet settings/logic when
    /// checking for the current connectivity status.
    /// </summary>
    public class CommNetConnectivityManager : IConnectivityManager
    {
        // WARNING: you must call .Clear() on this object after using it!
        // it will hold onto references to vessels even after they have been destroyed, causing a memory leak
        private readonly CommPath tempPath = new CommPath();

        /// <summary>
        /// Checks to see if CommNet is enabled in the current game.  This should be checked often
        /// because CommNet itself and kOS's CommNet integration can be enabled and disabled from
        /// the in game dificulty settings menu. True if all CommNet support is enabled.
        /// </summary>
        public bool IsEnabled
        {
            get
            {
                return HighLogic.CurrentGame != null
                    && HighLogic.CurrentGame.Parameters.Difficulty.EnableCommNet;
            }
        }

        public bool NeedAutopilotResubscribe
        {
            get
            {
                return false;
            }
        }

        /// <summary>A sanity check to avoid nullref errors when the vessel is
        /// not a participant in the stock CommNet system and is thus lacking
        /// certain properties. (Debris vessels lack CommNet info.)</summary>
        public static bool IsCommnetParticipant(Vessel v)
        {
            if (v == null || v.Connection == null || v.Connection.Comm == null)
                return false;
            return true;
        }

        public double GetDelay(Vessel vessel1, Vessel vessel2)
        {
            if (!IsEnabled)
                return 0;
            if (HasConnection(vessel1, vessel2))
                return 0; // CommNet.CommPath does not currently expose a delay value
            return -1;
        }

        public double GetDelayToHome(Vessel vessel)
        {
            if (!IsEnabled)
                return 0;
            if (HasConnectionToHome(vessel))
                return vessel.Connection.SignalDelay; // in stock this is a dummy 0, but other mods may modify it
            return -1;
        }

        public double GetDelayToControl(Vessel vessel)
        {
            return 0;
        }

        public bool HasConnectionToHome(Vessel vessel)
        {
            if (!IsEnabled)
                return true;
            if (!IsCommnetParticipant(vessel))
                return false;

            // IsConnectedHome is only set to true on the active vessel, so we have to manually
            // call the FindHome method to evaluate the home connection.

            // WARNING: In stock this will only work for vessels with a relay antenna installed.
            // This is a limitation put in place to improve performance in the stock game, and
            // there isn't a very good way around it.
            if (!vessel.isActiveVessel)
            {
                var net = CommNetNetwork.Instance.CommNet;
                net.FindHome(vessel.Connection.Comm, tempPath);
                bool result = tempPath.signalStrength > 0;
                tempPath.Clear();
                return result;
            }
            return vessel.Connection.IsConnectedHome;
        }

        public bool HasConnectionToControl(Vessel vessel)
        {
            if (!IsEnabled)
                return true;
            if (!IsCommnetParticipant(vessel))
                return false;

            // IsConnected is only set to true on the active vessel, so we have to manually
            // call the FindHome method to evaluate the home connection.

            // WARNING: In stock this will only work for vessels with a relay antenna installed.
            // This is a limitation put in place to improve performance in the stock game, and
            // there isn't a very good way around it.
            if (!vessel.isActiveVessel)
            {
                var net = CommNetNetwork.Instance.CommNet;
                net.FindClosestControlSource(vessel.Connection.Comm, tempPath);
                bool result = tempPath.signalStrength > 0 || vessel.CurrentControlLevel >= Vessel.ControlLevel.PARTIAL_MANNED;
                tempPath.Clear();
                return result;
            }
            return vessel.Connection.IsConnected || vessel.CurrentControlLevel >= Vessel.ControlLevel.PARTIAL_MANNED;
        }

        public bool HasConnection(Vessel vessel1, Vessel vessel2)
        {
            if (!IsEnabled)
                return true;
            if (!IsCommnetParticipant(vessel1) || !IsCommnetParticipant(vessel2))
                return false;

            // We next need to query the network to find a connection between the two vessels.
            // I found no exposed method for accessing cached paths directly, other than the
            // control path.

            // WARNING: In stock this will only work for vessels with a relay antenna installed.
            // This is a limitation put in place to improve performance in the stock game, and
            // there isn't a very good way around it.
            var net = CommNetNetwork.Instance.CommNet;
            bool result = vessel1.id == vessel2.id || net.FindPath(vessel1.Connection.Comm, tempPath, vessel2.Connection.Comm) || net.FindPath(vessel2.Connection.Comm, tempPath, vessel1.Connection.Comm);
            tempPath.Clear();
            return result;
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
            if (vessel != null)
            {
                vessel.OnPreAutopilotUpdate -= hook;
            }
        }
    }
}