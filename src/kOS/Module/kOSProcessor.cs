using System;
using System.Collections.Generic;
using System.Linq;
using kOS.AddOns.RemoteTech;
using kOS.Execution;
using kOS.Factories;
using kOS.Function;
using kOS.Safe.Persistence;
using kOS.Safe.Utilities;
using kOS.Utilities;
using UnityEngine;
using KSP.IO;
using kOS.InterProcessor;
using kOS.Binding;
using kOS.Persistence;
using kOS.Safe;
using kOS.Safe.Compilation;
using kOS.Safe.Compilation.KS;
using kOS.Safe.Module;
using kOS.Safe.Screen;
using kOS.Suffixed;

using KSPAPIExtensions;
using FileInfo = kOS.Safe.Encapsulation.FileInfo;

namespace kOS.Module
{
    public class kOSProcessor : PartModule, IProcessor, IPartCostModifier, IPartMassModifier
    {
        public ProcessorModes ProcessorMode = ProcessorModes.READY;

        public Harddisk HardDisk { get; private set; }
        private int vesselPartCount;
        private SharedObjects shared;
        private static readonly List<kOSProcessor> allMyInstances = new List<kOSProcessor>();
        private bool firstUpdate = true;

        //640K ought to be enough for anybody -sic
        private const int PROCESSOR_HARD_CAP = 655360;

        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Boot File"), UI_ChooseOption(scene=UI_Scene.Editor)]
        public string bootFile = "boot.ks";
        
        [KSPField(isPersistant = true, guiName = "kOS Disk Space", guiActive = true)]
        public int diskSpace = 1024;

        [KSPField(isPersistant = true, guiName = "kOS Base Disk Space", guiActive = false)]
        public int baseDiskSpace = 0;

        [KSPField(isPersistant = true, guiName = "kOS Base Module Cost", guiActive = false)]
        public float baseModuleCost = 0F;

        [KSPField(isPersistant = true, guiName = "kOS Base Part Mass", guiActive = false)]
        public float basePartMass = 0F;

        [KSPField(isPersistant = true)]
        public float maximumControllableMass = float.MaxValue;

        private bool massEnforcedPowerDown = false;

        [KSPField(isPersistant = false, guiName = "kOS Disk Space", guiActive = false, guiActiveEditor = true), UI_ChooseOption(scene = UI_Scene.Editor)]
        public string diskSpaceUI = "1024";

        [KSPField(isPersistant = true, guiName = "CPU/Disk Upgrade Cost", guiActive = false, guiActiveEditor = true)]
        public float additionalCost = 0F;

        [KSPField(isPersistant = false, guiName = "CPU/Disk Upgrade Mass", guiActive = false, guiActiveEditor = true)]
        public float additionalMass = 0F;

        [KSPField(isPersistant = true, guiActive = false)] public int MaxPartId = 100;

        [KSPEvent(guiActive = true, guiName = "Open Terminal", category = "skip_delay;")]
        public void Activate()
        {
            SafeHouse.Logger.Log("Activate");
            OpenWindow();
        }

        [KSPField(isPersistant = true, guiName = "Required Power", guiActive = true)] 
        public float RequiredPower;

        [KSPEvent(guiActive = true, guiName = "Toggle Power")]
        public void TogglePower()
        {
            SafeHouse.Logger.Log("Toggle Power");
            ProcessorModes newProcessorMode = (ProcessorMode != ProcessorModes.OFF) ? ProcessorModes.OFF : ProcessorModes.STARVED;
            SetMode(newProcessorMode);
            if (newProcessorMode == ProcessorModes.OFF)
                massEnforcedPowerDown = false;
        }
        
        [KSPAction("Open Terminal", actionGroup = KSPActionGroup.None)]
        public void Activate(KSPActionParam param)
        {
            SafeHouse.Logger.Log("Open Terminal from Dialog");
            Activate();
        }

        [KSPAction("Close Terminal", actionGroup = KSPActionGroup.None)]
        public void Deactivate(KSPActionParam param)
        {
            SafeHouse.Logger.Log("Close Terminal from ActionGroup");
            CloseWindow();
        }

        [KSPAction("Toggle Terminal", actionGroup = KSPActionGroup.None)]
        public void Toggle(KSPActionParam param)
        {
            SafeHouse.Logger.Log("Toggle Terminal from ActionGroup");
            ToggleWindow();
        }

