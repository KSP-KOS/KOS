using kOS.Binding;
using kOS.Callback;
using kOS.Execution;
using kOS.Communication;
using kOS.Persistence;
using kOS.Safe;
using kOS.Safe.Serialization;
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
using System.Reflection;
using System.Text.RegularExpressions;
using kOS.Safe.Execution;
using UnityEngine;
using kOS.Safe.Encapsulation;
using KSP.UI;
using kOS.Suffixed;
using kOS.Safe.Function;
using kOS.Lua;
using kOS.Screen;

namespace kOS.Module
{
    public class kOSProcessor : PartModule, IProcessor, IPartCostModifier, IPartMassModifier
    {
        private const string PAWGroup = "kOS";

        public ProcessorModes ProcessorMode { get; private set; }

        public Harddisk HardDisk { get; private set; }

        public Archive Archive { get; private set; }

        public MessageQueue Messages { get; private set; }

        private static bool bootListDirty;

        public string Tag
        {
            get
            {
                KOSNameTag tag = part.Modules.OfType<KOSNameTag>().FirstOrDefault();
                return tag == null ? string.Empty : tag.nameTag;
            }
            set
            {
                KOSNameTag tag = part.Modules.OfType<KOSNameTag>().FirstOrDefault();
                // Really a null tag shouldn't ever happen.  It would mean kOS is installed but KOSNameTag's aren't on all the things.
                // And that should only happen if someone has a bad ModuleManager config that's screwing with kOS.
                if (tag != null)
                    tag.nameTag = value;
            }
        }

        private int vesselPartCount;
        private SharedObjects shared;
        private static readonly List<kOSProcessor> allMyInstances = new List<kOSProcessor>();
        public bool HasBooted { get; set; }
        private bool objectsInitialized = false;
        private int  numUpdatesAfterStartHappened = 0;
        private bool finishedRP1ProceduralAvionicsUpdate = false;

        public float AdditionalMass { get; set; }

        /// How many times have instances of this class been constructed during this process run?
        private static int constructorCount;

        private int kOSCoreId;
        /// <summary>Can be used as a unique ID of which processor this is, but unlike Guid,
        /// it doesn't remain unique across runs so you shouldn't use it for serialization.</summary>
        public int KOSCoreId { get { return kOSCoreId; } }

        private MovingAverage averagePower = new MovingAverage();

        // This is the "constant" byte count used when calculating the EC
        // required by the archive volume (which has infinite space).
        // TODO: This corresponds to the existing value and should be adjusted for balance.
        private const int ARCHIVE_EFFECTIVE_BYTES = 50000;

        private const string BootDirectoryName = "boot";

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Interpreter", groupName = PAWGroup, groupDisplayName = PAWGroup), UI_ChooseOption(scene = UI_Scene.All)]
        public string interpreterLanguage = "KerboScript";

        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Boot File", groupName = PAWGroup, groupDisplayName = PAWGroup), UI_ChooseOption(scene = UI_Scene.Editor)]
        public string bootFile = "None";

        [KSPField(isPersistant = true, guiName = "kOS Disk Space", guiActive = true, groupName = PAWGroup, groupDisplayName = PAWGroup)]
        public int diskSpace = 1024;

        [KSPField(isPersistant = true, guiName = "kOS Base Disk Space", guiActive = false, groupName = PAWGroup, groupDisplayName = PAWGroup)]
        public int baseDiskSpace = 0;

        [KSPField(isPersistant = false, guiName = "kOS Base Module Cost", guiActive = false, groupName = PAWGroup, groupDisplayName = PAWGroup)]
        public float baseModuleCost = 0F;  // this is the base cost added to a part for including the kOSProcessor, default to 0.

        [KSPField(isPersistant = true, guiName = "kOS Base Module Mass", guiActive = false, groupName = PAWGroup, groupDisplayName = PAWGroup)]
        public float baseModuleMass = 0F;  // this is the base mass added to a part for including the kOSProcessor, default to 0.

        [KSPField(isPersistant = false, guiName = "kOS Disk Space", guiActive = false, guiActiveEditor = true, groupName = PAWGroup, groupDisplayName = PAWGroup), UI_ChooseOption(scene = UI_Scene.Editor)]
        public string diskSpaceUI = "1024";

