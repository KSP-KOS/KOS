using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace kOS
{
    public class kOSProcessor : PartModule
    {
        public enum Modes { READY, STARVED, OFF };
        public Modes Mode = Modes.READY;
        public Harddisk hardDisk = null;
        private int vesselPartCount = 0;
        private SharedObjects _shared;
        private static int MemSize = 10000;

        [KSPEvent(guiActive = true, guiName = "Open Terminal")]
        public void Activate()
        {
            Core.OpenWindow(_shared);
        }

        [KSPEvent(guiActive = true, guiName = "Toggle Power")]
        public void TogglePower()
        {
            Modes newMode;

            if (Mode != Modes.OFF)
            {
                newMode = Modes.OFF;
            }
            else
            {
                newMode = Modes.STARVED;
            }

            SetMode(newMode);
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
        public int UnitID = -1;

        [KSPField(isPersistant = true, guiActive = false)]
        public int MaxPartID = 0;

        public override void OnStart(PartModule.StartState state)
        {
            //Do not start from editor and at KSP first loading
            if (state == StartState.Editor || state == StartState.None)
            {
                return;
            }

            InitObjects();
        }

        public void InitObjects()
        {
            if (_shared == null)
            {
                _shared = new SharedObjects();
                _shared.Vessel = this.vessel;
                _shared.Processor = this;
                _shared.BindingMgr = new BindingManager(_shared);
                _shared.Interpreter = new Interpreter(_shared);
                _shared.Screen = _shared.Interpreter;
                _shared.ScriptHandler = new KS.KSScript();
                _shared.Logger = new Logger(_shared);
                _shared.VolumeMgr = new VolumeManager(_shared);
                _shared.Cpu = new CPU(_shared);

                // initialize the file system
                _shared.VolumeMgr.Add(new Archive());
                if (hardDisk == null) hardDisk = new Harddisk(MemSize);
                _shared.VolumeMgr.Add(hardDisk);
                _shared.VolumeMgr.SwitchTo(hardDisk);
            }
        }

        public void RegisterkOSExternalFunction(object[] parameters)
        {
            //Debug.Log("*** External Function Registration Succeeded");
            //cpu.RegisterkOSExternalFunction(parameters);
        }
        
        public void Update()
        {
            if (part.State == PartStates.DEAD)
            {
                Mode = Modes.OFF;
                return;
            }

            if (_shared != null && _shared.Vessel != this.vessel)
            {
                _shared.Vessel = this.vessel;
            }

            if (Mode == Modes.READY)
            {
                if (_shared.Cpu != null) _shared.Cpu.Update(Time.deltaTime);
                UpdateParts();
            }
            
            ProcessElectricity(this.part, TimeWarp.fixedDeltaTime);
        }

        public void UpdateParts()
        {
            // Trigger whenever the number of parts in the vessel changes (like when staging, docking or undocking)
            if (vessel.parts.Count != vesselPartCount)
            {
                List<Volume> attachedVolumes = new List<Volume>();
                attachedVolumes.Add(hardDisk);

                // Look for sister units that have newly been added to the vessel
                List<kOSProcessor> sisterProcs = new List<kOSProcessor>();
                foreach (Part part in vessel.parts)
                {
                    kOSProcessor sisterProc;
                    if (part != this.part && PartIsKosProc(part, out sisterProc))
                    {
                        sisterProcs.Add(sisterProc);
                        attachedVolumes.Add(sisterProc.hardDisk);
                    }
                }

                _shared.VolumeMgr.UpdateVolumes(attachedVolumes);
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
            // KSP Seems to want to make an instance of my partModule during initial load
            if (vessel == null) return;

            if (node.HasNode("harddisk"))
            {
                Harddisk newDisk = new Harddisk(node.GetNode("harddisk"));
                hardDisk = newDisk;
            }

            InitObjects();

            if (_shared.Cpu != null)
            {
                _shared.Cpu.OnLoad(node);
            }
            
            base.OnLoad(node);
        }

        public override void OnSave(ConfigNode node)
        {
            if (hardDisk != null)
            {
                ConfigNode hdNode = hardDisk.Save("harddisk");
                node.AddNode(hdNode);
            }

            if (_shared.Cpu != null)
            {
                _shared.Cpu.OnSave(node);
            }

            Config.GetInstance().SaveConfig();

            base.OnSave(node);
        }
        
        private void ProcessElectricity(Part part, float time)
        {
            if (Mode == Modes.OFF) return;

            var electricReq = 0.01f * time;
            var result = part.RequestResource("ElectricCharge", electricReq) / electricReq;

            var newMode = (result < 0.5f) ? Modes.STARVED : Modes.READY;
            SetMode(newMode);
        }

        public void SetMode(Modes newMode)
        {
            if (newMode != Mode)
            {
                switch (newMode)
                {
                    case Modes.READY:
                        if (Mode == Modes.STARVED && _shared.Cpu != null) _shared.Cpu.Boot();
                        if (_shared.Interpreter != null) _shared.Interpreter.SetInputLock(false);
                        if (_shared.Window != null) _shared.Window.SetPowered(true);
                        break;

                    case Modes.OFF:
                    case Modes.STARVED:
                        if (_shared.Interpreter != null) _shared.Interpreter.SetInputLock(true);
                        if (_shared.Window != null) _shared.Window.SetPowered(false);
                        break;
                }

                Mode = newMode;
            }
        }
    }
}
