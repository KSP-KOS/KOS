using KSP.IO;
using System.Collections.Generic;
using UnityEngine;

namespace kOS
{
    public class kOSProcessor : PartModule
    {
        private CPU cpu;
        private Harddisk hardDisk;
        private int vesselPartCount;
        private readonly List<kOSProcessor> sisterProcs = new List<kOSProcessor>();
        private const int MEM_SIZE = 10000;

        [KSPEvent(guiActive = true, guiName = "Open Terminal")]
        public void Activate()
        {
            Core.OpenWindow(cpu);
        }

        [KSPEvent(guiActive = true, guiName = "Toggle Power")]
        public void TogglePower()
        {
            if (cpu == null) return;

            cpu.Mode = cpu.Mode != CPU.Modes.OFF ? CPU.Modes.OFF : CPU.Modes.STARVED;
        }

        [KSPAction("Open Terminal", actionGroup = KSPActionGroup.None)]
        public void Activate(KSPActionParam param) {
            Activate();
        }

        [KSPAction("Close Terminal", actionGroup = KSPActionGroup.None)]
        public void Deactivate(KSPActionParam param) {
            Core.CloseWindow(cpu);
        }

        [KSPAction("Toggle Power", actionGroup = KSPActionGroup.None)]
        public void TogglePower(KSPActionParam param) {
            TogglePower();
        }

        [KSPField(isPersistant = true, guiName = "kOS Unit ID", guiActive = true)]
        public int UnitID = -1;

        [KSPField(isPersistant = true, guiActive = false)]
        public int MaxPartID = 0;

        public override void OnStart(StartState state)
        {
            //Do not start from editor and at KSP first loading
            if (state == StartState.Editor || state == StartState.None)
            {
                return;
            }

            if (hardDisk == null) hardDisk = new Harddisk(MEM_SIZE);

            InitCpu();

        }

        public void InitCpu()
        {
            if (cpu != null) return;
            cpu = new CPU(this, "ksp");
            cpu.AttachHardDisk(hardDisk);
            cpu.Boot();
        }

        public void RegisterkOSExternalFunction(object[] parameters)
        {
            UnityEngine.Debug.Log("*** External Function Registration Succeeded");

            cpu.RegisterkOSExternalFunction(parameters);
        }
        
        public static int AssignNewID()
        {
            var config = PluginConfiguration.CreateForType<kOSProcessor>();
            config.load();
            var id = config.GetValue<int>("CpuIDMax") + 1;
            config.SetValue("CpuIDMax", id);
            config.save();

            return id;
        }
        
        public void Update()
        {
            if (cpu == null) return;

            if (part.State == PartStates.DEAD)
            {
                cpu.Mode = CPU.Modes.OFF;
                return;
            }

            cpu.Update(Time.deltaTime);

            cpu.ProcessElectricity(part, TimeWarp.fixedDeltaTime);

            UpdateParts();
        }

        public void UpdateParts()
        {
            // Trigger whenever the number of parts in the vessel changes (like when staging, docking or undocking)
            if (vessel.parts.Count == vesselPartCount) return;

            var attachedVolumes = new List<Volume> {cpu.Archive, hardDisk};

            // Look for sister units that have newly been added to the vessel
            sisterProcs.Clear();
            foreach (var item in vessel.parts)
            {
                kOSProcessor sisterProc;
                if (item == part || !PartIsKosProc(item, out sisterProc)) continue;

                sisterProcs.Add(sisterProc);
                attachedVolumes.Add(sisterProc.hardDisk);
            }

            cpu.UpdateVolumeMounts(attachedVolumes);

            vesselPartCount = vessel.parts.Count;
        }

        public bool PartIsKosProc(Part input, out kOSProcessor proc)
        {
            foreach (PartModule module in input.Modules)
            {
                var processor = module as kOSProcessor;
                if (processor == null) continue;
                proc = processor;
                return true;
            }

            proc = null;
            return false;
        }

        public override void OnFixedUpdate()
        {
            
        }

        public override void OnLoad(ConfigNode node)
        {
            // KSP Seems to want to make an instance of my partModule during initial load
            if (vessel == null) return;

            foreach (var hdNode in node.GetNodes("harddisk"))
            {
                var newDisk = new Harddisk(hdNode);
                hardDisk = newDisk;
            }

            UnityEngine.Debug.Log("******************************* ON LOAD ");

            InitCpu();

            UnityEngine.Debug.Log("******************************* CPU Inited ");

            if (cpu != null) cpu.OnLoad(node);
            
            base.OnLoad(node);
        }

        public override void OnSave(ConfigNode node)
        {
            if (hardDisk != null)
            {
                var hdNode = hardDisk.Save("harddisk");
                node.AddNode(hdNode);
            }

            if (cpu != null)
            {
                cpu.OnSave(node);
            }

            base.OnSave(node);
        }
    }
}
