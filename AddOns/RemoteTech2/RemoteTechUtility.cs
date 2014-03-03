using System;

namespace kOS.AddOns.RemoteTech2
{
    public static class RemoteTechUtility
    {
        public static double GetTotalWaitTime(Vessel vessel)
        {
            double waitTotal = 0;

            if (RemoteTechHook.Instance != null && vessel.GetVesselCrew().Count == 0)
            {
                waitTotal = RemoteTechHook.Instance.GetShortestSignalDelay(vessel.id);
            }

            return waitTotal;
        }
    }
}
