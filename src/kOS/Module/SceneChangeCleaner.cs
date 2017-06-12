using kOS.Suffixed;
using UnityEngine;

namespace kOS.Module
{
    [KSPAddon(KSPAddon.Startup.AllGameScenes, false)]
    public class SceneChangeCleaner : MonoBehaviour
    {
        public void Awake()
        {
            VesselTarget.ClearInstanceCache();
            BodyTarget.ClearInstanceCache();
            Destroy(this);
        }
    }
}
