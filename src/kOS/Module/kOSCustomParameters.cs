using KSP.IO;
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
                    instance = HighLogic.CurrentGame.Parameters.CustomParams<kOSCustomParameters>();
                }
                return instance;
            }
        }

        public const string MIGRATION_DIALOG_TEXT = "Some kOS settings are now stored in the KSP save file instead " +
            "of an external file. Telnet settings are still stored in the external file. It appears that you still " +
            "have valid settings stored in the external file, but that this is either a new save or the save has " +
            "not yet been migrated.  Would you like to migrate now?";

        [GameParameters.CustomParameterUI("")]
        public bool migrated = false;

        [GameParameters.CustomIntParameterUI("")]
        public int version = 0;

        [GameParameters.CustomIntParameterUI("Instructions per update", minValue = 50, maxValue = 2000)]
        public int instructionsPerUpdate = 200;

        [GameParameters.CustomParameterUI("Enable compressed storage")]
        public bool useCompressedPersistence = true;

        [GameParameters.CustomParameterUI("Show statistics")]
        public bool showStatistics = false;

        [GameParameters.CustomParameterUI("Enable Remote Tech integration")]
        public bool enableRTIntegration = true;

        [GameParameters.CustomParameterUI("Start on the archive")]
        public bool startOnArchive = false;

        [GameParameters.CustomParameterUI("Obey hide UI toggle")]
        public bool obeyHideUi = true;

        [GameParameters.CustomParameterUI("Enable safe mode")]
        public bool enableSafeMode = true;

        [GameParameters.CustomParameterUI("Audible exceptions")]
        public bool audibleExceptions = true;

        [GameParameters.CustomParameterUI("Verbose exceptions")]
        public bool verboseExceptions = true;

        [GameParameters.CustomParameterUI("Only use Blizzy toolbar")]
        public bool useBlizzyToolbarOnly = false;

        [GameParameters.CustomParameterUI("Debug each opcode")]
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
                    kOSSettingsChecker.QueueDialog(new MultiOptionDialog(
                            MIGRATION_DIALOG_TEXT,
                            "kOS",
                            HighLogic.UISkin,
                            new DialogGUIButton("Yes, migrate settings", MigrateSettingsNormal, true),
                            new DialogGUIButton("Yes, migrate and prevent future migrations", MigrateSettingsPrevent, true),
                            new DialogGUIButton("No, start with default settings", () => { migrated = true; }, true)
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
            instructionsPerUpdate = config.GetValue("InstructionsPerUpdate", -1);
            useCompressedPersistence = config.GetValue<bool>("InstructionsPerUpdate");
            showStatistics = config.GetValue<bool>("InstructionsPerUpdate");
            enableRTIntegration = config.GetValue<bool>("EnableRTIntegration");
            startOnArchive = config.GetValue<bool>("StartOnArchive");
            obeyHideUi = config.GetValue<bool>("ObeyHideUI");
            enableSafeMode = config.GetValue<bool>("AudibleExceptions");
            audibleExceptions = config.GetValue<bool>("EnableSafeMode");
            verboseExceptions = config.GetValue<bool>("VerboseExceptions");
            debugEachOpcode = config.GetValue<bool>("UseBlizzyToolbarOnly");
            useBlizzyToolbarOnly = config.GetValue<bool>("UseBlizzyToolbarOnly");

            config.SetValue("SettingMigrationComment", "All settings except telnet settings are now stored in the game's save file. Settings stored here will be ignored.");
            if (preventFuture)
            {
                config.SetValue("InstructionsPerUpdate", -2); // using -2 so it's different from the default value used above
                config.SetValue("PreventFutureMigrationComment", "The user selected to prevent future migration notices when loading or creating save files.  Change the IPU value to a positive value to re-enable migrations.");
            }
            GameSettings.SaveSettings();
            config.save();
            migrated = true;
        }
    }
}