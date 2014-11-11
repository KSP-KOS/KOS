using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using kOS.Safe.Persistence;
using UnityEngine;
using FileInfo = kOS.Safe.Encapsulation.FileInfo;

namespace kOS.Persistence
{
    public class Archive : Volume
    {
        public const string KERBOSCRIPT_EXTENSION = "ks";
        public const string KOS_MACHINELANGUAGE_EXTENSION = "ksm";
        public static string ArchiveFolder
        {
            get
            {
                return GameDatabase.Instance.PluginDataFolder + "/Ships/Script/";
            }
        }

        public Archive()
        {
            Directory.CreateDirectory(ArchiveFolder);
            Renameable = false;
            Name = "Archive";
        }

        public override bool IsRoomFor(ProgramFile newFile)
        {
            return true;
        }

        public override ProgramFile GetByName(string name)
        {
            try
            {
                Safe.Utilities.Debug.Logger.Log("Archive: Getting File By Name: " + name);
                var fileInfo = FileSearch(name);
                if (fileInfo == null)
                {
                    return null;
                }

                using (var infile = new BinaryReader(File.Open(fileInfo.FullName, FileMode.Open)))
                {
                    byte[] fileBody = ProcessBinaryReader(infile);

                    var retFile = new ProgramFile(name);
                    FileCategory whatKind = PersistenceUtilities.IdentifyCategory(fileBody);
                    if (whatKind == FileCategory.KEXE)
                        retFile.BinaryContent = fileBody;
                    else
                        retFile.StringContent = System.Text.Encoding.UTF8.GetString(fileBody);

                    if (retFile.Category == FileCategory.ASCII || retFile.Category == FileCategory.KERBOSCRIPT)
                        retFile.StringContent = retFile.StringContent.Replace("\r\n", "\n");

                    base.DeleteByName(name);
                    base.Add(retFile);

                    return retFile;
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return null;
            }
        }

        private byte[] ProcessBinaryReader(BinaryReader infile)
        {
            const int BUFFER_SIZE = 4096;
            using (var ms = new MemoryStream())
            {
                var buffer = new byte[BUFFER_SIZE];
                int count;
                while ((count = infile.Read(buffer, 0, buffer.Length)) != 0)
                    ms.Write(buffer, 0, count);
                return ms.ToArray();
            }
        }

        public virtual bool CheckRange(Vessel vessel)
        {
            return true;
        }

        public override bool SaveFile(ProgramFile file)
        {
            base.SaveFile(file);

            Directory.CreateDirectory(ArchiveFolder);

            try
            {
                Safe.Utilities.Debug.Logger.Log("Archive: Saving File Name: " + file.Filename);
                byte[] fileBody;
                string fileExtension;
                switch (file.Category)
                {
                    case FileCategory.UNKNOWN:
                    case FileCategory.ASCII:
                    case FileCategory.KERBOSCRIPT:
                        string tempString = file.StringContent;
                        if (Application.platform == RuntimePlatform.WindowsPlayer)
                        {
                            // Only evil windows gets evil windows line breaks, and only if this is some sort of ascii:
                            tempString = tempString.Replace("\n", "\r\n");
                        }
                        fileBody = System.Text.Encoding.UTF8.GetBytes(tempString.ToCharArray());
                        fileExtension = KERBOSCRIPT_EXTENSION;
                        break;
                    case FileCategory.KEXE:
                        fileBody = file.BinaryContent;
                        fileExtension = KOS_MACHINELANGUAGE_EXTENSION;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                var fileName = string.Format("{0}{1}.{2}", ArchiveFolder, file.Filename, fileExtension);
                using (var outfile = new BinaryWriter(File.Open(fileName, FileMode.Create)))
                {
                    outfile.Write(fileBody);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }

            return true;
        }

        public override bool DeleteByName(string name)
        {
            try
            {
                Safe.Utilities.Debug.Logger.Log("Archive: Deleting File Name: " + name);
                var fullPath = FileSearch(name);
                if (fullPath == null)
                {
                    return false;
                }

                base.DeleteByName(fullPath.FullName);
                File.Delete(fullPath.FullName);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private System.IO.FileInfo FileSearch(string name)
        {
            var path = ArchiveFolder + name;
            if (Path.HasExtension(path))
            {
                return File.Exists(path) ? new System.IO.FileInfo(path) : null;
            }
            var kerboscriptFile = new System.IO.FileInfo(string.Format("{0}.{1}", path, KERBOSCRIPT_EXTENSION));
            var kosMlFile = new System.IO.FileInfo(string.Format("{0}.{1}", path, KOS_MACHINELANGUAGE_EXTENSION));

            if (kerboscriptFile.Exists && kosMlFile.Exists)
            {
                return kerboscriptFile.LastWriteTime > kosMlFile.LastWriteTime
                    ? kerboscriptFile : kosMlFile;
            }
            if (kerboscriptFile.Exists)
            {
                return kerboscriptFile;
            }
            if (kosMlFile.Exists)
            {
                return kosMlFile;
            }
            return null;
        }

        public override bool RenameFile(string name, string newName)
        {
            try
            {
                Safe.Utilities.Debug.Logger.Log(string.Format("Archive: Renaming: {0} To: {1}", name, newName));
                var fullSourcePath = FileSearch(name);
                if (fullSourcePath == null)
                {
                    return false;
                }

                string destinationPath;
                if (Path.HasExtension(newName))
                {
                    destinationPath = string.Format(ArchiveFolder + newName);
                }
                else
                {
                    destinationPath = string.Format(ArchiveFolder + newName + fullSourcePath.Extension);
                }

                File.Move(fullSourcePath.FullName, destinationPath);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public override List<FileInfo> GetFileList()
        {
            var retList = new List<FileInfo>();

            try
            {
                Safe.Utilities.Debug.Logger.Log(string.Format("Archive: Listing Files"));
                foreach (var file in Directory.GetFiles(ArchiveFolder).Where(f=>f.EndsWith('.' +KERBOSCRIPT_EXTENSION) || f.EndsWith('.' + KOS_MACHINELANGUAGE_EXTENSION)))
                {
                    var sysFileInfo = new System.IO.FileInfo(file);
                    var fileInfo = new FileInfo(sysFileInfo);

                    retList.Add(fileInfo);
                }
            }
            catch (DirectoryNotFoundException)
            {
            }

            return retList;
        }

        public override float RequiredPower()
        {
            const int MULTIPLIER = 5;
            const float POWER_REQUIRED = BASE_POWER * MULTIPLIER;

            return POWER_REQUIRED;
        }
    }
}
