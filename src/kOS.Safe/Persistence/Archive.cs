using kOS.Safe.Encapsulation;
using kOS.Safe.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using kOS.Safe.Exceptions;

namespace kOS.Safe.Persistence
{
    [kOS.Safe.Utilities.KOSNomenclature("Archive")]
    public class Archive : Volume
    {
        public const string ArchiveName = "Archive";
        public ArchiveDirectory RootArchiveDirectory { get; private set; }

        private static string ArchiveFolder { get; set; }

        public override VolumeDirectory Root {
            get {
                return RootArchiveDirectory;
            }
        }

        public Archive(string archiveFolder)
        {
            ArchiveFolder = Path.GetFullPath(archiveFolder).TrimEnd(VolumePath.PathSeparator);
            CreateArchiveDirectory();
            Renameable = false;
            InitializeName(ArchiveName);

            RootArchiveDirectory = new ArchiveDirectory(this, VolumePath.EMPTY);
        }

        private void CreateArchiveDirectory()
        {
            Directory.CreateDirectory(ArchiveFolder);
        }

        public string GetArchivePath(VolumePath path)
        {
            if (path.PointsOutside)
            {
                throw new KOSInvalidPathException("Path refers to parent directory", path.ToString());
            }

            string mergedPath = ArchiveFolder;

            foreach (string segment in path.Segments)
            {
                mergedPath = Path.Combine(mergedPath, segment);
            }

            string fullPath = Path.GetFullPath(mergedPath);

            if (!fullPath.StartsWith(ArchiveFolder, StringComparison.Ordinal))
            {
                throw new KOSInvalidPathException("Path refers to parent directory", path.ToString());
            }

            return fullPath;
        }

        public override void Clear()
        {
            if (Directory.Exists(ArchiveFolder))
            {
                Directory.Delete(ArchiveFolder, true);
            }

            Directory.CreateDirectory(ArchiveFolder);
        }

        public override VolumeItem Open(VolumePath path, bool ksmDefault = false)
        {
            try
            {
                var fileSystemInfo = Search(path, ksmDefault);

                if (fileSystemInfo == null) {
                    return null;
                }
                else if (fileSystemInfo is FileInfo)
                {
                    VolumePath filePath = VolumePath.FromString(fileSystemInfo.FullName.Substring(ArchiveFolder.Length).Replace(Path.DirectorySeparatorChar, VolumePath.PathSeparator));
                    return new ArchiveFile(this, fileSystemInfo as FileInfo, filePath);
                }
                else {
                    // we can use 'path' here, default extensions are not added to directories
                    return new ArchiveDirectory(this, path);
                }
            }
            catch (Exception e)
            {
                throw new KOSPersistenceException("Could not open path: " + path, e);
            }
        }

        public override VolumeDirectory CreateDirectory(VolumePath path)
        {
            string archivePath = GetArchivePath(path);

            if (Directory.Exists(archivePath))
            {
                throw new KOSPersistenceException("Already exists: " + path);
            }

            try
            {
                Directory.CreateDirectory(archivePath);
            }
            catch (IOException)
            {
                throw new KOSPersistenceException("Could not create directory: " + path);
            }

            return new ArchiveDirectory(this, path);
        }

        public override VolumeFile CreateFile(VolumePath path)
        {
            if (path.Depth == 0)
            {
                throw new KOSPersistenceException("Can't create a file over root directory");
            }

            string archivePath = GetArchivePath(path);

            if (File.Exists(archivePath))
            {
                throw new KOSPersistenceException("Already exists: " + path);
            }

            try
            {
                Directory.CreateDirectory(GetArchivePath(path.GetParent()));
            }
            catch (IOException)
            {
                throw new KOSPersistenceException("Parent directory for path does not exist: " + path.ToString());
            }

            try
            {
                File.Create(archivePath).Dispose();
            }
            catch (UnauthorizedAccessException)
            {
                throw new KOSPersistenceException("Could not create file: " + path);
            }

            return Open(path) as VolumeFile;
        }

        public override bool Exists(VolumePath path, bool ksmDefault = false)
        {
            return Search(path, ksmDefault) != null;
        }

        public override bool Delete(VolumePath path, bool ksmDefault = false)
        {
            if (path.Depth == 0)
            {
                throw new KOSPersistenceException("Can't delete root directory");
            }

            var fileSystemInfo = Search(path, ksmDefault);

            if (fileSystemInfo == null)
            {
                return false;
            }
            else if (fileSystemInfo is FileInfo)
            {
                File.Delete(fileSystemInfo.FullName);
            }
            else
            {
                Directory.Delete(fileSystemInfo.FullName, true);
            }

            return true;
        }

        public override VolumeFile SaveFile(VolumePath path, FileContent content, bool verifyFreeSpace = true)
        {
            Directory.CreateDirectory(ArchiveFolder);

            string archivePath = GetArchivePath(path);
            if (Directory.Exists(archivePath))
            {
                throw new KOSPersistenceException("Can't save file over a directory: " + path);
            }

            string parentPath = Directory.GetParent(archivePath).FullName;

            if (!Directory.Exists(parentPath))
            {
                Directory.CreateDirectory(parentPath);
            }

            byte[] fileBody = ConvertToWindowsNewlines(content.Bytes);

            using (var outfile = new BinaryWriter(File.Open(archivePath, FileMode.Create)))
            {
                outfile.Write(fileBody);
            }

            return Open(path) as VolumeFile;
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

        public override float RequiredPower()
        {
            const int MULTIPLIER = 5;
            const float POWER_REQUIRED = BASE_POWER * MULTIPLIER;

            return POWER_REQUIRED;
        }

        /// <summary>
        /// Get the file from the OS.
        /// </summary>
        /// <param name="name">filename to look for</param>
        /// <param name="ksmDefault">if true, it prefers to use the KSM filename over the KS.  The default is to prefer KS.</param>
        /// <returns>the full fileinfo of the filename if found</returns>
        private FileSystemInfo Search(VolumePath volumePath, bool ksmDefault)
        {
            var path = GetArchivePath(volumePath);

            if (Directory.Exists(path))
            {
                return new DirectoryInfo(path);
            }

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