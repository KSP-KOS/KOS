using UnityEngine;

namespace kOS.AddOns.InfernalRobotics
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class IRHandler : MonoBehaviour
    {
        private void Awake()
        {
            GameEvents.onVesselChange.Add(OnVesselChange);
            GameEvents.onVesselWasModified.Add(OnVesselWasModified);

        }
        public void Start()
        {
            IRWrapper.InitWrapper();
        }
        private void OnVesselChange(Vessel v)
        {
            //if the scene was loaded on non-IR Vessel and then IR vessel became focused we might need to re-init the API
            if (!IRWrapper.APIReady)
                IRWrapper.InitWrapper();
           
        }

        private void OnVesselWasModified(Vessel v)
        {
            //in case some IR capable vessel docked with this one, we might need to re-init the IR API
            if (v == FlightGlobals.ActiveVessel)
            {
                OnVesselChange(v);
            }
        }
        public void OnDestroy()
        {
            GameEvents.onVesselChange.Remove(OnVesselChange);
            GameEvents.onVesselWasModified.Remove(OnVesselWasModified);
        }
    }
}