        [KSPAction("Toggle Power", actionGroup = KSPActionGroup.None)]
        public void TogglePower(KSPActionParam param)
        {
            SafeHouse.Logger.Log("Toggle Power from ActionGroup");
            TogglePower();
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

        public IScreenBuffer GetScreen()
        {
            return shared.Screen;
        }
        
        // TODO - later refactor making this kOS.Safer so it can work on ITermWindow, which also means moving all of UserIO's classes too.
        public Screen.TermWindow GetWindow()
        {
            return shared.Window;
        }

        //returns basic information on kOSProcessor module in Editor
        public override string GetInfo()
        {
            const float MAXIMUM_POWER_CONSUMPTION = 0.2F;
            string moduleInfo = "KOS Processor\n";

            moduleInfo += "\nDefault disk capacity: " + diskSpace;

            moduleInfo += "\nMax Power consuption, EC/s : " + System.Math.Round(MAXIMUM_POWER_CONSUMPTION, 2);

            if (additionalCost > 0)
            {
                moduleInfo += "\nCost of probe CPU upgrade: " + System.Math.Round(additionalCost,0);
            }

            return moduleInfo;
        }

        //implement IPartCostModifier component
        public float GetModuleCost(float defaultCost)
        {
            return additionalCost;
        }

        private void UpdateCostAndMass()
        {
            const float DISK_SPACE_MASS_MULTIPLIER = 0.0000048829F; //implies approx 20kg for 4096bytes of diskSpace
            const float DISK_SPACE_COST_MULTIPLIER = 0.0244140625F; //implies approx 100funds for 4096bytes of diskSpace

            additionalCost = baseModuleCost + (float)System.Math.Round((diskSpace - baseDiskSpace) * DISK_SPACE_COST_MULTIPLIER,0);
            additionalMass = (diskSpace - baseDiskSpace) * DISK_SPACE_MASS_MULTIPLIER;

            part.mass = basePartMass + additionalMass;
        }

        //implement IPartMassModifier component
        public float GetModuleMass(float defaultMass)
        {
            return part.mass - defaultMass; //copied this fix from ProceduralParts mod as we already changed part.mass
            //return additionalMass;
        }

        public override void OnStart(StartState state)
        {
            //if in Editor, populate boot script selector, diskSpace selector and etc.
            if (state == StartState.Editor)
            {
                if (baseDiskSpace == 0) 
                    baseDiskSpace = diskSpace;

                if (System.Math.Abs (baseModuleCost) < 0.000001F)
                    baseModuleCost = additionalCost;  //remember module cost before tweaks
                else
                    additionalCost = baseModuleCost; //reset module cost and update later in UpdateCostAndMass()

                if (System.Math.Abs (basePartMass) < 0.000001F)
                    basePartMass = part.mass;  //remember part mass before tweaks
                else
                    part.mass = basePartMass; //reset part mass to original value and update later in UpdateCostAndMass()

                InitUI();
            }

            UpdateCostAndMass(); 

            //Do not start from editor and at KSP first loading
            if (state == StartState.Editor || state == StartState.None)
            {
                return;
            }

            SafeHouse.Logger.Log(string.Format("OnStart: {0} {1}", state, ProcessorMode));
            InitObjects();
        }
        private void InitUI()
        {
            //Populate selector for boot scripts
            BaseField field = Fields["bootFile"];
            var options = (UI_ChooseOption)field.uiControlEditor;

            var bootFiles = new List<string>();

            var temp = new Archive();
            var files = temp.GetFileList();
            var maxchoice = 0;
            foreach (FileInfo file in files)
            {
                if (!file.Name.StartsWith("boot", StringComparison.InvariantCultureIgnoreCase)) continue;
                bootFiles.Add(file.Name);
                maxchoice++;
            }
            //no need to show the control if there are no files starting with boot
            options.controlEnabled = maxchoice > 0;
            field.guiActiveEditor = maxchoice > 0;
            options.options = bootFiles.ToArray();

            //populate diskSpaceUI selector
            diskSpaceUI = diskSpace.ToString();
            field = Fields["diskSpaceUI"];
            options = (UI_ChooseOption)field.uiControlEditor;
            var sizeOptions = new string[3];
            sizeOptions[0] = baseDiskSpace.ToString();
            sizeOptions[1] = (baseDiskSpace*2).ToString();
            sizeOptions[2] = (baseDiskSpace*4).ToString();
            options.options = sizeOptions;
        
        }

        public void InitObjects()
        {
            SafeHouse.Logger.LogWarning("InitObjects: " + (shared == null));

            shared = new SharedObjects();
            CreateFactory();
                    
            shared.Vessel = vessel;
            shared.Processor = this;
            shared.KSPPart = part;
            shared.UpdateHandler = new UpdateHandler();
            shared.BindingMgr = new BindingManager(shared);
            shared.Interpreter = shared.Factory.CreateInterpreter(shared);
            shared.Screen = shared.Interpreter;
            shared.ScriptHandler = new KSScript();
            shared.Logger = new KSPLogger(shared);
            shared.VolumeMgr = shared.Factory.CreateVolumeManager(shared);
            shared.ProcessorMgr = new ProcessorManager();
            shared.FunctionManager = new FunctionManager(shared);
            shared.Cpu = new CPU(shared);

            // Make the window that is going to correspond to this kOS part:
            var gObj = new GameObject("kOSTermWindow", typeof(Screen.TermWindow));
            DontDestroyOnLoad(gObj);
            shared.Window = (Screen.TermWindow)gObj.GetComponent(typeof(Screen.TermWindow));
            shared.Window.AttachTo(shared);

            // initialize archive
            var archive = shared.Factory.CreateArchive();
            shared.VolumeMgr.Add(archive);

            // initialize harddisk
            if (HardDisk == null)
            {
                HardDisk = new Harddisk(Mathf.Min(diskSpace, PROCESSOR_HARD_CAP));
                // populate it with the boot file, but only if using a new disk and in PRELAUNCH situation:
                if (vessel.situation == Vessel.Situations.PRELAUNCH)
                {
                    var bootProgramFile = archive.GetByName(bootFile);
                    if (bootProgramFile != null)
                    {
                        // Copy to HardDisk as "boot".
                        var boot = new ProgramFile(bootProgramFile) { Filename = "boot.ks" };
                        HardDisk.Add(boot);
                    }
                }
            }
            shared.VolumeMgr.Add(HardDisk);

            // process setting
            if (!Config.Instance.StartOnArchive)
            {
                shared.VolumeMgr.SwitchTo(HardDisk);
            }
            
            InitProcessorTracking();
            // move Cpu.Boot() to within the first Update() to prevent boot script errors from killing OnStart
            // shared.Cpu.Boot();
        }

        private void InitProcessorTracking()
        {
            // Track a list of all instances of me that exist:
            if (! allMyInstances.Contains(this))
            {
                allMyInstances.Add(this);
                allMyInstances.Sort(delegate(kOSProcessor a, kOSProcessor b)
                {
                    // sort "nulls" first:
                    if (a.part == null || a.part.vessel == null)
                        return -1;
                    if (b.part == null || b.part.vessel == null)
                        return 1;
                    // If on different vessels, sort by vessel name next:
                    int compare = String.Compare(a.part.vessel.vesselName, b.part.vessel.vesselName,
                        StringComparison.CurrentCultureIgnoreCase);
                    // If on same vessel, sort by part UID last:
                    if (compare != 0)
                        return compare;
                    return (a.part.uid() < b.part.uid()) ? -1 : (a.part.uid() > b.part.uid()) ? 1 : 0;
                });
            }
            GameEvents.onPartDestroyed.Add(OnDestroyingMyHardware);
        }

        private void OnDestroyingMyHardware(Part p)
        {
            // This is technically called any time ANY part is destroyed, so ignore it if it's not MY part:
            if (p != part)
                return;
            
            GetWindow().DetachAllTelnets();
            
            allMyInstances.RemoveAll(m => m==this);
        }
        
        
        /// <summary>
        /// Return a list of all existing runtime instances of this PartModule.
        /// The list is guaranteed to be ordered by the Vessel that it's on.
        /// (i.e. all the instances of no vessel are first ,then all the module instances
        /// on vessel A, then all the instances on vessel B, and so on)
        /// </summary>
        /// <returns></returns>
        public static List<kOSProcessor> AllInstances()
        {
            // Doing it this way to force return value to be a shallow-level copy,
            // rather than an exact reference to the internal private list.
            // So if the caller adds/removes from it, it won't mess with the
            // private list we're internally maintaining:
            return allMyInstances.GetRange(0, allMyInstances.Count);
        }

        private void CreateFactory()
        {
            SafeHouse.Logger.LogWarning("Starting Factory Building");
            bool isAvailable;
            try
            {
                isAvailable = RemoteTechHook.IsAvailable(vessel.id);
            }
            catch
            {
                isAvailable = false;
            }

            if (isAvailable)
            {
                SafeHouse.Logger.LogWarning("RemoteTech Factory Building");
                shared.Factory = new RemoteTechFactory();
            }
            else
            {
                SafeHouse.Logger.LogWarning("Standard Factory Building");
                shared.Factory = new StandardFactory();
            }
        }

        public void RegisterkOSExternalFunction(object[] parameters)
        {
            //SafeHouse.Logger.Log("*** External Function Registration Succeeded");
            //cpu.RegisterkOSExternalFunction(parameters);
        }
        
        public static int AssignNewId()
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
            if (HighLogic.LoadedScene == GameScenes.EDITOR)
            {
                if (diskSpace != Convert.ToInt32(diskSpaceUI))
                {
                    diskSpace = Convert.ToInt32(diskSpaceUI);
                    UpdateCostAndMass();
                    GameEvents.onEditorShipModified.Fire(EditorLogic.fetch.ship);
                }
                
            }
            if (!IsAlive()) return;
            if (firstUpdate)
            {
                SafeHouse.Logger.LogWarning("First Update()");
                firstUpdate = false;
                shared.Cpu.Boot();
            }
            UpdateVessel();
            UpdateObservers();
            ProcessElectricity(part, TimeWarp.fixedDeltaTime);
        }

