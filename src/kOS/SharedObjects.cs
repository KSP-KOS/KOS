using kOS.Communication;
using kOS.Execution;
using kOS.Screen;
using kOS.Callback;
using kOS.Sound;
using System.Collections.Generic;
using System;
using kOS.Suffixed;

namespace kOS
{
    public class SharedObjects : Safe.SafeSharedObjects
    {
        public Vessel Vessel { get; set; }
        public VesselTarget VesselTarget { get { return VesselTarget.CreateOrGetExisting(this); } }
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

        public Dictionary<ManeuverNode, Node> NodeLookup { get; private set; }

        public SharedObjects()
        {
            ManagedWindows = new List<KOSManagedWindow>();
            AllVoiceValues = new Dictionary<int, VoiceValue>();
            NodeLookup = new Dictionary<ManeuverNode, Node>();
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
            if (Window != null) { UnityEngine.Object.Destroy(Window); }
            if (SoundMaker != null) { SoundMaker.StopAllVoices(); }
            if (UpdateHandler != null) { UpdateHandler.ClearAllObservers(); }
            if (GameEventDispatchManager != null) { GameEventDispatchManager.Clear(); }
            if (Interpreter != null) { Interpreter.RemoveAllResizeNotifiers(); }
            var props = typeof(SharedObjects).GetProperties();
            IDisposable tempDisp;
            foreach (var prop in props)
            {
                if (!prop.PropertyType.IsValueType && prop.GetSetMethod(nonPublic: true) != null)
                {
                    tempDisp = prop.GetValue(this, null) as IDisposable;
                    if (tempDisp != null)
                    {
                        tempDisp.Dispose();
                    }
                    prop.SetValue(this, null, null);
                }
            }
        }
    }
}
