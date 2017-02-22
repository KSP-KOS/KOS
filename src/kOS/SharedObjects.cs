using kOS.Communication;
using kOS.Execution;
using kOS.Screen;
using kOS.Callback;
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
        public List<KOSManagedWindow> ManagedWindows { get; private set; }
        public TransferManager TransferManager { get; set; }
        public AddOns.AddonManager AddonManager { get; set; }
        public GameEventDispatchManager DispatchManager
        {
            get { return (GameEventDispatchManager)GameEventDispatchManager; }
        }
        public Dictionary<int, VoiceValue> AllVoiceValues { get; private set; }

        public SharedObjects()
        {
            ManagedWindows = new List<KOSManagedWindow>();
            AllVoiceValues = new Dictionary<int, VoiceValue>();
        }

        public void AddWindow(KOSManagedWindow w)
        {
            ManagedWindows.Add(w);
        }

        public void RemoveWindow(KOSManagedWindow w)
        {
            ManagedWindows.Remove(w);
        }

        public void DestroyObjects()
        {
            if (BindingMgr != null) { BindingMgr.Dispose(); }
            if (Window != null) { UnityEngine.Object.Destroy(Window); }
            if (SoundMaker != null) { SoundMaker.StopAllVoices(); }
            var props = typeof(SharedObjects).GetProperties();
            foreach (var prop in props)
            {
                if (!prop.PropertyType.IsValueType)
                {
                    prop.SetValue(this, null, null);
                }
            }
        }
    }
}
