using System.Collections.Generic;
using System.Linq;

namespace kOS.Binding
{
    public static class SteeringManagerProvider
    {
        public static Dictionary<string, SteeringManager> AllInstances = new Dictionary<string, SteeringManager>();
        public static SteeringManager GetInstance(SharedObjects shared)
        {
            string key = shared.Vessel.id.ToString();
            if (AllInstances.Keys.Contains(key))
            {
                SteeringManager instance = AllInstances[key];
                if (!instance.SubscribedParts.Contains(shared.KSPPart.flightID))
                {
                    AllInstances[key].SubscribedParts.Add(shared.KSPPart.flightID);
                }
                return AllInstances[key];
            }

            SteeringManager sm = new SteeringManager(shared);

            sm.SubscribedParts.Add(shared.KSPPart.flightID);

            AllInstances.Add(key, sm);
            return sm;
        }

        public static void RemoveInstance(Vessel vessel)
        {
            var id = vessel.id.ToString();
            if (AllInstances.ContainsKey(id))
            {
                var instance = AllInstances[id];
                AllInstances.Remove(id);
                instance.Dispose();
            }
        }

        public static SteeringManager SwapInstance(SharedObjects shared, SteeringManager oldInstance)
        {
            if (shared.Vessel == oldInstance.Vessel) return oldInstance;
            if (oldInstance.SubscribedParts.Contains(shared.KSPPart.flightID)) oldInstance.SubscribedParts.Remove(shared.KSPPart.flightID);
            SteeringManager instance = SteeringManager.DeepCopy(oldInstance, shared);

            if (oldInstance.Enabled)
            {
                if (oldInstance.PartId == shared.KSPPart.flightID)
                {
                    oldInstance.DisableControl();
                    instance.EnableControl(shared);
                    instance.Value = oldInstance.Value;
                }
            }
            return instance;
        }
    }
}