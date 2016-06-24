using kOS.AddOns.RemoteTech;
using kOS.Binding;
using kOS.Execution;
using kOS.Factories;
using kOS.Function;
using kOS.Communication;
using kOS.Persistence;
using kOS.Safe;
using kOS.Safe.Compilation;
using kOS.Safe.Compilation.KS;
using kOS.Safe.Module;
using kOS.Safe.Persistence;
using kOS.Safe.Screen;
using kOS.Safe.Utilities;
using kOS.Utilities;
using KSP.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Execution;
using UnityEngine;
using kOS.Safe.Encapsulation;
using KSP.UI;
using kOS.Suffixed;
using kOS.Safe.Communication;
using kOS.Safe.Function;

namespace kOS.Module
{
    public class kOSProcessor : PartModule, IProcessor, IPartCostModifier, IPartMassModifier
    {
        public ProcessorModes ProcessorMode { get; private set; }

        public Harddisk HardDisk { get; private set; }

        public Archive Archive { get; private set; }

        public MessageQueue Messages { get; private set; }

        public string Tag
        {
            get
            {
                KOSNameTag tag = part.Modules.OfType<KOSNameTag>().FirstOrDefault();
                return tag == null ? string.Empty : tag.nameTag;
            }
        }

        private int vesselPartCount;
        private SharedObjects shared;
        private static readonly List<kOSProcessor> allMyInstances = new List<kOSProcessor>();
        private bool firstUpdate = true;

        private MovingAverage averagePower = new MovingAverage();

        // This is the "constant" byte count used when calculating the EC
        // required by the archive volume (which has infinite space).
        // TODO: This corresponds to the existing value and should be adjusted for balance.
        private const int ARCHIVE_EFFECTIVE_BYTES = 50000;

        //640K ought to be enough for anybody -sic
        private const int PROCESSOR_HARD_CAP = 655360;

        private const string BootDirectoryName = "boot";
        private GlobalPath bootDirectoryPath = GlobalPath.FromVolumePath(VolumePath.FromString(BootDirectoryName),
            Archive.ArchiveName);

        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Boot File"), UI_ChooseOption(scene = UI_Scene.Editor)]
        public string bootFile = "boot.ks";

        [KSPField(isPersistant = true, guiName = "kOS Disk Space", guiActive = true)]
        public int diskSpace = 1024;

        [KSPField(isPersistant = true, guiName = "kOS Base Disk Space", guiActive = false)]
        public int baseDiskSpace = 0;

        [KSPField(isPersistant = false, guiName = "kOS Base Module Cost", guiActive = false)]
        public float baseModuleCost = 0F;  // this is the base cost added to a part for including the kOSProcessor, default to 0.

        [KSPField(isPersistant = true, guiName = "kOS Base Module Mass", guiActive = false)]
        public float baseModuleMass = 0F;  // this is the base mass added to a part for including the kOSProcessor, default to 0.

        [KSPField(isPersistant = false, guiName = "kOS Disk Space", guiActive = false, guiActiveEditor = true), UI_ChooseOption(scene = UI_Scene.Editor)]
        public string diskSpaceUI = "1024";

        [KSPField(isPersistant = true, guiName = "CPU/Disk Upgrade Cost", guiActive = false, guiActiveEditor = true)]
        public float additionalCost = 0F;

