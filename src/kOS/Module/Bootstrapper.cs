using System;
using System.IO;
using kOS.Persistence;
using kOS.Safe.Persistence;
using kOS.Suffixed;
using UnityEngine;

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
        }

        private void BuildEnvironment()
        {
            Safe.Utilities.Environment.Init(
                Config.Instance, 
                Application.platform == RuntimePlatform.WindowsPlayer,
                GameDatabase.Instance.PluginDataFolder + "/Ships/Script/"
                );
        }

        private void BuildLogger()
        {
            if (Safe.Utilities.Debug.Logger != null) return;
            Debug.Log("kOS: Init Logger");
            Safe.Utilities.Debug.Logger = new KSPLogger();
        }

        private void CheckForLegacyArchive()
        {
            if (!Directory.Exists(legacyArchiveFolder))
            {
                return;
            }

            if (Directory.Exists(Safe.Utilities.Environment.ArchiveFolder))
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

            Safe.Utilities.Debug.Logger.Log("ScriptMigrate START");
            Directory.CreateDirectory(Safe.Utilities.Environment.ArchiveFolder);

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
                    newFileName = string.Format("{0}/{1}.{2}", Safe.Utilities.Environment.ArchiveFolder, bareFilename, NEW_EXTENSION);
                }
                else
                {
                    newFileName = Safe.Utilities.Environment.ArchiveFolder + Path.DirectorySeparatorChar + Path.GetFileName(fileName);
                }

                Safe.Utilities.Debug.Logger.Log("ScriptMigrate moving: " + fileName + " to: " + newFileName);
                File.Move(fileInfo.FullName, newFileName);
            }

            Safe.Utilities.Debug.Logger.Log("ScriptMigrate END");
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
                Safe.Utilities.Debug.Logger.Log("copying: " + fileName + " to: " + newFileName);
                File.Copy(fileName, newFileName);
            }
        }        
    }
}