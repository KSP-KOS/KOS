using UnityEngine;
using Debug = UnityEngine.Debug;

namespace kOS.AddOns.InfernalRobotics
{
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public class IRHandler : MonoBehaviour
    {
        public void Start()
        {
            IRWrapper.InitWrapper();
            if (IRWrapper.APIReady)
            {
                
            }
        }

        public void OnDestroy()
        {
            if (IRWrapper.APIReady)
            {
            }
        }
    }
}