        [KSPField(isPersistant = false, guiName = "CPU/Disk Upgrade Mass", guiActive = false, guiActiveEditor = true)]
        public float additionalMass = 0F;

        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false)]
        public float diskSpaceCostFactor = 0.0244140625F; //implies approx 100funds for 4096bytes of diskSpace

        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false)]
        public float diskSpaceMassFactor = 0.0000048829F;  //implies approx 0.020kg for 4096bytes of diskSpace

        [KSPField(isPersistant = true, guiActive = false)]
        public int MaxPartId = 100;

        // This represents how much EC to consume per executed instruction.
        // This would be the "variable" component of the processor's power.
        // Important: This value should be overriden in the part.cfg file
        // for the kOS processor.  The only reason it's being given a value
        // here is as a fallback for those cases where an old legacy part
        // might be loaded from before the part files had this value.
        [KSPField(isPersistant = false, guiActive = false)]
        public float ECPerInstruction = 0.000004F;

        // This represents how much EC to consume per Byte of the current volume, per second.
        // This would be the "continuous" compoenent of the processor's power (though it varies
        // when you change to another volume).
        // IMPORTANT: The value defaults to zero and must be overriden in the module
        // definition for any given part (within the part.cfg file).
        [KSPField(isPersistant = false, guiActive = false)]
        public float ECPerBytePerSecond = 0F;

        public kOSProcessor()
        {
            ProcessorMode = ProcessorModes.READY;
        }

        public GlobalPath BootFilePath {
            get {
                return bootDirectoryPath.Combine(bootFile);
            }
        }

        [KSPEvent(guiActive = true, guiName = "Open Terminal", category = "skip_delay;")]
        public void Activate()
        {
            SafeHouse.Logger.Log("Activate");
            OpenWindow();
        }

        [KSPField(isPersistant = true, guiName = "kOS Average Power", guiActive = true, guiActiveEditor = true, guiUnits = "EC/s", guiFormat = "0.000")]
        public float RequiredPower = 0;

        [KSPEvent(guiActive = true, guiName = "Toggle Power")]
        public void TogglePower()
        {
            SafeHouse.Logger.Log("Toggle Power");
            ProcessorModes newProcessorMode = (ProcessorMode != ProcessorModes.OFF) ? ProcessorModes.OFF : ProcessorModes.STARVED;
            SetMode(newProcessorMode);
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
            int defaultAvgInstructions = 200;
            string format =
                "Default disk capacity: {0}\n\n" +
                "<color=#99ff00ff>Requires:</color>\n" +
                " - ElectricCharge: {1}\n" +
                "<color=#99ff00ff>Example:</color>\n" +
                " - {2:N3}EC/s if IPU={3} and no wait instructions.";
            // For the sake of GetInfo, prorate the EC usage based on the smallest physics frame currently selected
            // Because this is called before the part is set, we need to manually calculate it instead of letting Update handle it.
            double power = diskSpace * ECPerBytePerSecond + defaultAvgInstructions * ECPerInstruction / Time.fixedDeltaTime;
            string chargeText = (ECPerInstruction == 0) ?
                "None.  It's powered by pure magic ... apparently." : // for cheaters who use MM or editing part.cfg, to get rid of it.
                string.Format("1 per {0} instructions executed", (int)(1 / ECPerInstruction));
            return string.Format(format, diskSpace, chargeText, power, defaultAvgInstructions);
        }

        //implement IPartCostModifier component
        public float GetModuleCost(float defaultCost, ModifierStagingSituation sit)
        {
            // the 'sit' arg is irrelevant to us, but the interface requires it.

            return baseModuleCost + additionalCost;
        }
        //implement IPartMassModifier component
        public ModifierChangeWhen GetModuleCostChangeWhen()
        {
            return ModifierChangeWhen.FIXED;
        }

        private void UpdateCostAndMass()
        {
            float spaceDelta = diskSpace - baseDiskSpace;
            additionalCost = (float)System.Math.Round(spaceDelta * diskSpaceCostFactor, 0);
            additionalMass = spaceDelta * diskSpaceMassFactor;
        }

        //implement IPartMassModifier component
        public float GetModuleMass(float defaultMass, ModifierStagingSituation sit)
        {
            // the 'sit' arg is irrelevant to us, but the interface requires it.
            
            return baseModuleMass + additionalMass;
        }
        //implement IPartMassModifier component
        public ModifierChangeWhen GetModuleMassChangeWhen()
        {
            return ModifierChangeWhen.FIXED;
        }

        public override void OnStart(StartState state)
        {
            try
            {
                //if in Editor, populate boot script selector, diskSpace selector and etc.
                if (state == StartState.Editor)
                {
                    if (baseDiskSpace == 0)
                        baseDiskSpace = diskSpace;

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
            } catch (Exception e)
            {
                SafeHouse.Logger.LogException(e);
            }
        }

        private void InitUI()
        {
            //Populate selector for boot scripts
            BaseField field = Fields["bootFile"];
            var options = (UI_ChooseOption)field.uiControlEditor;

            var bootFiles = new List<string>();

            bootFiles.Add("None");
            bootFiles.AddRange(BootDirectoryFiles());

            //no need to show the control if there are no available boot files
            options.controlEnabled = bootFiles.Count > 1;
            field.guiActiveEditor = bootFiles.Count > 1;
            options.options = bootFiles.ToArray();

            //populate diskSpaceUI selector
            diskSpaceUI = diskSpace.ToString();
            field = Fields["diskSpaceUI"];
            options = (UI_ChooseOption)field.uiControlEditor;
            var sizeOptions = new string[3];
            sizeOptions[0] = baseDiskSpace.ToString();
            sizeOptions[1] = (baseDiskSpace * 2).ToString();
            sizeOptions[2] = (baseDiskSpace * 4).ToString();
            options.options = sizeOptions;
        }

        private IEnumerable<string> BootDirectoryFiles()
        {
            var result = new List<string>();

            var archive = new Archive(SafeHouse.ArchiveFolder);

            var bootDirectory = archive.Open(bootDirectoryPath) as VolumeDirectory;

            if (bootDirectory == null)
            {
                return result;
            }

            var files = bootDirectory.List();

            foreach (KeyValuePair<string, VolumeItem> pair in files)
            {
                if (pair.Value is VolumeFile && (pair.Value.Extension.Equals(Volume.KERBOSCRIPT_EXTENSION)
                    || pair.Value.Extension.Equals(Volume.KOS_MACHINELANGUAGE_EXTENSION)))
                {
                    result.Add(pair.Key);
                }
            }

            return result;
        }

        public void InitObjects()
        {
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
            shared.ConnectivityMgr = shared.Factory.CreateConnectivityManager();
            shared.ProcessorMgr = new ProcessorManager();
            shared.FunctionManager = new FunctionManager(shared);
            shared.TransferManager = new TransferManager(shared);
            shared.Cpu = new CPU(shared);
            shared.SoundMaker = Sound.SoundMaker.Instance;
            shared.AddonManager = new AddOns.AddonManager(shared);

            // Make the window that is going to correspond to this kOS part:
            var gObj = new GameObject("kOSTermWindow", typeof(Screen.TermWindow));
            DontDestroyOnLoad(gObj);
            shared.Window = (Screen.TermWindow)gObj.GetComponent(typeof(Screen.TermWindow));
            shared.Window.AttachTo(shared);

            // initialize archive
            Archive = shared.Factory.CreateArchive();
            shared.VolumeMgr.Add(Archive);

            Messages = new MessageQueue();

            // initialize harddisk
            if (HardDisk == null)
            {
                HardDisk = new Harddisk(Mathf.Min(diskSpace, PROCESSOR_HARD_CAP));

                if (!string.IsNullOrEmpty(Tag))
                {
                    HardDisk.Name = Tag;
                }

                // populate it with the boot file, but only if using a new disk and in PRELAUNCH situation:
                if (vessel.situation == Vessel.Situations.PRELAUNCH && bootFile != "None" && !SafeHouse.Config.StartOnArchive)
                {
                    var bootVolumeFile = Archive.Open(BootFilePath) as VolumeFile;
                    if (bootVolumeFile != null)
                    {
                        GlobalPath harddiskPath = GlobalPath.FromVolumePath(
                            VolumePath.FromString(BootFilePath.Name),
                            shared.VolumeMgr.GetVolumeRawIdentifier(HardDisk));

                        if (HardDisk.SaveFile(harddiskPath, bootVolumeFile.ReadAll()) == null)
                        {
                            // Throwing an exception during InitObjects will break the initialization and won't show
                            // the error to the user.  So we just log the error instead.  At some point in the future
                            // it would be nice to queue up these init errors and display them to the user somewhere.
                            SafeHouse.Logger.LogError("Error copying boot file to local volume: not enough space.");
                        }
                    }
                }
            }

            shared.VolumeMgr.Add(HardDisk);

            // process setting
            if (!SafeHouse.Config.StartOnArchive)
            {
                shared.VolumeMgr.SwitchTo(HardDisk);
            }

            // initialize processor mode if different than READY
            if (ProcessorMode != ProcessorModes.READY)
            {
                ProcessorModeChanged();
            }

            InitProcessorTracking();
        }

        private void InitProcessorTracking()
        {
            // Track a list of all instances of me that exist:
            if (!allMyInstances.Contains(this))
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
                    int compare = string.Compare(a.part.vessel.vesselName, b.part.vessel.vesselName,
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

            allMyInstances.RemoveAll(m => m == this);
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
                isAvailable = RemoteTechHook.IsAvailable();
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
                RequiredPower = this.diskSpace * ECPerBytePerSecond + SafeHouse.Config.InstructionsPerUpdate * ECPerInstruction / Time.fixedDeltaTime;
            }
            if (!IsAlive()) return;
            UpdateVessel();
            UpdateObservers();
        }

        public void FixedUpdate()
        {
            if (!IsAlive()) return;

            if (!vessel.HoldPhysics)
            {
                if (firstUpdate)
                {
                    SafeHouse.Logger.LogWarning("First Update()");
                    firstUpdate = false;
                    shared.Cpu.Boot();
                }
                UpdateVessel();
                UpdateFixedObservers();
                ProcessElectricity(part, TimeWarp.fixedDeltaTime);
            }
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
                if (shared.UpdateHandler != null) shared.UpdateHandler.UpdateObservers(TimeWarp.deltaTime);
                UpdateParts();
            }
        }

        private void UpdateFixedObservers()
        {
            if (ProcessorMode == ProcessorModes.READY)
            {
                if (shared.UpdateHandler != null) shared.UpdateHandler.UpdateFixedObservers(TimeWarp.fixedDeltaTime);
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

                if (node.HasValue("activated") && !bool.Parse(node.GetValue("activated")))
                {
                    ProcessorMode = ProcessorModes.OFF;
                }

                if (node.HasNode("harddisk"))
                {
                    var newDisk = node.GetNode("harddisk").ToHardDisk();
                    HardDisk = newDisk;
                }

                InitObjects();

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
                node.AddValue("activated", ProcessorMode != ProcessorModes.OFF);

                if (HardDisk != null)
                {
                    ConfigNode hdNode = HardDisk.ToConfigNode("harddisk");
                    node.AddNode(hdNode);
                }

                if (shared != null && shared.Cpu != null)
                {
                    SafeHouse.Config.SaveConfig();
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
        // in OnLoad and OnStart might really belong here.
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

            double volumePower = 0;
            if (shared.VolumeMgr.CheckCurrentVolumeRange(shared.Vessel))
            {
                // If the current volume is in range, check the capacity and calculate power
                var volume = shared.VolumeMgr.CurrentVolume;
                if (volume.Name == "Archive")
                {
                    volumePower = ARCHIVE_EFFECTIVE_BYTES * ECPerBytePerSecond;
                }
                else
                {
                    volumePower = volume.Capacity * ECPerBytePerSecond;
                }
            }
            else
            {
                // if the volume isn't in range, assume it doesn't consume any power
                volumePower = 0;
            }

            if (ProcessorMode == ProcessorModes.STARVED)
            {
                // If the processor is STARVED, check to see if there is enough EC to turn it back on.
                var request = averagePower.Mean;  // use the average power draw as a baseline of the power needed to restart.
                if (request > 0)
                {
                    var available = partObj.RequestResource("ElectricCharge", request);
                    if (available / request > 0.5)
                    {
                        SetMode(ProcessorModes.READY);
                    }
                    // Since we're just checking to see if there is enough power to restart, return
                    // the consumed EC.  The actual demand value will be drawn on the next update after
                    // the cpu boots.  This should give the ship a chance to collect a little more EC
                    // before the cpu actually boots.
                    partObj.RequestResource("ElectricCharge", -available);
                }
                else
                {
                    // If there is no historical power request, simply turn the processor back on.  This
                    // should not be possible, since it means that some how the processor got set to
                    // the STARVED mode, even though no power was requested.
                    SetMode(ProcessorModes.READY);
                }
                RequiredPower = (float)request; // Make sure RequiredPower matches the average.
            }
            else
            {
                // Because the processor is not STARVED, evaluate the power requirement based on actual operation.
                // For EC drain purposes, always pretend atleast 1 instruction happened, so idle drain isn't quite zero:
                int instructions = System.Math.Max(shared.Cpu.InstructionsThisUpdate, 1);
                var request = volumePower * time + instructions * ECPerInstruction;
                if (request > 0)
                {
                    // only check the available EC if the request is greater than 0EC.  If the request value
                    // is zero, then available will always be zero and it appears that mono/.net treat
                    // "0 / 0" as equaling "0", which prevents us from checking the ratio.  Since getting
                    // "0" available of "0" requested is a valid state, the processor mode is only evaluated
                    // if request is greater than zero.
                    var available = partObj.RequestResource("ElectricCharge", request);
                    if (available / request < 0.5)
                    {
                        // 0.5 is an arbitrary ratio for triggering the STARVED mode.  It allows for some
                        // fluctuation away from the exact requested EC, ando adds some fuzzy math to how
                        // we deal with the descreet physics frames.  Essentially if there was enough power
                        // to run for half of a physics frame, the processor stays on.
                        SetMode(ProcessorModes.STARVED);
                    }
                }
                // Set RequiredPower to the average requested power.  This should help "de-bounce" the value
                // so that it doesn't fluctuate wildly (between 0.2 and 0.000001 in a single frame for example)
                RequiredPower = (float)averagePower.Update(request) / TimeWarp.fixedDeltaTime;
            }
        }

        public void SetMode(ProcessorModes newProcessorMode)
        {
            if (newProcessorMode != ProcessorMode)
            {
                ProcessorMode = newProcessorMode;

                ProcessorModeChanged();
            }
        }

        private void ProcessorModeChanged()
        {
            switch (ProcessorMode)
            {
            case ProcessorModes.READY:
                shared.VolumeMgr.SwitchTo(SafeHouse.Config.StartOnArchive ? (Volume)Archive : HardDisk);
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
            UIStateToggleButton[] modeButtons = FindObjectOfType<VesselAutopilotUI>().modeButtons;
            modeButtons.ElementAt(mode).SetState(true);
        }

        public string BootFilename
        {
            get { return bootFile; }
            set { bootFile = value; }
        }

        public bool CheckCanBoot()
        {
            if (shared.VolumeMgr == null) { shared.Logger.Log("No volume mgr"); }
            else if (!shared.VolumeMgr.CheckCurrentVolumeRange(shared.Vessel)) { shared.Logger.Log(new Safe.Exceptions.KOSVolumeOutOfRangeException("Boot")); }
            else if (shared.VolumeMgr.CurrentVolume == null) { shared.Logger.Log("No current volume"); }
            else if (shared.ScriptHandler == null) { shared.Logger.Log("No script handler"); }
            else
            {
                return true;
            }
            return false;
        }

        public void Send(Structure content)
        {
            double sentAt = Planetarium.GetUniversalTime();
            Messages.Push(Message.Create(content, sentAt, sentAt, new VesselTarget(shared), Tag));
        }
    }
}