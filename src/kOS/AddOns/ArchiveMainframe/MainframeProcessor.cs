using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using kOS.Safe;
using kOS.Safe.Module;
using kOS.Safe.Persistence;
using kOS.Safe.Utilities;

namespace kOS.AddOns.ArchiveMainframe
{
    class MainframeProcessor : IProcessor
    {
        public MainframeProcessor(SharedObjects shared)
        {
            this.shared = shared;
        }
        private bool firstUpdate = true;
        private SharedObjects shared;
        public VolumePath BootFilePath { get { return VolumePath.FromString("boot/archive"); } }
        public bool CheckCanBoot() { return true; }
        public string Tag { get { return ""; } }
        public int KOSCoreId { get { return -1; } }
        public void SetMode(ProcessorModes newProcessorMode)
        {
            switch (newProcessorMode)
            {
                case ProcessorModes.READY:
                    shared.VolumeMgr.SwitchTo(shared.VolumeMgr.GetVolume(0));
                    if (shared.Interpreter != null) shared.Interpreter.SetInputLock(false);
                    firstUpdate = true;
                    if (shared.Window != null) shared.Window.IsPowered = true;
                    foreach (var w in shared.ManagedWindows) w.IsPowered = true;
                    break;
                case ProcessorModes.OFF:
                    if (shared.Cpu != null) shared.Cpu.BreakExecution(true);
                    if (shared.Interpreter != null) shared.Interpreter.SetInputLock(true);
                    if (shared.Window != null) shared.Window.IsPowered = false;
                    if (shared.SoundMaker != null) shared.SoundMaker.StopAllVoices();
                    foreach (var w in shared.ManagedWindows) w.IsPowered = false;
                    break;
            }
        }

        public void OpenWindow()
        {
            shared.Window.Open();
        }

        public void CloseWindow()
        {
            shared.Window.Close();
        }

        public void ToggleWindow()
        {
            shared.Window.Toggle();
        }

        public bool WindowIsOpen()
        {
            return shared.Window.IsOpen;
        }

        public bool TelnetIsAttached()
        {
            return shared.Window.NumTelnets() > 0;
        }

        public void Update()
        {
            if (firstUpdate)
            {
                firstUpdate = false;
                shared.Cpu.Boot(new string[] { "archive" });
            }

            // Mainframe runs in realtime, not physics time
            if (shared.UpdateHandler != null) shared.UpdateHandler.UpdateObservers(TimeWarp.deltaTime);
            if (shared.UpdateHandler != null) shared.UpdateHandler.UpdateFixedObservers(TimeWarp.fixedDeltaTime);
        }
    }
}
