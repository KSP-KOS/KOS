using System;
using kOS.Communication;

namespace kOS.AddOns.RemoteTech
{
    public class RemoteTechConnectivityManager : ConnectivityManager
    {
        public RemoteTechConnectivityManager()
        {
        }

        public double GetDelay(Vessel vessel1, Vessel vessel2)
        {
            double delay = RemoteTechHook.Instance.GetSignalDelayToSatellite(vessel1.id, vessel2.id);
            return delay != Single.PositiveInfinity ? delay : -1;
        }

    }
}

