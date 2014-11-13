using System;
using System.Collections.Generic;
using System.Linq;
using kOS.Execution;
using kOS.Factories;
using kOS.Function;
using kOS.Safe.Persistence;
using UnityEngine;
using KSP.IO;
using kOS.InterProcessor;
using kOS.Binding;
using kOS.Persistence;
using kOS.Safe;
using kOS.Safe.Compilation;
using kOS.Safe.Compilation.KS;
using kOS.Safe.Module;
using kOS.Suffixed;
using kOS.AddOns.RemoteTech2;

namespace kOS.Module
{
    public class kOSProcessor : PartModule, IProcessor
    {
        public ProcessorModes ProcessorMode = ProcessorModes.READY;

        public Harddisk HardDisk { get; private set; }
        private int vesselPartCount;
        private SharedObjects shared;
        private static readonly List<kOSProcessor> allMyInstances = new List<kOSProcessor>();

        //640K ought to be enough for anybody -sic
        private const int PROCESSOR_HARD_CAP = 655360;

        [KSPField(isPersistant = true, guiName = "Boot File", guiActive = false, guiActiveEditor = false)]
        public string bootFile = "boot";

        [KSPField(isPersistant = false, guiName = "Boot File Choice", guiActive = false, guiActiveEditor = true), UI_FloatRange(minValue=0f,maxValue=1f,stepIncrement=1f)]
        public float bootFileChoice = 0f;
        private float bootFileChoiceLast = -1f;

        [KSPField(isPersistant = true, guiName = "kOS Disk Space", guiActive = true)]
        public int diskSpace = 500;

        [KSPField(isPersistant = true, guiActive = false)] public int MaxPartId = 100;

        [KSPEvent(guiActive = true, guiName = "Open Terminal", category = "skip_delay;")]
        public void Activate()
        {
            Debug.Log("kOS: Activate");
            OpenWindow();
        }

        [KSPField(isPersistant = true, guiName = "Required Power", guiActive = true)] 
        public float RequiredPower;

        [KSPEvent(guiActive = true, guiName = "Toggle Power")]
        public void TogglePower()
        {
            Debug.Log("kOS: Toggle Power");
            ProcessorModes newProcessorMode = (ProcessorMode != ProcessorModes.OFF) ? ProcessorModes.OFF : ProcessorModes.STARVED;
            SetMode(newProcessorMode);
        }
        
        [KSPAction("Open Terminal", actionGroup = KSPActionGroup.None)]
        public void Activate(KSPActionParam param)
        {
            Debug.Log("kOS: Open Terminal from Dialog");
            Activate();
        }

        [KSPAction("Close Terminal", actionGroup = KSPActionGroup.None)]
        public void Deactivate(KSPActionParam param)
        {
            Debug.Log("kOS: Close Terminal from ActionGroup");
            CloseWindow();
        }

        [KSPAction("Toggle Terminal", actionGroup = KSPActionGroup.None)]
        public void Toggle(KSPActionParam param)
        {
            Debug.Log("kOS: Toggle Terminal from ActionGroup");
            ToggleWindow();
        }

        [KSPAction("Toggle Power", actionGroup = KSPActionGroup.None)]
        public void TogglePower(KSPActionParam param)
        {
            Debug.Log("kOS: Toggle Power from ActionGroup");
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
            return shared.Window.IsOpen();
        }

        public override void OnStart(StartState state)
        {
            //Do not start from editor and at KSP first loading
            if (state == StartState.Editor || state == StartState.None)
            {
                return;
            }

            Debug.Log(string.Format("kOS: OnStart: {0} {1}", state, ProcessorMode));
            InitObjects();
        }