        public void FixedUpdate()
        {
            if (!IsAlive()) return;

            UpdateFixedObservers();
        }

        private void UpdateVessel()
        {
            if (shared != null && shared.Vessel != vessel)
            {
                shared.Vessel = vessel;
            }
        }

        private void UpdateObservers()
        {
            if (ProcessorMode == ProcessorModes.READY)
            {
                if (shared.UpdateHandler != null) shared.UpdateHandler.UpdateObservers(Time.deltaTime);
                UpdateParts();
            }
        }

        private void UpdateFixedObservers()
        {
            if (ProcessorMode == ProcessorModes.READY)
            {
                if (shared.UpdateHandler != null) shared.UpdateHandler.UpdateFixedObservers(Time.deltaTime);
            }
        }

        private bool IsAlive()
        {
            if (shared == null) return false;

            if (part.State == PartStates.DEAD)
            {
                ProcessorMode = ProcessorModes.OFF;
                return false;
            }
            // Do a check for vessel's current total mass and make sure we can control it
            if (vessel != null && maximumControllableMass < float.MaxValue)
            {
                int numParts = vessel.parts.Count;
                float vesselMass = 0f;
                for (int i = 0; i < vessel.Parts.Count; i++)
                {
                    // add up mass
                    if ((object)(vessel.Parts[i].rb) != null)
                        vesselMass += vessel.Parts[i].rb.mass;
                    else
                        vesselMass += vessel.Parts[i].mass + vessel.Parts[i].GetResourceMass();
                }
                // if current mass is higher than our controllable mass, we turn off the CPU
                if (vesselMass > maximumControllableMass)
                {
                    if (ProcessorMode == ProcessorModes.READY || ProcessorMode == ProcessorModes.STARVED)
                        massEnforcedPowerDown = true;
                    SetMode(ProcessorModes.OFF);
                    return false;
                }
                // If current mass is lower than our controllable mass, we turn the CPU back on
                // This ensure that the CPU can be re-enabled when the craft gets lighter!
                else
                {
                    if (massEnforcedPowerDown && ProcessorMode == ProcessorModes.OFF)
                        SetMode(ProcessorModes.STARVED);
                }
            }
            return true;
        }

