using System;
using System.IO;
using System.Linq;
using kOS.Safe.Persistence;
using kOS.Safe.Utilities;
using kOS.Suffixed;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace kOS.Module
{
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class Bootstrapper : MonoBehaviour
    {
        private string legacyArchiveFolder;
        private const string LEGACY_KOS_EXTENSION = ".txt";
        private string backupFolder;

        private const string LEGACY_KOS_FOLDER_DESC = "The kOS v0.15 update has moved the archive folder to /Ships/Script/ and " +
                                                      "changed the file extension from *.txt to *.ks to be more in line with " +
                                                      "squad's current folder structure. Would you like us to attempt to migrate " +
                                                      "your existing scripts?";
        private const string LEGACY_KOS_BOOT_DESC = "The kOS v1.0.0 update has updated how boot files are handled.  Boot file are " +
                                                    "now expected to within a boot folder on the archive.  Would you like us to " +
                                                    "attempt to migrate your existing scripts?  See the warning regarding the boot " +
                                                    "directory at http://kos.github.io/KOS_DOC/general/volumes.html#special-handling-of-files-in-the-boot-directory " +
                                                    "for more details.";

        private bool backup = true;
        
        public void Start()
        {
            legacyArchiveFolder = GameDatabase.Instance.PluginDataFolder + "/Plugins/PluginData/Archive/";
            backupFolder = GameDatabase.Instance.PluginDataFolder + "/GameData/kOS/Backup_" + DateTime.Now.ToFileTimeUtc();
            BuildEnvironment();
            BuildLogger();

            CheckForLegacyArchive();

            CheckForUnpickedFont();
            
            var assemblies = AssemblyLoader.loadedAssemblies.Where(a => a.dllName.StartsWith("kOS.") || a.dllName.Equals("kOS") || a.dependencies.Where(d => d.name.Equals("kOS")).Any()).Select(a => a.assembly).ToArray();
            AssemblyWalkAttribute.Walk(assemblies);
        }

        private void BuildEnvironment()
        {
            SafeHouse.Init(
                Config.Instance, 
                Core.VersionInfo,
                "https://ksp-kos.github.io/KOS_DOC/",
                Application.platform == RuntimePlatform.WindowsPlayer,
                GameDatabase.Instance.PluginDataFolder + "/Ships/Script/"
                );
        }

        private void BuildLogger()
        {
            if (SafeHouse.Logger != null) return;
            Debug.Log(string.Format("{0} Init Logger", KSPLogger.LOGGER_PREFIX));
            SafeHouse.Logger = new KSPLogger();
        }

        private void CheckForLegacyArchive()
        {
            if (!Directory.Exists(legacyArchiveFolder))
            {
                CheckForLegacyBoot();
                return;
            }

            if (Directory.Exists(SafeHouse.ArchiveFolder))
            {
                CheckForLegacyBoot();
                return;
            }
            
            PopupDialog.SpawnPopupDialog(
                new MultiOptionDialog(
                    "Legacy kOS",
                    LEGACY_KOS_FOLDER_DESC,
                    "kOS",
                    HighLogic.UISkin,
                    new DialogGUIButton("Yes, Do it!", MigrateScripts, true),
                    new DialogGUIButton("No, I'll do it myself", CheckForLegacyBoot, true),
                    new DialogGUIToggle(true, "Backup my scripts first", (bool val) => backup = val)
                    ),
                true,
                HighLogic.UISkin
                );
        }

        private void CheckForLegacyBoot()
        {
            string bootDirectory = Path.Combine(SafeHouse.ArchiveFolder, "boot");
            // if the boot directory exists, we presume the migration has already been done.
            if (Directory.Exists(bootDirectory))
                return;
            Directory.CreateDirectory(bootDirectory);

            // if there aren't any files to migrate, don't bother showing the migration option.
            if (Directory.GetFiles(SafeHouse.ArchiveFolder, "boot*").Length == 0)
                return;

            backup = true;

            PopupDialog.SpawnPopupDialog(
                new MultiOptionDialog(
                    "Legacy kOS Boot",
                    LEGACY_KOS_BOOT_DESC,
                    "kOS",
                    HighLogic.UISkin,
                    new DialogGUIButton("Yes, and strip \"boot\" prefixes", MigrateBootAndRename, true),
                    new DialogGUIButton("Yes, but don't rename", MigrateBootNoRename, true),
                    new DialogGUIButton("No, I'll handle it myself", () => { }, true),
                    new DialogGUIToggle(backup, "Copy the scripts, don't move them", (bool val) => backup = val)
                    ),
                true,
                HighLogic.UISkin
                );
        }

        private void CheckForUnpickedFont()
        {
            if (SafeHouse.Config.TerminalFontName.Equals("_not_chosen_yet_"))
            {
                SafeHouse.Config.TerminalFontName = "Choose Font";
            }
        }

        private void MigrateScripts()
        {
            if (backup)
            {
                BackupScripts();
            }

            SafeHouse.Logger.Log("ScriptMigrate START");
            Directory.CreateDirectory(SafeHouse.ArchiveFolder);

            var files = Directory.GetFiles(legacyArchiveFolder);

            foreach (var fileName in files)
            {
                if (fileName == null) { continue; }

                var fileInfo = new FileInfo(fileName);

                string newFileName;
                var extension = Path.GetExtension(fileName);

                if (extension == LEGACY_KOS_EXTENSION)
                {
                    var bareFilename = Path.GetFileNameWithoutExtension(fileName);
                    const string NEW_EXTENSION = Archive.KERBOSCRIPT_EXTENSION;
                    newFileName = string.Format("{0}/{1}.{2}", SafeHouse.ArchiveFolder, bareFilename, NEW_EXTENSION);
                }
                else
                {
                    newFileName = SafeHouse.ArchiveFolder + Path.DirectorySeparatorChar + Path.GetFileName(fileName);
                }

                SafeHouse.Logger.Log("ScriptMigrate moving: " + fileName + " to: " + newFileName);
                File.Move(fileInfo.FullName, newFileName);
            }

            SafeHouse.Logger.Log("ScriptMigrate END");

            CheckForLegacyBoot();
        }

        private void BackupScripts()
        {
            if (Directory.Exists(backupFolder))
            {
                Directory.Delete(backupFolder);
            }
            Directory.CreateDirectory(backupFolder);

            var files = Directory.GetFiles(legacyArchiveFolder);

            foreach (var fileName in files)
            {
                var fileInfo = new FileInfo(fileName);
                var newFileName = backupFolder + Path.DirectorySeparatorChar + fileInfo.Name;
                SafeHouse.Logger.Log("copying: " + fileName + " to: " + newFileName);
                File.Copy(fileName, newFileName);
            }
        }

        private void MigrateBootAndRename()
        {
            SafeHouse.Logger.Log("MigrateBootAndRename START");
            string destDirectory = Path.Combine(SafeHouse.ArchiveFolder, "boot");
            if (!Directory.Exists(destDirectory))
                Directory.CreateDirectory(destDirectory);
            string sourceDirectory = SafeHouse.ArchiveFolder;

            var files = Directory.GetFiles(sourceDirectory, "boot*");
            foreach (var fileName in files)
            {
                var fileInfo = new FileInfo(fileName);
                string newFilename = fileInfo.Name;

                if (!newFilename.Equals("boot.ks", StringComparison.OrdinalIgnoreCase))
                {
                    newFilename = newFilename.Substring(4);
                    if (newFilename.StartsWith("_"))
                    {
                        newFilename = newFilename.Substring(1);
                    }
                }
                newFilename = Path.Combine(destDirectory, newFilename);
                if (backup)
                    File.Copy(fileName, newFilename);
                else
                    File.Move(fileName, newFilename);
            }
        }

        private void MigrateBootNoRename()
        {
            SafeHouse.Logger.Log("MigrateBootNoRename START");
            string destDirectory = Path.Combine(SafeHouse.ArchiveFolder, "boot");
            if (!Directory.Exists(destDirectory))
                Directory.CreateDirectory(destDirectory);
            string sourceDirectory = SafeHouse.ArchiveFolder;

            var files = Directory.GetFiles(sourceDirectory, "boot*");
            foreach (var fileName in files)
            {
                var fileInfo = new FileInfo(fileName);
                string newFilename = Path.Combine(destDirectory, fileInfo.Name);
                if (backup)
                    File.Copy(fileName, newFilename);
                else
                    File.Move(fileName, newFilename);
            }
        }
    }
}