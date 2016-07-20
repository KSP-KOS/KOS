namespace kOS.AddOns.RemoteTech
{
    public static class RemoteTechUtility
    {
        public static double GetTotalWaitTime(Vessel vessel)
        {
            double waitTotal = 0;

            if (RemoteTechHook.IsAvailable(vessel.id) && vessel.GetVesselCrew().Count == 0)
            {
                waitTotal = RemoteTechHook.Instance.GetShortestSignalDelay(vessel.id);
            }

            return waitTotal;
        }

        public static double GetInputWaitTime(Vessel vessel)
        {
            if (RemoteTechHook.Instance.HasLocalControl(vessel.id)) return 0d;
            else return GetTotalWaitTime(vessel);
        }

        public static bool HasConnectionOrControl(Vessel vessel)
        {
            return RemoteTechHook.Instance.HasAnyConnection(vessel.id) || RemoteTechHook.Instance.HasLocalControl(vessel.id);
        }
    }
}
