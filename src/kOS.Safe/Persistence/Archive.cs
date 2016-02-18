using kOS.Safe.Encapsulation;
using kOS.Safe.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace kOS.Safe.Persistence
{
    [kOS.Safe.Utilities.KOSNomenclature("Archive")]
    public class Archive : Volume
    {
        private static string ArchiveFolder
        {
            get { return SafeHouse.ArchiveFolder; }
        }

        public Archive()
        {
            Directory.CreateDirectory(ArchiveFolder);
            Renameable = false;
            Name = "Archive";
        }

        public override float RequiredPower()
        {
            const int MULTIPLIER = 5;
            const float POWER_REQUIRED = BASE_POWER * MULTIPLIER;

            return POWER_REQUIRED;
        }

        public override VolumeFile Open(string name, bool ksmDefault = false)
        {
            try
            {
                var fileInfo = FileSearch(name, ksmDefault);
                if (fileInfo == null)
                {
                    return null;
                }

                return new ArchiveFile(fileInfo);
            }
            catch (Exception e)
            {
                SafeHouse.Logger.Log(e);
                return null;
            }
        }

        public override bool Delete(string name)
        {
            var fullPath = FileSearch(name);
            if (fullPath == null)
            {
                return false;
            }
            File.Delete(fullPath.FullName);
            return true;
        }

        public override bool RenameFile(string name, string newName)
        {
            try
            {
                var fullSourcePath = FileSearch(name);
                if (fullSourcePath == null)
                {
                    return false;
                }

                string destinationPath = string.Format(ArchiveFolder + newName);
                if (!Path.HasExtension(newName))
                {
                    destinationPath += fullSourcePath.Extension;
                }

                File.Move(fullSourcePath.FullName, destinationPath);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public override VolumeFile Create(string name)
        {
            string filePath = Path.Combine(ArchiveFolder, name);
            if (File.Exists(filePath))
            {
                throw new KOSFileException("File already exists: " + name);
            }

            using (File.Create(filePath))
            {
                // Do Nothing
            }

            return new ArchiveFile(FileSearch(name));
        }

        public override VolumeFile Save(string name, FileContent content)
        {
            Directory.CreateDirectory(ArchiveFolder);

            byte[] fileBody = ConvertToWindowsNewlines(content.Bytes);

            using (var outfile = new BinaryWriter(File.Open(Path.Combine(ArchiveFolder, name), FileMode.Create)))
            {
                outfile.Write(fileBody);
            }

            return new ArchiveFile(FileSearch(name));
        }

        public override Dictionary<string, VolumeFile> FileList
        {
            get
            {
                var listFiles = Directory.GetFiles(ArchiveFolder);
                var filterHid = listFiles.Where(f => (File.GetAttributes(f) & FileAttributes.Hidden) != 0);
                var filterSys = listFiles.Where(f => (File.GetAttributes(f) & FileAttributes.System) != 0);

                var visFiles = listFiles.Except(filterSys).Except(filterHid);
                var kosFiles = visFiles.Except(Directory.GetFiles(ArchiveFolder, ".*"));
                return kosFiles.Select(file => new FileInfo(file)).Select(sysFileInfo => new ArchiveFile(sysFileInfo)).
                    Cast<VolumeFile>().ToDictionary(f => f.Name, f => f);
            }
        }

        public override long Size
        {
            get
            {
                return FileList.Values.Sum(i => i.Size);
            }
        }

        public override bool Exists(string name)
        {
            return FileSearch(name) != null;
        }

        public static byte[] ConvertToWindowsNewlines(byte[] bytes)
        {
            FileCategory category = PersistenceUtilities.IdentifyCategory(bytes);

            if (SafeHouse.IsWindows && !PersistenceUtilities.IsBinary(category))
            {
                string asString = FileContent.DecodeString(bytes);
                // Only evil windows gets evil windows line breaks, and only if this is some sort of ASCII:
                asString = asString.Replace("\n", "\r\n");
                return FileContent.EncodeString(asString);
            }

            return bytes;
        }

        public static byte[] ConvertFromWindowsNewlines(byte[] bytes)
        {
            FileCategory category = PersistenceUtilities.IdentifyCategory(bytes);

            if (!PersistenceUtilities.IsBinary(category))
            {
                string asString = FileContent.DecodeString(bytes);
                // Only evil windows gets evil windows line breaks, and only if this is some sort of ASCII:
                asString = asString.Replace("\r\n", "\n");
                return FileContent.EncodeString(asString);
            }

            return bytes;
        }

        /// <summary>
        /// Get the file from the OS.
        /// </summary>
        /// <param name="name">filename to look for</param>
        /// <param name="ksmDefault">if true, it prefers to use the KSM filename over the KS.  The default is to prefer KS.</param>
        /// <returns>the full fileinfo of the filename if found</returns>
        private FileInfo FileSearch(string name, bool ksmDefault = false)
        {
            var path = Path.Combine(ArchiveFolder, name);
            if (File.Exists(path))
            {
                return new FileInfo(path);
            }
            var kerboscriptFile = new FileInfo(PersistenceUtilities.CookedFilename(path, KERBOSCRIPT_EXTENSION, true));
            var kosMlFile = new FileInfo(PersistenceUtilities.CookedFilename(path, KOS_MACHINELANGUAGE_EXTENSION, true));

            if (kerboscriptFile.Exists && kosMlFile.Exists)
            {
                return ksmDefault ? kosMlFile : kerboscriptFile;
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
    }
}