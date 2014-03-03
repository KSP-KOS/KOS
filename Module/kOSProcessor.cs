using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using KSP.IO;
using kOS.InterProcessor;
using kOS.Binding;
using kOS.Persistence;
using kOS.Suffixed;

namespace kOS.Module
{
    public class kOSProcessor : PartModule
    {
        public enum Modes { READY, STARVED, OFF };
        public Modes Mode = Modes.READY;

        public Harddisk HardDisk { get; private set; }
        private int vesselPartCount = 0;
        private SharedObjects _shared = null;

        //640K ought to be enough for anybody -sic
        public const int PROCESSOR_HARD_CAP = 655360;

        [KSPField(isPersistant = true, guiName = "kOS Disk Space", guiActive = true)]
        public int diskSpace = 500;

        [KSPField(isPersistant = true, guiActive = false)] public int MaxPartID = 100;

        [KSPField(isPersistant = true, guiName = "kOS Unit ID", guiActive = true, guiActiveEditor = true)] private int
            unitID = -1;

        [KSPEvent(guiActive = true, guiName = "Open Terminal", category = "skip_delay;")]
        public void Activate()
        {
            UnityEngine.Debug.Log("kOS: Activate");
            Core.OpenWindow(_shared);
        }

        [KSPField(isPersistant = true, guiName = "Required Power", guiActive = true)] 
        public float RequiredPower;

        [KSPEvent(guiActive = true, guiName = "Toggle Power")]
        public void TogglePower()
        {
            UnityEngine.Debug.Log("kOS: Toggle Power");
            Modes newMode = (Mode != Modes.OFF) ? Modes.OFF : Modes.STARVED;
            SetMode(newMode);
        }
        
        [KSPAction("Open Terminal", actionGroup = KSPActionGroup.None)]
        public void Activate(KSPActionParam param)
        {
            UnityEngine.Debug.Log("kOS: Open Terminal from Dialog");
            Activate();
        }

        [KSPAction("Close Terminal", actionGroup = KSPActionGroup.None)]
        public void Deactivate(KSPActionParam param)
        {
            UnityEngine.Debug.Log("kOS: Close Terminal from ActionGroup");
            Core.CloseWindow(_shared);
        }

        [KSPAction("Toggle Power", actionGroup = KSPActionGroup.None)]
        public void TogglePower(KSPActionParam param)
        {
            UnityEngine.Debug.Log("kOS: Toggle Power from ActionGroup");
            TogglePower();
        }

        [KSPEvent(guiName = "Unit +", guiActive = false, guiActiveEditor = true)]
        public void IncrementUnitId()
        {
            unitID++;
            throw new NotImplementedException();
        }

        [KSPEvent(guiName = "Unit -", guiActive = false, guiActiveEditor = true)]
        public void DecrementUnitId()
        {
            unitID--;
            throw new NotImplementedException();
        }

        public override void OnStart(StartState state)
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
                CreateFactory();
                    
                _shared.Vessel = this.vessel;
                _shared.Processor = this;
                _shared.UpdateHandler = new UpdateHandler();
                _shared.BindingMgr = new BindingManager(_shared);
                _shared.Interpreter = _shared.Factory.CreateInterpreter(_shared);
                _shared.Screen = _shared.Interpreter;
                _shared.ScriptHandler = new Compilation.KS.KSScript();
                _shared.Logger = new Logger(_shared);
                _shared.VolumeMgr = new VolumeManager(_shared);
                _shared.ProcessorMgr = new ProcessorManager(_shared);
                _shared.Cpu = new kOS.Execution.CPU(_shared);

                // initialize the file system
                _shared.VolumeMgr.Add(new Archive());
                if (HardDisk == null) HardDisk = new Harddisk(Mathf.Min(diskSpace, PROCESSOR_HARD_CAP));
                _shared.VolumeMgr.Add(HardDisk);
                _shared.VolumeMgr.SwitchTo(HardDisk);
            }
        }

        private void CreateFactory()
        {
            // TODO: replace the 'false' with a check to identify if RT2 is available
            if (Config.GetInstance().EnableRT2Integration && false)
            {
                _shared.Factory = new kOS.AddOns.RemoteTech2.RemoteTechFactory();
            }
            else
            {
                _shared.Factory = new kOS.Factories.StandardFactory();
            }
        }

        public void RegisterkOSExternalFunction(object[] parameters)
        {
            //Debug.Log("*** External Function Registration Succeeded");
            //cpu.RegisterkOSExternalFunction(parameters);
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
            if (_shared == null) return;

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
                if (_shared.UpdateHandler != null) _shared.UpdateHandler.UpdateObservers(Time.deltaTime);
                UpdateParts();
            }

            ProcessElectricity(this.part, TimeWarp.fixedDeltaTime);
        }
        
        public void UpdateParts()
        {
            // Trigger whenever the number of parts in the vessel changes (like when staging, docking or undocking)
            if (vessel.parts.Count != vesselPartCount)
            {
                bool missingHardDisks = false;
                List<Volume> attachedVolumes = new List<Volume>();
                List<kOSProcessor> processors = new List<kOSProcessor>();

                // Look for all the processors that exists in the vessel
                foreach (Part part in vessel.parts)
                {
                    kOSProcessor processorPart;
                    if (PartIsKosProc(part, out processorPart))
                    {
                        processors.Add(processorPart);

                        // A harddisk may be null because the kOS part haven't been initialized yet
                        // Wait until the next update and everything should be fine
                        if (processorPart.HardDisk != null)
                        {
                            attachedVolumes.Add(processorPart.HardDisk);
                        }
                        else
                        {
                            missingHardDisks = true;
                            break;
                        }
                    }
                }

                if (!missingHardDisks)
                {
                    _shared.VolumeMgr.UpdateVolumes(attachedVolumes);
                    _shared.ProcessorMgr.UpdateProcessors(processors);
                    vesselPartCount = vessel.parts.Count;
                }
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
                HardDisk = newDisk;
            }

            InitObjects();

            if (_shared != null && _shared.Cpu != null)
            {
                _shared.Cpu.OnLoad(node);
            }
            base.OnLoad(node);
        }

        public override void OnSave(ConfigNode node)
        {
            if (HardDisk != null)
            {
                ConfigNode hdNode = HardDisk.Save("harddisk");
                node.AddNode(hdNode);
            }

            if (_shared != null && _shared.Cpu != null)
            {
                _shared.Cpu.OnSave(node);
                Config.GetInstance().SaveConfig();
            }

            base.OnSave(node);
        }

        private void ProcessElectricity(Part part, float time)
        {
            if (Mode == Modes.OFF) return;

            RequiredPower = (float)Math.Round((double)_shared.VolumeMgr.CurrentVolume.RequiredPower(), 2);
            var electricReq = time * RequiredPower;
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

        public void ExecuteInterProcCommand(InterProcCommand command)
        {
            if (command != null)
            {
                command.Execute(_shared);
            }
        }
    }
}
