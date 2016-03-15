using UnityEngine;

namespace kOS.AddOns.InfernalRobotics
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class IRHandler : MonoBehaviour
    {
        private bool initPending = true;

        private void Awake()
        {
            GameEvents.onVesselChange.Add(OnVesselChange);
            GameEvents.onVesselWasModified.Add(OnVesselWasModified);
            GameEvents.onVesselLoaded.Add(OnVesselLoaded);

        }
        public void Start()
        {
            initPending = true;
        }

        public void FixedUpdate()
        {
            //due to dll order incositency had to move initialization into FixedUpdate
            if(initPending)
            {
                //if the scene was loaded on non-IR Vessel and then IR vessel became focused we might need to re-init the API
                if (!IRWrapper.APIReady)
                    IRWrapper.InitWrapper();

                UnityEngine.Debug.Log ("KOS-IR: FixedUpdate reinit: " + IRWrapper.APIReady);

                initPending = false;
            }
        }

        private void OnVesselChange(Vessel v)
        {
            //if the scene was loaded on non-IR Vessel and then IR vessel became focused we might need to re-init the API
            initPending = true;  
        }

        private void OnVesselLoaded(Vessel v)
        {
            initPending = true;
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
            GameEvents.onVesselLoaded.Remove(OnVesselLoaded);
        }
    }
}