        public void InitObjects()
        {
            Debug.LogWarning("kOS: InitObjects: " + (shared == null));

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
            if (HardDisk == null && archive.CheckRange(vessel))
            {
                HardDisk = new Harddisk(Mathf.Min(diskSpace, PROCESSOR_HARD_CAP));
                var bootProgramFile = archive.GetByName(bootFile);
                if (bootProgramFile != null)
                {
                    // Copy to HardDisk as "boot".
                    var boot = new ProgramFile(bootProgramFile) {Filename = "boot"};
                    HardDisk.Add(boot);
                }
            }
            shared.VolumeMgr.Add(HardDisk);

            // process setting
            if (!Config.Instance.StartOnArchive)
            {
                shared.VolumeMgr.SwitchTo(HardDisk);
            }
            
            InitProcessorTracking();
            shared.Cpu.Boot();
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
                    return (a.part.uid < b.part.uid) ? -1 : (a.part.uid > b.part.uid) ? 1 : 0;
                });
            }
            GameEvents.onPartDestroyed.Add(OnDestroyingMyHardware);
        }

        private void OnDestroyingMyHardware(Part p)
        {
            // This is technically called any time ANY part is destroyed, so ignore it if it's not MY part:
            if (p != part)
                return;
            
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
            Debug.LogWarning("kOS: Starting Factory Building");
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
                Debug.LogWarning("kOS: RemoteTech Factory Building");
                shared.Factory = new RemoteTechFactory();
            }
            else
            {
                Debug.LogWarning("kOS: Standard Factory Building");
                shared.Factory = new StandardFactory();
            }
        }

        public void RegisterkOSExternalFunction(object[] parameters)
        {
            //Debug.Log("*** External Function Registration Succeeded");
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
            ProcessBoot();
            if (!IsAlive()) return;
            UpdateVessel();
            UpdateObservers();
            ProcessElectricity(part, TimeWarp.fixedDeltaTime);
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

        private bool IsAlive()
        {
            if (shared == null) return false;

            if (part.State == PartStates.DEAD)
            {
                ProcessorMode = ProcessorModes.OFF;
                return false;
            }
            return true;
        }

        private void ProcessBoot()
        {
            if (bootFileChoice == bootFileChoiceLast) return;

            var temp = new Archive();
            var files = temp.GetFileList();
            var maxchoice = 0;
            for (var i = 0; i < files.Count; ++i)
            {
                if (!files[i].Name.StartsWith("boot", StringComparison.InvariantCultureIgnoreCase)) continue;
                maxchoice++;
                if (bootFileChoiceLast < 0)
                {
                    // find previous
                    if (files[i].Name == bootFile)
                    {
                        bootFileChoice = i;
                        bootFileChoiceLast = i;
                    }
                }
                if (i == bootFileChoice)
                {
                    bootFile = files[i].Name;
                }
            }
            var field = Fields["bootFileChoice"];
            if (field != null)
            {
                field.guiName = bootFile;
                var ui = field.uiControlEditor as UI_FloatRange;
                if (ui != null)
                {
                    ui.maxValue = maxchoice;
                    ui.controlEnabled = maxchoice > 0;
                    field.guiActiveEditor = maxchoice > 0;
                    bootFileChoiceLast = bootFileChoice;
                }
            }
        }

        public void UpdateParts()
        {
            // Trigger whenever the number of parts in the vessel changes (like when staging, docking or undocking)
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
            Debug.Log("kOS: Processor Stop");
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
            catch (Exception ex) //Chris: Intentional Pokemon, if exceptions get out of here it can kill the craft
            {
                Debug.Log("kOS: ONLOAD Exception: " + ex.TargetSite);
                Debug.LogException(ex);
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
            catch (Exception ex) //Chris: Intentional Pokemon, if exceptions get out of here it can kill the craft
            {
                Debug.Log("kOS: ONSAVE Exception: " + ex.TargetSite);
                Debug.LogException(ex);
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
                        if (ProcessorMode == ProcessorModes.STARVED && shared.Cpu != null) shared.Cpu.Boot();
                        if (shared.Interpreter != null) shared.Interpreter.SetInputLock(false);
                        if (shared.Window != null) shared.Window.SetPowered(true);
                        break;

                    case ProcessorModes.OFF:
                    case ProcessorModes.STARVED:
                        if (shared.Interpreter != null) shared.Interpreter.SetInputLock(true);
                        if (shared.Window != null) shared.Window.SetPowered(false);
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
    }
}
