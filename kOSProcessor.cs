using KSP.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace kOS
{
    public class kOSProcessor : PartModule
    {
        public CPU cpu;
        public Harddisk hardDisk = null;
        private int vesselPartCount = 0;
        private List<kOSProcessor> sisterProcs = new List<kOSProcessor>();

        private static int MemSize = 10000;
        private static int cpuIdMax;

        [KSPEvent(guiActive = true, guiName = "Open Terminal")]
        public void Activate()
        {
            Core.OpenWindow(cpu);
        }

        [KSPEvent(guiActive = true, guiName = "Toggle Power")]
        public void TogglePower()
        {
            if (cpu == null) return;

            if (cpu.Mode != CPU.Modes.OFF)
            {
                cpu.Mode = CPU.Modes.OFF;
            }
            else
            {
                cpu.Mode = CPU.Modes.STARVED;
            }
        }

        [KSPAction("Open Terminal", actionGroup = KSPActionGroup.None)]
        public void Activate(KSPActionParam param) {
            Activate();
        }

        [KSPAction("Toggle Power", actionGroup = KSPActionGroup.None)]
        public void TogglePower(KSPActionParam param) {
            TogglePower();
        }

        [KSPField(isPersistant = true, guiName = "kOS Unit ID", guiActive = true)]
        public int ID = -1;

        public override void OnStart(PartModule.StartState state)
        {
            //Do not start from editor and at KSP first loading
            if (state == StartState.Editor || state == StartState.None)
            {
                return;
            }

            cpu = new CPU(this, "ksp");

            if (hardDisk == null) hardDisk = new Harddisk(MemSize);

            cpu.AttachHardDisk(hardDisk);
            cpu.Boot();

            //PluginConfiguration config = PluginConfiguration.CreateForType<kOSProcessor>();
            //config.load();
            //ID = config.GetValue<int>("CpuIDMax") + 1;
        }
        
        public void Update()
        {
            if (part.State == PartStates.DEAD)
            {
                cpu.Mode = CPU.Modes.OFF;
                return;
            }

            cpu.Update(Time.deltaTime);

            cpu.ProcessElectricity(this.part, TimeWarp.fixedDeltaTime);

            UpdateParts();
        }

        public void UpdateParts()
        {
            // Trigger whenever the number of parts in the vessel changes (like when staging, docking or undocking)
            if (vessel.parts.Count != vesselPartCount)
            {
                List<Volume> attachedVolumes = new List<Volume>();
                attachedVolumes.Add(CPU.archive);
                attachedVolumes.Add(this.hardDisk);

                // Look for sister units that have newly been added to the vessel
                sisterProcs.Clear();
                foreach (Part part in vessel.parts)
                {
                    kOSProcessor sisterProc;
                    if (part != this.part && PartIsKosProc(part, out sisterProc))
                    {
                        sisterProcs.Add(sisterProc);
                        attachedVolumes.Add(sisterProc.hardDisk);
                    }
                }

                cpu.UpdateVolumeMounts(attachedVolumes);

                vesselPartCount = vessel.parts.Count;
            }
        }

        public bool PartIsKosProc(Part input, out kOSProcessor proc)
        {
            foreach (PartModule module in input.Modules)
            {
                if (module is kOSProcessor)
                {
                    proc = (kOSProcessor)module;
                    return true;
                }
            }

            proc = null;
            return false;
        }

        public override void OnFixedUpdate()
        {
            
        }

        public override void OnLoad(ConfigNode node)
        {
            //PluginConfiguration config = PluginConfiguration.CreateForType<kOSProcessor>();
            //config.load();
            //ID = config.GetValue<int>("CpuIDMax") + 1;

            foreach (ConfigNode hdNode in node.GetNodes("harddisk"))
            {
                Harddisk newDisk = new Harddisk(hdNode);
                this.hardDisk = newDisk;
            }
            
            base.OnLoad(node);
        }

        public override void OnSave(ConfigNode node)
        {
            //PluginConfiguration config = PluginConfiguration.CreateForType<kOSProcessor>();
            //config.SetValue("CpuIDMax", ID);
            //config.save();

            if (hardDisk != null)
            {
                ConfigNode hdNode = hardDisk.Save("harddisk");
                node.AddNode(hdNode);
            }

            base.OnSave(node);
        }
    }
}
