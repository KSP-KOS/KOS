using System;
using System.IO;
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
        private readonly string legacyArchiveFolder = GameDatabase.Instance.PluginDataFolder + "/Plugins/PluginData/Archive/";
        private const string LEGACY_KOS_EXTENSION = ".txt";
        private readonly string backupFolder = GameDatabase.Instance.PluginDataFolder + "/GameData/kOS/Backup_" + DateTime.Now.ToFileTimeUtc();

        private bool backup = true;
        
        public void Start()
        {
            BuildEnvironment();
            BuildLogger();

            CheckForLegacyArchive();

            KOSNomenclature.PopulateMapping(typeof(kOS.Safe.Encapsulation.Structure).Assembly, this.GetType().Assembly);
        }

        private void BuildEnvironment()
        {
            SafeHouse.Init(
                Config.Instance, 
                Core.VersionInfo,
                "http://ksp-kos.github.io/KOS_DOC/",
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
                return;
            }

            if (Directory.Exists(SafeHouse.ArchiveFolder))
            {
                return;
            }

            PopupDialog.SpawnPopupDialog(
                new MultiOptionDialog(
                    "The kOS v0.15 update has moved the archive folder to /Ships/Script/ and changed the file extension from *.txt to *.ks to be more in line with squad's current folder structure. Would you like us to attempt to migrate your existing scripts?",
                    () => backup = GUILayout.Toggle(backup, "Backup My scripts first"),
                    "kOS",
                    HighLogic.Skin, 
                    new DialogOption("Yes, Do it!", MigrateScripts, true),
                    new DialogOption("No, I'll do it myself", () => { }, true)
                    ),
                true,
                HighLogic.Skin
                );
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
                    newFileName = string.Format("{0}/{1}.{2}", Safe.Utilities.SafeHouse.ArchiveFolder, bareFilename, NEW_EXTENSION);
                }
                else
                {
                    newFileName = Safe.Utilities.SafeHouse.ArchiveFolder + Path.DirectorySeparatorChar + Path.GetFileName(fileName);
                }

                SafeHouse.Logger.Log("ScriptMigrate moving: " + fileName + " to: " + newFileName);
                File.Move(fileInfo.FullName, newFileName);
            }

            SafeHouse.Logger.Log("ScriptMigrate END");
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
    }
}