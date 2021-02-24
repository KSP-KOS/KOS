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
            if (newProcessorMode == ProcessorModes.READY)
                firstUpdate = true;
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
