using System;
using CommNet;
using kOS.Module;

namespace kOS.Communication
{
    public class StockConnectivityManager : IConnectivityManager
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

        public bool HasControl(Vessel vessel)
        {
            return true;
        }

        public bool HasConnection(Vessel vessel1, Vessel vessel2)
        {
            return true;
        }
    }
}