        [KSPField(isPersistant = true, guiName = "CPU/Disk Upgrade Cost", guiActive = false, guiActiveEditor = true, groupName = PAWGroup, groupDisplayName = PAWGroup)]
        public float additionalCost = 0F;

        [KSPField(isPersistant = false, guiName = "CPU/Disk Upgrade Mass", guiActive = false, guiActiveEditor = true, guiUnits = "Kg", guiFormat = "0.00", groupName = PAWGroup, groupDisplayName = PAWGroup)]
        public float additionalMassGui = 0F;

        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false)]
        public float diskSpaceCostFactor = 0.0244140625F; //implies approx 100funds for 4096bytes of diskSpace

        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false)]
        public float diskSpaceMassFactor = 0.0000000048829F;  //implies approx 0.020kg for 4096bytes of diskSpace

        [KSPField(isPersistant = true, guiActive = false)]
        public int MaxPartId = 100;

        // This represents how much EC to consume per executed instruction.
        // This would be the "variable" component of the processor's power.
        // Important: This value should be overriden in the part.cfg file
        // for the kOS processor.  The only reason it's being given a value
        // here is as a fallback for those cases where an old legacy part
        // might be loaded from before the part files had this value.
        [KSPField(isPersistant = true, guiActive = false)]
        public float ECPerInstruction = 0.000004F;

        // This represents how much EC to consume per Byte of the current volume, per second.
        // This would be the "continuous" component of the processor's power (though it varies
        // when you change to another volume).
        // IMPORTANT: The value defaults to zero and must be overriden in the module
        // definition for any given part (within the part.cfg file).
        [KSPField(isPersistant = true, guiActive = false)]
        public float ECPerBytePerSecond = 0F;

        public kOSProcessor()
        {
            ProcessorMode = ProcessorModes.READY;
            kOSCoreId = ++constructorCount;
        }

        public VolumePath BootFilePath {
            get
            {
                if (string.IsNullOrEmpty(bootFile) || bootFile.Equals("None", StringComparison.OrdinalIgnoreCase))
                    return null;
                return VolumePath.FromString(bootFile);
            }
        }

        [KSPEvent(guiActive = true, guiName = "Open Terminal", category = "skip_delay;", groupName = PAWGroup, groupDisplayName = PAWGroup)]
        public void Activate()
        {
            SafeHouse.Logger.Log("Open Window by event");
            OpenWindow();
        }

        [KSPEvent(guiActive = true, guiName = "Close Terminal", category = "skip_delay;", groupName = PAWGroup, groupDisplayName = PAWGroup)]
        public void Deactivate()
        {
            SafeHouse.Logger.Log("Close Window by event");
            CloseWindow();
        }

        [KSPField(isPersistant = true, guiName = "kOS Average Power", guiActive = true, guiActiveEditor = true, guiUnits = "EC/s", guiFormat = "0.000", groupName = PAWGroup, groupDisplayName = PAWGroup)]
        public float RequiredPower = 0;

        [KSPEvent(guiActive = true, guiName = "Toggle Power", groupName = PAWGroup, groupDisplayName = PAWGroup)]
        public void TogglePower()
        {
            SafeHouse.Logger.Log("Toggle Power");
            ProcessorModes newProcessorMode = (ProcessorMode != ProcessorModes.OFF) ? ProcessorModes.OFF : ProcessorModes.STARVED;
            SetMode(newProcessorMode);
        }

        [KSPAction("Open Terminal", actionGroup = KSPActionGroup.None)]
        public void Activate(KSPActionParam param)
        {
            SafeHouse.Logger.Log("Open Terminal from ActionGroup");
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

        [KSPAction("Suppress On", actionGroup = KSPActionGroup.None)]
        public void StartSuppressAutopilot(KSPActionParam param)
        {
            SafeHouse.Logger.Log("Start Suppress Autopilot from ActionGroup");
            SafeHouse.Config.SuppressAutopilot = true;
        }

        [KSPAction("Suppress Off", actionGroup = KSPActionGroup.None)]
        public void StopSuppressAutopilot(KSPActionParam param)
        {
            SafeHouse.Logger.Log("Stop Suppress Autopilot from ActionGroup");
            SafeHouse.Config.SuppressAutopilot = false;
        }

        [KSPAction("Toggle Suppression", actionGroup = KSPActionGroup.None)]
        public void ToggleSuppressAutopilot(KSPActionParam param)
        {
            SafeHouse.Logger.Log("Toggle Suppress Autopilot from ActionGroup");
            SafeHouse.Config.SuppressAutopilot = !SafeHouse.Config.SuppressAutopilot;
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
            if (shared.Window != null)
                return shared.Window;
            return GetComponent<Screen.TermWindow>();
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
            // Clamp this to prevent negative cost and mass.
            // (Can happen when people edit part.cfg data or
            // use ModuleManager.)
            float spaceDelta = Mathf.Max(diskSpace - baseDiskSpace, 0.0f);
            additionalCost = (float)System.Math.Round(spaceDelta * diskSpaceCostFactor, 0);
            AdditionalMass = spaceDelta * diskSpaceMassFactor;
            additionalMassGui = AdditionalMass * 1000;
        }

        private PartModule RP1AvionicsModule = null;

        private void FindRP1Modules()
        {
            RP1AvionicsModule = null;
            foreach (PartModule otherModule in part.Modules)
            {
                for (System.Type t = otherModule.GetType(); t != null; t = t.BaseType)
                {
                    if (t.Name.Contains("ModuleProceduralAvionics"))
                    {
                        RP1AvionicsModule = otherModule;
                        break;
                    }
                }
                if (RP1AvionicsModule != null)
                    break;
            }
            SafeHouse.Logger.Log(string.Format("FindRP1Module: {0}", RP1AvionicsModule != null ? "Found" : "Not Found"));
        }

        private void UpdateRP1TechLevel(bool InEditor)
        {
            if (RP1AvionicsModule != null)
            {
                var techNodePropInfo = RP1AvionicsModule.GetType().GetProperty("CurrentProceduralAvionicsTechNode");
                if (techNodePropInfo != null)
                {
                    var techNodeProp = techNodePropInfo.GetValue(RP1AvionicsModule);
                    if (techNodeProp != null)
                    {
                        System.Type techNodeType = techNodeProp.GetType();
                        System.Reflection.FieldInfo fieldInfo;

                        if (InEditor)
                        {
                            fieldInfo = techNodeType.GetField("kosDiskSpace");
                            int newDiskSpace = (fieldInfo != null) ? (int)fieldInfo.GetValue(techNodeProp) : 0;
                            if (newDiskSpace != baseDiskSpace && newDiskSpace > 0)
                            {
                                SafeHouse.Logger.Log(string.Format("Changing base disk space for RP-1 config change: {0} -> {1}", baseDiskSpace, newDiskSpace));

                                // Adjust disk space to be the same multiple of the new base disk space
                                diskSpace = newDiskSpace * diskSpace / baseDiskSpace;
                                baseDiskSpace = newDiskSpace;

                                PopulateDiskSpaceUI();
                            }
                        }

                        fieldInfo = techNodeType.GetField("kosSpaceCostFactor");
                        if (fieldInfo != null)
                            diskSpaceCostFactor = (float)fieldInfo.GetValue(techNodeProp);
                        fieldInfo = techNodeType.GetField("kosSpaceMassFactor");
                        if (fieldInfo != null)
                            diskSpaceMassFactor = (float)fieldInfo.GetValue(techNodeProp);
                        fieldInfo = techNodeType.GetField("kosECPerInstruction");
                        if (fieldInfo != null)
                            ECPerInstruction = (float)fieldInfo.GetValue(techNodeProp);
                    }
                }
            }
        }

        private void PopulateDiskSpaceUI()
        {
            //populate diskSpaceUI selector
            diskSpaceUI = diskSpace.ToString();
            BaseField field = Fields["diskSpaceUI"];
            UI_ChooseOption options = (UI_ChooseOption)field.uiControlEditor;
            var sizeOptions = new string[3];
            sizeOptions[0] = baseDiskSpace.ToString();
            sizeOptions[1] = (baseDiskSpace * 2).ToString();
            sizeOptions[2] = (baseDiskSpace * 4).ToString();
            options.options = sizeOptions;
        }

        //implement IPartMassModifier component
        public float GetModuleMass(float defaultMass, ModifierStagingSituation sit)
        {
            // the 'sit' arg is irrelevant to us, but the interface requires it.
            return baseModuleMass + AdditionalMass;
        }
        //implement IPartMassModifier component
        public ModifierChangeWhen GetModuleMassChangeWhen()
        {
            return ModifierChangeWhen.FIXED;
        }

        public override void OnStart(StartState state)
        {
            numUpdatesAfterStartHappened = 0;
            finishedRP1ProceduralAvionicsUpdate = false;
            try
            {
                FindRP1Modules();
                UpdateRP1TechLevel(state == StartState.Editor);
                InitInterpreterField(state);
                //if in Editor, populate boot script selector, diskSpace selector and etc.
                if (state == StartState.Editor)
                {
                    if (baseDiskSpace == 0)
                        baseDiskSpace = diskSpace;

                    InitUI();
                }

                // Removed: UpdateCostAndMass();
                // Please do not call UpdateCostAndMass() here because during OnStart() in RP-1, diskSpaceMassFactor is still
                // the very heavy default value - ProceduralAvionics Tech has not yet been updated for tech level during OnStart(),
                // so if we reported our mass now back to KSP, we'd have a super heavy probe core for just one physics frame that
                // might unfairly break wheels and legs and joints, etc.

                //Do not start from editor and at KSP first loading
                if (state == StartState.Editor || state == StartState.None)
                {
                    return;
                }

                SafeHouse.Logger.Log(string.Format("OnStart: {0} {1}", state, ProcessorMode));
                InitObjects();
            }
            catch (Exception e)
            {
                SafeHouse.Logger.LogException(e);
            }
        }

        public static void SetBootListDirty()
        {
            bootListDirty = true;
        }

        private void InitInterpreterField(StartState state)
        {
            BaseField interpreterLanguageField = Fields["interpreterLanguage"];
            List<string> interpreterOptions = new List<string> { "KerboScript", "Lua" };
            if (state == StartState.Editor) // TODO: show available boot files based on the selected interpreter
            {
                var interpreterLanguageOption = (UI_ChooseOption)interpreterLanguageField.uiControlEditor;
                interpreterLanguageOption.options = interpreterOptions.ToArray();
            } else
            {
                var interpreterLanguageOption = (UI_ChooseOption)interpreterLanguageField.uiControlFlight;
                interpreterLanguageOption.options = interpreterOptions.ToArray();
                interpreterLanguageOption.onFieldChanged = OnInterpreterChanged;
            }
        }

        private void OnInterpreterChanged(BaseField field, object prevValue)
        {
            UnityEngine.Debug.Log("interpreter changed. "+prevValue.ToString()+" to "+interpreterLanguage);
            if (interpreterLanguage == "Lua") shared.Interpreter = new LuaInterpreter(shared);
            else shared.Interpreter = new KSInterpreter(shared);
        }

        private void InitUI()
        {
            //Populate selector for boot scripts
            BaseField field = Fields["bootFile"];
            var options = (UI_ChooseOption)field.uiControlEditor;

            var bootFiles = new List<VolumePath>();

            bootFiles.AddRange(BootDirectoryFiles());

            //no need to show the control if there are no available boot files
            options.controlEnabled = bootFiles.Count >= 1;
            field.guiActiveEditor = bootFiles.Count >= 1;
            var availableOptions = bootFiles.Select(e => e.ToString()).ToList();
            var availableDisplays = bootFiles.Select(e => e.Name).ToList();

            availableOptions.Insert(0, "None");
            availableDisplays.Insert(0, "None");

            var bootFilePath = BootFilePath; // store the selected path temporarily
            if (bootFilePath != null && !bootFiles.Contains(bootFilePath))
            {
                // if the path is not null ("None", null, or empty) and if it isn't in list of files
                var archive = new Archive(SafeHouse.ArchiveFolder);
                var file = archive.Open(bootFilePath) as VolumeFile;  // try to open the file as saved
                if (file == null)
                {
                    // check the same file name, but in the boot directory.
                    var path = VolumePath.FromString(BootDirectoryName).Combine(bootFilePath.Name);
                    file = archive.Open(path) as VolumeFile; // try to open the new path
                    if (file == null)
                    {
                        // try the file name without "boot" prefix
                        var name = bootFilePath.Name;
                        if (name.StartsWith("boot", StringComparison.OrdinalIgnoreCase) && name.Length > 4)
                        {
                            // strip the boot prefix and try that file name
                            name = name.Substring(4);
                            path = VolumePath.FromString(BootDirectoryName).Combine(name);
                            file = name.StartsWith(".") ? null : archive.Open(path) as VolumeFile;  // try to open the new path
                            if (file == null)
                            {
                                // try the file name without "boot_" prefix
                                if (name.StartsWith("_", StringComparison.OrdinalIgnoreCase) && name.Length > 1)
                                {
                                    // only need to strip "_" here
                                    name = name.Substring(1);
                                    path = VolumePath.FromString(BootDirectoryName).Combine(name);
                                    file = name.StartsWith(".") ? null : archive.Open(path) as VolumeFile;  // try to open the new path
                                }
                            }
                        }
                    }
                }

                // now, if we have a file object, use its values.
                if (file != null)
                {
                    // store the boot file information
                    bootFile = file.Path.ToString();
                    if (!bootFiles.Contains(file.Path))
                    {
                        availableOptions.Insert(1, bootFile);
                        availableDisplays.Insert(1, "*" + file.Path.Name); // "*" is indication the file is not normally available
                    }
                }
            }
            SafeHouse.Logger.SuperVerbose("bootFile: " + bootFile);

            options.options = availableOptions.ToArray();
            options.display = availableDisplays.ToArray();

            bootListDirty = false;
            ForcePAWRefresh();

            PopulateDiskSpaceUI();
        }

        public void ForcePAWRefresh()
        {
            // Thanks to https://github.com/blowfishpro for finding this API call for me:
            UIPartActionWindow paw = UIPartActionController.Instance?.GetItem(part, false);

            if (paw != null)
            {
                paw.ClearList();
                paw.displayDirty = true;
            }
        }

        private IEnumerable<VolumePath> BootDirectoryFiles()
        {
            var result = new List<VolumePath>();

            var archive = new Archive(SafeHouse.ArchiveFolder);

            var path = VolumePath.FromString(BootDirectoryName);

            var bootDirectory = archive.Open(path) as VolumeDirectory;

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
                    result.Add(pair.Value.Path);
                }
            }

            return result;
        }

        private static Regex VolumeNameRemoveChars = new Regex("[/\\\\<>:\"|?*]*", RegexOptions.Compiled);

        public void InitObjects()
        {
            if (objectsInitialized)
            {
                SafeHouse.Logger.SuperVerbose("kOSProcessor.InitObjects() - objects already initialized");
                return;
            }
            objectsInitialized = true;

            CalcConstsFromKSP();

            shared = new SharedObjects();

            shared.Vessel = vessel;
            shared.Processor = this;
            shared.KSPPart = part;
            shared.UpdateHandler = new UpdateHandler();
            shared.BindingMgr = new BindingManager(shared);
            shared.Terminal = new Screen.ConnectivityTerminal(shared);
            shared.Screen = shared.Terminal;
            shared.ScriptHandler = new KSScript();
            shared.Logger = new KSPLogger(shared);
            shared.VolumeMgr = new ConnectivityVolumeManager(shared);
            shared.ProcessorMgr = new ProcessorManager();
            shared.FunctionManager = new FunctionManager(shared);
            shared.TransferManager = new TransferManager(shared);
            shared.Cpu = new CPU(shared);
            shared.AddonManager = new AddOns.AddonManager(shared);
            shared.GameEventDispatchManager = new GameEventDispatchManager(shared);

            if (interpreterLanguage == "Lua") shared.Interpreter = new LuaInterpreter(shared);
            else shared.Interpreter = new KSInterpreter(shared);
            // TODO: add methods like Boot, Shutdown to IInterpreterLink
            // OnInterpreterChanged would call them to swap interpreters out so they dont run at the same time

            // Make the window that is going to correspond to this kOS part:
            shared.Window = gameObject.AddComponent<Screen.TermWindow>();
            shared.Window.AttachTo(shared);
            shared.SoundMaker = shared.Window.GetSoundMaker();

            // initialize archive
            Archive = new Archive(SafeHouse.ArchiveFolder);
            shared.VolumeMgr.Add(Archive);

            Messages = new MessageQueue();

            // initialize harddisk
            if (HardDisk == null)
            {
                HardDisk = new Harddisk(diskSpace);

                if (!string.IsNullOrEmpty(Tag))
                {
                    // Tag could contain characters that are not allowed.
                    var tmpTag = VolumeNameRemoveChars.Replace(Tag, "");

                    if( !string.IsNullOrWhiteSpace(tmpTag))
                    {
                        HardDisk.Name = tmpTag.Replace(' ', '_');
                    }
                }

                var path = BootFilePath;
                // populate it with the boot file, but only if using a new disk:
                if (path != null && !SafeHouse.Config.StartOnArchive)
                {
                    var bootVolumeFile = Archive.Open(BootFilePath) as VolumeFile;
                    if (bootVolumeFile != null)
                    {
                        if (HardDisk.SaveFile(BootFilePath, bootVolumeFile.ReadAll()) == null)
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

        // The official value of some physics constants change over time as standards bodies re-calculate them.
        // This code below ensures we're using whatever value KSP itself is using.
        // The reason this code is *here* not in ConstantValue is because ConstantValue can't call
        // the KSP API.  It's in kOS.Safe.
        private void CalcConstsFromKSP()
        {
            // GravitationalAcceleration did not exist in PhysicsGlobals prior to KSP 1.6.x.
            // This code has to use reflection to avoid calling it on older backports:
            Type physGlobType = typeof(PhysicsGlobals);
            if (physGlobType != null)
            {
                // KSP often changes its mind whether a member is a Field or Property, so let's write this
                // to future-proof against them changing which it is by trying both ways:
                FieldInfo asField = (physGlobType.GetField("GravitationalAcceleration", BindingFlags.Public | BindingFlags.Static));
                if (asField != null)
                    ConstantValue.G0 = (double) asField.GetValue(null);
                else
                {
                    PropertyInfo asProperty = (physGlobType.GetProperty("GravitationalAcceleration", BindingFlags.Public | BindingFlags.Static));
                    if (asProperty != null)
                        ConstantValue.G0 = (double)asProperty.GetValue(null, null);
                }
            }
            // Fallback: Note if none of the above work, G0 still does have a reasonable value because we
            // hardcode it to a literal in ConstantValue before doing any of the above work.


            // Cannot find anything in KSP's API exposing their value of G, so this indirect means
            // of calculating it from an arbitrary body is used:
            CelestialBody anyBody = FlightGlobals.fetch.bodies.FirstOrDefault();
            if (anyBody == null)
                SafeHouse.Logger.LogError("kOSProcessor: This game installation is badly broken.  It appears to have no planets in it.");
            else
                ConstantValue.GravConst = anyBody.gravParameter / anyBody.Mass;

            ConstantValue.AvogadroConst = PhysicsGlobals.AvogadroConstant;
            ConstantValue.BoltzmannConst = PhysicsGlobals.BoltzmannConstant;
            ConstantValue.IdealGasConst = PhysicsGlobals.IdealGasConstant;
        }

        private void InitProcessorTracking()
        {
            // Track a list of all instances of me that exist:
            if (!allMyInstances.Contains(this))
            {
                allMyInstances.Add(this);
                SortAllInstances();
            }
        }

        public static void SortAllInstances()
        {
            allMyInstances.Sort(delegate (kOSProcessor a, kOSProcessor b)
            {
                // THIS SORT IS EXPENSIVE BECAUSE IT KEEPS RECALCULATING THE CRITERIA
                // (DISTANCE BETWEEN VESSELS, HOPS TO ROOT PART) ON EACH PAIRWISE
                // COMPARISON OF TWO ITEMS DURING THE SORT, RATHER THAN REMEMBERING A
                // PART'S SCORE AND RE-USING THAT ON SUBSEQUENT PAIRWISE COMPARISONS WITH THAT PART.
                // I DON'T THINK OPTOMIZING IT IS WORTH IT WHEN THIS SORT WON"T BE DONE SUPER FREQUENTLY,
                // MAYBE ONCE EVERY 1 or 2 SECONDS AT MOST.

                // sort "nulls" to be last:
                if (a.part == null || a.part.vessel == null)
                    return 1;
                if (b.part == null || b.part.vessel == null)
                    return -1;

                // If on diffrent vessels, sort by distance of vessel from active vessel - nearest vessels first:
                if (a.part.vessel != b.part.vessel)
                {
                    Vector3d activePos = FlightGlobals.ActiveVessel.GetWorldPos3D();
                    // May as well use square magnitude - it's faster and sorts things in the same order:
                    double aSquareDistance = (activePos - a.part.vessel.GetWorldPos3D()).sqrMagnitude;
                    double bSquareDistance = (activePos - b.part.vessel.GetWorldPos3D()).sqrMagnitude;
                    return (aSquareDistance < bSquareDistance) ? -1 : 1;
                }

                // If it gets to here, they're on the same vessel.
                // So sort by number of parent links to walk to get to root part (closest to root goes first).
                int aCountParts = 0;
                int bCountParts = 0;
                for (Part p = a.part; p != null; p = p.parent)
                    ++aCountParts;
                for (Part p = b.part; p != null; p = p.parent)
                    ++bCountParts;
                if (aCountParts != bCountParts)
                    return aCountParts - bCountParts;

                // If it gets to here it's a tie so far - two parts were an equal number of parts away from root,
                // which can easily happen if the kOS CPUs were attached with symmetry in the VAB/SPH.

                // We CANNOT have a tie.  We need a deterministic order because differences in the list is
                // how we discover the CPU list has chnaged.  So make one final arbitrary thing to break the
                // tie with:
                uint aUID = a.part.uid();
                uint bUID = b.part.uid();
                return (aUID < bUID ? -1 : 1);

            });
        }

        public void OnDestroy()
        {
            SafeHouse.Logger.SuperVerbose("kOSProcessor.OnDestroy()!");

            allMyInstances.RemoveAll(m => m == this);
            SortAllInstances();

            if (shared != null)
            {
                shared.Cpu.BreakExecution(false);
                shared.Cpu.Dispose();
                shared.DestroyObjects();
                shared = null;
            }
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
            bool isInEditor = false;
            if (HighLogic.LoadedScene == GameScenes.EDITOR && EditorLogic.fetch != null)
            {
                isInEditor = true;
                if (bootListDirty)
                {
                    InitUI();
                }
                UpdateRP1TechLevel(true);
                if (diskSpace != Convert.ToInt32(diskSpaceUI))
                {
                    diskSpace = Convert.ToInt32(diskSpaceUI);
                    UpdateCostAndMass();
                    GameEvents.onEditorShipModified.Fire(EditorLogic.fetch.ship);
                }
                RequiredPower = this.diskSpace * ECPerBytePerSecond + SafeHouse.Config.InstructionsPerUpdate * ECPerInstruction / Time.fixedDeltaTime;
            }
            if (!IsAlive()) return;

            // Conceptually this next bit really belongs in OnStart() because it should only happen
            // once up front during scene load, but when RP-1 is installed and this KOSProcessor
            // is inside the same part as RP-1's ModuleProceduralAvionics, a race condition develops
            // between the two if KOSProcessor.OnStart() tries to do the following work.
            // The values queried from ModuleProceduralAvionics for mass cost per byte of disk
            // are defaulted to the bottom rung starting 1950's tech *at first* until it has had a chance
            // to run its OnStart() to correct those values to the actual avionics tech level of the part.
            // Thus if KOSProcessor tried to do the following code in its OnStart(), and KOS's OnStart()
            // happened before ModuleProceduralAvionics' OnStart(), the KOSProcessor would end up being
            // several tonnes when it should only be a few kilograms. (Issue #3081)
            //
            // It is *hoped* that this work will occur before physics is turned on, so the part gets the
            // corrected mass before it has a chance to start crushing landing legs and wheels and so on.
            // Allegedly, Unity will invoke all the Update()s several times before KSP turns physics on so
            // this *should* be okay.
            if (!finishedRP1ProceduralAvionicsUpdate && !isInEditor )
            {
                float massFactorBefore = diskSpaceMassFactor;
                UpdateRP1TechLevel(false);
                UpdateCostAndMass();
                // Stop wasting CPU time doing this recalculation when either the value has changed to a new number (proving
                // that RP-1 has finally applied its tech upgrade which is why the value changed), or because we tried long
                // enough to prove we really are at the default starting tech level so it's not going to change.)
                if (diskSpaceMassFactor != massFactorBefore || numUpdatesAfterStartHappened > 60 )
                {
                    finishedRP1ProceduralAvionicsUpdate = true;
                }
            }
            UpdateVessel();
            UpdateObservers();
            ++numUpdatesAfterStartHappened;
        }

        public void FixedUpdate()
        {
            if (!IsAlive()) return;

            if (!vessel.HoldPhysics)
            {
                if (!HasBooted)
                {
                    SafeHouse.Logger.LogWarning("First Update()");
                    shared.Cpu.Boot(); // TODO: add to InterpreterLink
                    HasBooted = true;
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
            SafeHouse.Logger.SuperVerbose("kOSProcessor.OnLoad");
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
            SafeSerializationMgr.CheckIDumperStatics();
        }

        private void ProcessElectricity(Part partObj, float time)
        {
            if (ProcessorMode == ProcessorModes.OFF) return;

            double volumePower = 0;
            if (shared.VolumeMgr.CheckCurrentVolumeRange())
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
                int instructions = System.Math.Max(shared.Interpreter.InstructionsThisUpdate(), 1);
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
                    if (SafeHouse.Config.StartOnArchive)
                    {
                        shared.VolumeMgr.SwitchTo(Archive);
                    }
                    else
                    {
                        shared.VolumeMgr.SwitchTo(HardDisk);
                    }
                    HasBooted = false; // When FixedUpdate() first happesn, then the boot will happen.
                    if (shared.Terminal != null) shared.Terminal.SetInputLock(false);
                    if (shared.Window != null) shared.Window.IsPowered = true;
                    foreach (var w in shared.ManagedWindows) w.IsPowered = true;
                    break;

                case ProcessorModes.OFF:
                case ProcessorModes.STARVED:
                    if (shared.Cpu != null) shared.Interpreter.BreakExecution(true);
                    if (shared.Terminal != null) shared.Terminal.SetInputLock(true);
                    if (shared.Window != null) shared.Window.IsPowered = false;
                    if (shared.SoundMaker != null) shared.SoundMaker.StopAllVoices();
                    foreach (var w in shared.ManagedWindows) w.IsPowered = false;
                    kOSVesselModule vesselModule = kOSVesselModule.GetInstance(shared.Vessel);
                    if (!vesselModule.AnyProcessorReady()) vesselModule.OnAllProcessorsStarved();
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
            // First change the real setting underneath the UI:
            FlightGlobals.ActiveVessel.Autopilot.SetMode((VesselAutopilot.AutopilotMode) mode);
        }

        public string BootFilename
        {
            get { return bootFile; }
            set { bootFile = value; }
        }

        public bool CheckCanBoot()
        {
            if (shared.VolumeMgr == null) { shared.Logger.Log("No volume mgr"); }
            else if (!shared.VolumeMgr.CheckCurrentVolumeRange()) { shared.Logger.LogException(new Safe.Exceptions.KOSVolumeOutOfRangeException("Boot")); }
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
            Messages.Push(Message.Create(content, sentAt, sentAt, VesselTarget.CreateOrGetExisting(shared), Tag));
        }
    }
}



