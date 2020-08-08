using KSP.IO;
using System;
using System.Reflection;
using System.Linq;

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

        [GameParameters.CustomParameterUI("")]
        public bool passedClickThroughCheck = false;

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
            if (member.Name == "migrated" || member.Name == "version")
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

        public void CheckClickThroughBlockerExists()
        {
            if (passedClickThroughCheck)
                return;
            bool clickThroughExists = false;

            var loadedCTBAssembly = AssemblyLoader.loadedAssemblies.FirstOrDefault(a => a.dllName.Equals("ClickThroughBlocker"));
            if (loadedCTBAssembly != null)
            {
                // Must be at least version 0.10 of ClickThroughBlocker:
                if (loadedCTBAssembly.versionMajor > 0 || loadedCTBAssembly.versionMinor >= 10)
                {
                    Type ctbType = loadedCTBAssembly.assembly.GetType("ClickThroughFix.CTB", false);
                    if (ctbType != null)
                    {
                        if (ctbType.GetField("focusFollowsclick") != null)
                        {
                            clickThroughExists = true;
                        }
                    }
                }
            }

            string popupText =
                "=======================================\n" +
                "<b><color=#ffffff>kOS is Checking for ClickThroughBlocker</color></b>\n" +
                "=======================================\n\n" +
                "Starting with kOS v1.3, kOS has become dependent on the existence of the ClickThroughBlocker mod. " +
                "(And it must be at least version 0.10 of ClickThroughBlocker.)\n\n";

            if (clickThroughExists)
            {
                popupText +=
                    "     <b><color=#ddffdd><<<< CHECK SUCCEEDED >>>>></color></b>\n\n" +
                    "kOS has found ClickThroughBlocker installed, and it appears to be a version that will work with kOS.\n" +
                    "\n" +
                    "Please note that while in the past the kOS terminal has always been click-to-focus, from now " +
                    "on it will behave however ClickThroughBlocker is set to act, which may be focus-follows-mouse.\n" +
                    "You can use ClickThroughBlocker's settings to change this behvior like this:\n\n" +
                    "[Hit Escape] Settings ->\n" +
                    "  Difficulty Options ->\n" +
                    "    ClickThroughBlocker ->\n" +
                    "      [x] Focus Follows Click\n\n";
            }
            else
            {
                popupText +=
                    "     <b><color=#ffff88>!!! CHECK FAILED !!!</color></b>\n\n" +
                    "kOS couldn't find a version of ClickThroughBlocker that works with kOS. This could be " +
                    "because ClickThroughBlocker is not installed at all, or it could be because its version is too old " +
                    "(or too new, if ClickThroughBlocker ever renames some things that kOS is using).\n" +
                    "\n\n" +
                    "To use kOS v1.3 or higher you'll need to quit Kerbal Space Program and install a version of ClickThroughBlocker that it supports.\n";
            }

            string buttonText;
            global::Callback clickThroughAck;
            if (clickThroughExists)
            {
                clickThroughAck = AcceptClickThrough;
                buttonText = "Acknowledged.";
            }
            else
            {
                clickThroughAck = FailedClickThrough;
                buttonText = "Acknowledged. I'll have to quit and change my mods.";
            }

            kOSSettingsChecker.QueueDialog(
                0.75f, 0.6f,
                new MultiOptionDialog(
                    "ClickThroughBlockerCheck",
                    popupText,
                    "kOS ClickThroughBlocker Check",
                    HighLogic.UISkin,
                    new DialogGUIButton(buttonText, clickThroughAck, true)
                    ));
        }

        public void AcceptClickThrough()
        {
            passedClickThroughCheck = true;
        }

        public void FailedClickThrough()
        {
            passedClickThroughCheck = false;
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