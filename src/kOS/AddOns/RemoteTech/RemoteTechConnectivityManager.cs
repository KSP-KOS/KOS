using System;
using kOS.Communication;
using kOS.Module;

namespace kOS.AddOns.RemoteTech
{
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
                return hookAvailable;
            }
        }

        public double GetDelay(Vessel vessel1, Vessel vessel2)
        {
            if (!IsEnabled)
                return 0;
            double delay = RemoteTechHook.Instance.GetSignalDelayToSatellite(vessel1.id, vessel2.id);
            return delay != double.PositiveInfinity ? delay : -1;
        }

        public double GetDelayToControl(Vessel vessel)
        {
            if (!IsEnabled)
                return 0;
            return RemoteTechUtility.GetInputWaitTime(vessel);
        }

        public double GetDelayToHome(Vessel ves)
        {
            if (!IsEnabled)
                return 0;
            double delay = RemoteTechHook.Instance.GetSignalDelayToKSC(ves.id);
            return delay != double.PositiveInfinity ? delay : -1;
        }

        public bool HasConnection(Vessel ves1, Vessel ves2)
        {
            if (!IsEnabled)
                return true;
            return GetDelay(ves1, ves2) >= 0;
        }

        public bool HasConnectionToHome(Vessel ves)
        {
            if (!IsEnabled)
                return true;
            return RemoteTechHook.Instance.HasConnectionToKSC(ves.id);
        }

        public bool HasControl(Vessel ves)
        {
            if (!IsEnabled)
                return true;
            return RemoteTechHook.Instance.HasAnyConnection(ves.id);
        }
    }
}