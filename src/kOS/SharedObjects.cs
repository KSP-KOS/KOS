using kOS.Communication;
using kOS.Execution;
using kOS.Screen;
using kOS.Sound;
using System.Collections.Generic;

namespace kOS
{
    public class SharedObjects : Safe.SafeSharedObjects
    {
        public Vessel Vessel { get; set; }
        public ProcessorManager ProcessorMgr { get; set; }
        public Part KSPPart { get; set; }
        public TermWindow Window { get; set; }
        public TransferManager TransferManager { get; set; }
        public AddOns.AddonManager AddonManager { get; set; }
        public Dictionary<int, VoiceValue> AllVoiceValues { get; private set; }

        public SharedObjects()
        {
            AllVoiceValues = new Dictionary<int, VoiceValue>();
            GameEvents.onVesselDestroy.Add(OnVesselDestroy);
        }

        private void OnVesselDestroy(Vessel data)
        {
            if (data.id == Vessel.id)
            {
                BindingMgr.Dispose();
            }
        }
    }
}