        public void UpdateParts()
        {
            // Trigger whenever the number of parts in the vessel changes (like when staging, docking or undocking).
            if (vessel.parts.Count == vesselPartCount) return;

            var missingHardDisks = false;
            var attachedVolumes = new List<Volume>();
            var processors = new List<kOSProcessor>();

            // Look for all the processors that exists in the vessel
            foreach (var partObj in vessel.parts)
            {
                kOSProcessor processorPart;
                if (!PartIsKosProc(partObj, out processorPart)) continue;

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

            if (missingHardDisks) return;

            shared.VolumeMgr.UpdateVolumes(attachedVolumes);
            shared.ProcessorMgr.UpdateProcessors(processors);
            vesselPartCount = vessel.parts.Count;
        }

        public bool PartIsKosProc(Part input, out kOSProcessor proc)
        {
            foreach (var processor in input.Modules.OfType<kOSProcessor>())
            {
                proc = processor;
                return true;
            }

            proc = null;
            return false;
        }

        public override void OnFixedUpdate()
        {
        }

        public override void OnInactive()
        {
            SafeHouse.Logger.Log("Processor Stop");
        }

        public override void OnLoad(ConfigNode node)
        {
            try
            {
                // KSP Seems to want to make an instance of my partModule during initial load
                if (vessel == null) return;

                if (node.HasNode("harddisk"))
                {
                    var newDisk = node.GetNode("harddisk").ToHardDisk();
                    HardDisk = newDisk;
                }

                InitObjects();

                if (shared != null && shared.Cpu != null)
                {
                    ((CPU)shared.Cpu).OnLoad(node);
                }
                base.OnLoad(node);
            }
            catch (Exception ex) //Intentional Pokemon, if exceptions get out of here it can kill the craft
            {
                SafeHouse.Logger.Log("ONLOAD Exception: " + ex.TargetSite);
                SafeHouse.Logger.LogException(ex);
            }
        }

        public override void OnSave(ConfigNode node)
        {
            try
            {
                if (HardDisk != null)
                {
                    ConfigNode hdNode = HardDisk.ToConfigNode("harddisk");
                    node.AddNode(hdNode);
                }

                if (shared != null && shared.Cpu != null)
                {
                    ((CPU)shared.Cpu).OnSave(node);
                    Config.Instance.SaveConfig();
                }

                base.OnSave(node);
            }
            catch (Exception ex) //Intentional Pokemon, if exceptions get out of here it can kill the craft
            {
                SafeHouse.Logger.Log("ONSAVE Exception: " + ex.TargetSite);
                SafeHouse.Logger.LogException(ex);
            }
        }
        
        // This is what KSP calls during the initial loading screen (the screen
        // with the progress bar and all of the "Turning correct end toward space"
        // funny messages.)  This is where kOS *should* be putting all the
        // static initializing that does not change per-part, or per-vessel
        // or per-CPU.  It might be true that some of what we're doing up above
        // in OnLoad and OnStart might relly belong here.
        //
        // At some future point it would be a really good idea to look very carefully
        // at *everything* being done during those initialzations and see if any of
        // those steps really are meant to be static do-once steps that belong here
        // instead:
        public override void OnAwake()
        {
            Opcode.InitMachineCodeData();
            CompiledObject.InitTypeData();
        }

        private void ProcessElectricity(Part partObj, float time)
        {
            if (ProcessorMode == ProcessorModes.OFF) return;

            RequiredPower = shared.VolumeMgr.CurrentRequiredPower;
            var electricReq = time * RequiredPower;
            var result = partObj.RequestResource("ElectricCharge", electricReq) / electricReq;

            var newMode = (result < 0.5f) ? ProcessorModes.STARVED : ProcessorModes.READY;
            SetMode(newMode);
        }

        public void SetMode(ProcessorModes newProcessorMode)
        {
            if (newProcessorMode != ProcessorMode)
            {
                switch (newProcessorMode)
                {
                    case ProcessorModes.READY:
                        if (Config.Instance.StartOnArchive)
                        {
                            shared.VolumeMgr.SwitchTo(shared.VolumeMgr.GetVolume(0));
                        }
                        else
                        {
                            shared.VolumeMgr.SwitchTo(HardDisk);
                        }
                        if (shared.Cpu != null) shared.Cpu.Boot();
                        if (shared.Interpreter != null) shared.Interpreter.SetInputLock(false);
                        if (shared.Window != null) shared.Window.IsPowered = true;
                        break;

                    case ProcessorModes.OFF:
                    case ProcessorModes.STARVED:
                        if (shared.Interpreter != null) shared.Interpreter.SetInputLock(true);
                        if (shared.Window != null) shared.Window.IsPowered = false;
                        if (shared.BindingMgr != null) shared.BindingMgr.UnBindAll(); 
                        break;
                }

                ProcessorMode = newProcessorMode;
            }
        }

        public void ExecuteInterProcCommand(InterProcCommand command)
        {
            if (command != null)
            {
                command.Execute(shared);
            }
        }

        public void SetAutopilotMode(int mode)
        {
            RUIToggleButton[] modeButtons = FindObjectOfType<VesselAutopilotUI>().modeButtons;
            modeButtons.ElementAt(mode).SetTrue();
        }
    }
}
