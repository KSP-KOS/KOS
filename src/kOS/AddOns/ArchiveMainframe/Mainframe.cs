using System;
using System.Collections.Generic;
using System.Linq;
using kOS.Safe;
using kOS.Binding;
using kOS.Safe.Compilation.KS;
using kOS.Persistence;
using kOS.Safe.Persistence;
using kOS.Communication;
using kOS.Safe.Function;
using kOS.Safe.Execution;
using kOS.Safe.Utilities;
using kOS.Callback;
using UnityEngine;

namespace kOS.AddOns.ArchiveMainframe
{
    [KSPAddon(KSPAddon.Startup.FlightEditorAndKSC, false)]
    class Mainframe : MonoBehaviour
    {
        public static Mainframe instance;
        public static Dump queueDump;
        public void Start()
        {
            Mainframe.instance = this;
            shared = new SharedMainframeObjects();
            
            shared.UpdateHandler = new UpdateHandler();
            shared.BindingMgr = new BindingManager(shared);
            shared.Interpreter = new Screen.ConnectivityInterpreter(shared);
            shared.Screen = shared.Interpreter;
            shared.ScriptHandler = new KSScript();
            shared.Logger = new KSPLogger(shared);
            shared.VolumeMgr = new ConnectivityVolumeManager(shared);
            shared.ProcessorMgr = new ProcessorManager();
            shared.FunctionManager = new FunctionManager(shared);
            shared.Cpu = new CPU(shared);
            shared.AddonManager = new AddOns.AddonManager(shared);
            shared.GameEventDispatchManager = new GameEventDispatchManager(shared);

            shared.Window = gameObject.AddComponent<Screen.TermWindow>();
            shared.Window.VirtualCPU = true;
            shared.Window.AttachTo(shared);

            processor = new MainframeProcessor(shared);
            shared.Processor = processor;

            // initialize archive
            shared.VolumeMgr.Add(new Archive(SafeHouse.ArchiveFolder));

            messageQueue = new MessageQueue();
            if (queueDump != null)
                messageQueue.LoadDump(queueDump);
        }

        private SharedObjects shared;
        public MainframeProcessor processor { get; private set; }
        public MessageQueue messageQueue { get; private set; }

        public void Update()
        {
            processor.Update();
        }

        public void OnDestroy()
        {
            queueDump = messageQueue.Dump();
        }
    }
}
