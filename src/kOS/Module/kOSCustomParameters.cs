using KSP.IO;
using System;
using System.Reflection;

namespace kOS.Module
{
    public class kOSCustomParameters : GameParameters.CustomParameterNode
    {
        private static kOSCustomParameters instance;

        public static kOSCustomParameters Instance
        {
            get
            {
                if (instance == null)
                {
                    if (HighLogic.CurrentGame != null)
                    {
                        instance = HighLogic.CurrentGame.Parameters.CustomParams<kOSCustomParameters>();
                    }
                }
                return instance;
            }
        }

        public const string MIGRATION_DIALOG_TEXT = "Some kOS settings are now tracked seperately per-game " +
            "using the stock settings menu. " +
            "\n\n" +
            "You currently seem to have some of these settings stored in kOS's " +
            "global folder, probably because you were using a previous version of kOS in the past. " +
            "You can migrate these settings from their global (now unused) location into this game's individual settings if you like. " +
            "(Note that the telnet server settings are still kept globally, but everything else has moved.)" +
            "\n\n<color=#ffffff>" +
            "The new place to adjust the settings in-game is in the <color=#ffff00>\"Difficulty Options\"</color> button " +
            "of the in-game settings window. (Press <color=#ffff00>ESC</color>, pick <color=#ffff00>Settings</color>, " +
            "then <color=#ffff00>Difficulty Options</color>, then <color=#ffff00>kOS</color> to reach them now.)" +
            "\n" +
            "The settings are there despite them not really being about \"Difficulty\". " +            
            "That's just the location where KSP allows mods to make custom parameters." +
            "</color>\n\n" +
            "Would you like to migrate now?";

        [GameParameters.CustomParameterUI("")]
        public bool migrated = false;

        [GameParameters.CustomIntParameterUI("")]
        public int version = 0;

        // these values constrain and back the InstructionsPerUpdate property so that it is clamped both in the
        // user interface and when set from within a script.
        private const int ipuMin = 50;
        private const int ipuMax = 2000;
        private int instructionsPerUpdate = 200;

        [GameParameters.CustomIntParameterUI("Instructions per update", minValue = ipuMin, maxValue = ipuMax,
                                            toolTip = "All CPU's run at a speed that executes up to\n" +
                                            "this many kRISC opcodes per physics 'tick'.")]
        public int InstructionsPerUpdate
        {
            get
            {
                return instructionsPerUpdate;
            }
            set
            {
                instructionsPerUpdate = Math.Max(ipuMin, Math.Min(ipuMax, value));
            }
        }
        
        private const int luaIpuMin = 150;
        private const int luaIpuMax = 2000;
        private int luaInstructionsPerUpdate = 200;

        [GameParameters.CustomIntParameterUI("Lua instructions per update", minValue = luaIpuMin, maxValue = luaIpuMax,
                                            toolTip = "Maximum number of instructions used per physics tick by CPUs using lua")]
        public int LuaInstructionsPerUpdate
        {
            get => luaInstructionsPerUpdate;
            set => luaInstructionsPerUpdate = Math.Max(luaIpuMin, Math.Min(luaIpuMax, value));
        }

        [GameParameters.CustomParameterUI("Enable compressed storage",
                                         toolTip = "When storing local volumes' data in the saved game,\n"+
                                         "it will be compressed then base64 encoded.")]
        public bool useCompressedPersistence = true;

        [GameParameters.CustomParameterUI("Show statistics",
                                         toolTip = "After the outermost program is finished, you will\n" +
                                         "see some profiling output describing how fast it ran.")]
        public bool showStatistics = false;
        
        [GameParameters.CustomParameterUI("Start on the archive",
                                         toolTip = "When launching a new ship, or reloading a scene,\n" +
                                         "the default volume will start as 0 instead of 1.")]
        public bool startOnArchive = false;

        [GameParameters.CustomParameterUI("Obey hide UI toggle",
                                          toolTip = "When you press the \"Hide UI\" button (F2 in default bindings)\n" +
                                          "kOS's terminals will hide themselves too.")]
        public bool obeyHideUi = true;

        [GameParameters.CustomParameterUI("Enable safe mode",
                                         toolTip = "kOS will throw an error if Infinity or Not-A-Number is the result\n" +
                                         "of any expression.  This ensures no such values can ever get\n"+
                                         "passed in to KSP's stock API, which doesn't protect itself against their effects.")]
        public bool enableSafeMode = true;

        [GameParameters.CustomParameterUI("Audible exceptions",
                                         toolTip = "When kOS throws an error, you hear a sound effect.")]
        public bool audibleExceptions = true;

        [GameParameters.CustomParameterUI("Verbose exceptions",
                                         toolTip = "When kOS has an error, some error messages have alternative longer\n" +
                                         "paragraph-length descriptions that this enables.")]
        public bool verboseExceptions = true;

        [GameParameters.CustomParameterUI("Allow clobbering built-ins",
                                         toolTip = "True if scripts' variable and function names are allowed to hide or clobber kOS's\n" +
                                         "built-in variables and functions, like THROTTLE, SHIP, PRINT(), and so on. This setting\n " +
                                         "can be overridden per-script with the @CLOBBERBUILTINS compiler directive.\n" +
                                         "(This should only be turned on if you need it for backward compatibilty to run older\n" +
                                         "scripts written prior to kOS v1.4.x, from before kOS was enforcing this check.)")]
        public bool clobberBuiltIns = false;

        [GameParameters.CustomParameterUI("Only use Blizzy toolbar", 
                                         toolTip = "If you have the \"Blizzy Toolbar\" mod installed, only put the kOS\n" +
                                         "button on it instead of both it and the stock toolbar.")]
        public bool useBlizzyToolbarOnly = false;

        [GameParameters.CustomParameterUI("Debug each opcode",
                                         toolTip = "(For mod developers) Spams the Unity log file with a message for every time\n" +
                                         "an opcode is executed in the virtual machine.  Very laggy.")]
        public bool debugEachOpcode = false;

        public override GameParameters.GameMode GameMode
        {
            get
            {
                return GameParameters.GameMode.ANY;
            }
        }

        public override bool HasPresets
        {
            get
            {
                return false;
            }
        }

        public override string DisplaySection { get { return "kOS"; } }

        public override string Section
        {
            get
            {
                return "kOS";
            }
        }

        public override int SectionOrder
        {
            get
            {
                return 0;
            }
        }

        public override string Title
        {
            get
            {
                return "CONFIG";
            }
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            instance = null;
        }

        public override bool Enabled(MemberInfo member, GameParameters parameters)
        {
            if (member.Name == "migrated" || member.Name == "version" || member.Name == "passedClickThroughCheck")
            {
                return false;
            }
            return base.Enabled(member, parameters);
        }

        public void CheckMigrateSettings()
        {
            Safe.Utilities.SafeHouse.Logger.SuperVerbose("kOSCustomParameters.CheckMigrateSettings()");
            if (!migrated)
            {
                var config = PluginConfiguration.CreateForType<kOSCustomParameters>();
                config.load();
                var ipu = config.GetValue("InstructionsPerUpdate", -1);
                // if the ipu is set below zero, it means that the file was created after we switch to
                // the new system, or that the user selected to prevent future migrations.
                if (ipu > 0)
                {
                    kOSSettingsChecker.QueueDialog(
                        0.5f, 0.5f, // causes it to be centered (half of box's own width left and down from center is the corner).
                        new MultiOptionDialog(
                            "Migration Dialog",
                            MIGRATION_DIALOG_TEXT,
                            "kOS",
                            HighLogic.UISkin,
                            new DialogGUIButton("Yes: migrate settings", MigrateSettingsNormal, true),
                            new DialogGUIButton("Yes: migrate settings this one time,\nbut never ask again for this or any other game", MigrateSettingsPrevent, true),
                            new DialogGUIButton("No: start with new default settings", DontMigrate, true),
                            new DialogGUIButton("No: start with new default settings\nand never ask again for this or any other game", DontMigrateAndPrevent, true)
                            ));
                }
                else
                {
                    Safe.Utilities.SafeHouse.Logger.LogError("ipu: " + ipu.ToString());
                    migrated = true;
                }
            }
        }

        public void MigrateSettingsNormal()
        {
            MigrateSettings(false);
        }

        public void MigrateSettingsPrevent()
        {
            MigrateSettings(true);
        }

        public void MigrateSettings(bool preventFuture)
        {
            var config = PluginConfiguration.CreateForType<kOSCustomParameters>();
            config.load();
            InstructionsPerUpdate = config.GetValue("InstructionsPerUpdate", -1);
            useCompressedPersistence = config.GetValue<bool>("InstructionsPerUpdate");
            showStatistics = config.GetValue<bool>("InstructionsPerUpdate");
            startOnArchive = config.GetValue<bool>("StartOnArchive");
            obeyHideUi = config.GetValue<bool>("ObeyHideUI");
            enableSafeMode = config.GetValue<bool>("EnableSafeMode");
            audibleExceptions = config.GetValue<bool>("AudibleExceptions");
            verboseExceptions = config.GetValue<bool>("VerboseExceptions");
            debugEachOpcode = config.GetValue<bool>("DebugEachOpcode");
            useBlizzyToolbarOnly = config.GetValue<bool>("UseBlizzyToolbarOnly");

            config.SetValue("SettingMigrationComment", "All settings except telnet settings are now stored in the game's save file. Settings stored here will be ignored.");
            if (preventFuture)
            {
                config.SetValue("InstructionsPerUpdate", -2); // using -2 so it's different from the default value used above
                config.SetValue("PreventFutureMigrationComment", "The user selected to prevent future migration notices when loading or creating save files.  Change the IPU value to a positive value to re-enable migrations.");
            }
            config.save();
            migrated = true;
        }

        public void DontMigrate()
        {
            var config = PluginConfiguration.CreateForType<kOSCustomParameters>();
            config.load();
            config.SetValue("SettingMigrationComment", "All settings except telnet settings are now stored in the game's save file. Settings stored here will be ignored.");
            config.save();
            migrated = true;
        }

        public void DontMigrateAndPrevent()
        {
            var config = PluginConfiguration.CreateForType<kOSCustomParameters>();
            config.load();
            config.SetValue("InstructionsPerUpdate", -2); // using -2 so it's different from the default value
            config.SetValue("SettingMigrationComment", "All settings except telnet settings are now stored in the game's save file. Settings stored here will be ignored.");
            config.SetValue("PreventFutureMigrationComment", "The user selected to prevent future migration notices when loading or creating save files.  Change the IPU value to a positive value to re-enable migrations.");
            config.save();
            migrated = true;
        }
    }
}
