using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using kOS.Safe.Utilities;
using FileInfo = kOS.Safe.Encapsulation.FileInfo;

namespace kOS.Safe.Persistence
{
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

        public override bool IsRoomFor(ProgramFile newFile)
        {
            return true;
        }

        /// <summary>
        /// Get a file given its name
        /// </summary>
        /// <param name="name">filename to get.  if it has no filename extension, one will be guessed at, ".ks" usually.</param>
        /// <param name="ksmDefault">true if a filename of .ksm is preferred in contexts where the extension was left off.  The default is to prefer .ks</param>
        /// <returns>the file</returns>
        public override ProgramFile GetByName(string name, bool ksmDefault = false)
        {
            try
            {
                SafeHouse.Logger.Log("Archive: Getting File By Name: " + name);
                var fileInfo = FileSearch(name, ksmDefault);
                if (fileInfo == null)
                {
                    return null;
                }

                using (var infile = new BinaryReader(File.Open(fileInfo.FullName, FileMode.Open)))
                {
                    byte[] fileBody = ProcessBinaryReader(infile);

                    var retFile = new ProgramFile(fileInfo.Name);
                    FileCategory whatKind = PersistenceUtilities.IdentifyCategory(fileBody);
                    if (whatKind == FileCategory.KSM)
                        retFile.BinaryContent = fileBody;
                    else
                        retFile.StringContent = System.Text.Encoding.UTF8.GetString(fileBody);

                    if (retFile.Category == FileCategory.ASCII || retFile.Category == FileCategory.KERBOSCRIPT)
                        retFile.StringContent = retFile.StringContent.Replace("\r\n", "\n");

                    base.Add(retFile, true);

                    return retFile;
                }
            }
            catch (Exception e)
            {
                SafeHouse.Logger.Log(e);
                return null;
            }
        }

        public override bool SaveFile(ProgramFile file)
        {
            base.SaveFile(file);

            Directory.CreateDirectory(ArchiveFolder);

            try
            {
                SafeHouse.Logger.Log("Archive: Saving File Name: " + file.Filename);
                byte[] fileBody;
                string fileExtension;
                switch (file.Category)
                {
                    case FileCategory.OTHER:
                    case FileCategory.TOOSHORT:
                    case FileCategory.ASCII:
                    case FileCategory.KERBOSCRIPT:
                        string tempString = file.StringContent;
                        if (SafeHouse.IsWindows)
                        {
                            // Only evil windows gets evil windows line breaks, and only if this is some sort of ASCII:
                            tempString = tempString.Replace("\n", "\r\n");
                        }
                        fileBody = System.Text.Encoding.UTF8.GetBytes(tempString.ToCharArray());
                        fileExtension = KERBOSCRIPT_EXTENSION;
                        break;
                    case FileCategory.KSM:
                        fileBody = file.BinaryContent;
                        fileExtension = KOS_MACHINELANGUAGE_EXTENSION;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                var fileName = string.Format("{0}{1}", ArchiveFolder, PersistenceUtilities.CookedFilename(file.Filename, fileExtension, true));
                using (var outfile = new BinaryWriter(File.Open(fileName, FileMode.Create)))
                {
                    outfile.Write(fileBody);
                }
            }
            catch (Exception e)
            {
                SafeHouse.Logger.Log(e);
                return false;
            }

            return true;
        }

        public override bool DeleteByName(string name)
        {
            try
            {
                SafeHouse.Logger.Log("Archive: Deleting File Name: " + name);
                var fullPath = FileSearch(name);
                if (fullPath == null)
                {
                    return false;
                }
                base.DeleteByName(name);
                File.Delete(fullPath.FullName);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }


        public override bool RenameFile(string name, string newName)
        {
            try
            {
                SafeHouse.Logger.Log(string.Format("Archive: Renaming: {0} To: {1}", name, newName));
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

        public override List<FileInfo> GetFileList()
        {
            var retList = new List<FileInfo>();

            try
            {
                SafeHouse.Logger.Log(string.Format("Archive: Listing Files"));
                var kosFiles = Directory.GetFiles(ArchiveFolder);
                retList.AddRange(kosFiles.Select(file => new System.IO.FileInfo(file)).Select(sysFileInfo => new FileInfo(sysFileInfo)));
            }
            catch (DirectoryNotFoundException)
            {
            }

            return retList;
        }

        public override float RequiredPower()
        {
            const int MULTIPLIER = 5;
            const float POWER_REQUIRED = BASE_POWER*MULTIPLIER;

            return POWER_REQUIRED;
        }

        /// <summary>
        /// Get the file from the OS.
        /// </summary>
        /// <param name="name">filename to look for</param>
        /// <param name="ksmDefault">if true, it prefers to use the KSM filename over the KS.  The default is to prefer KS.</param>
        /// <returns>the full fileinfo of the filename if found</returns>
        private System.IO.FileInfo FileSearch(string name, bool ksmDefault = false)
        {
            var path = ArchiveFolder + name;
            if (Path.HasExtension(path))
            {
                return File.Exists(path) ? new System.IO.FileInfo(path) : null;
            }
            var kerboscriptFile = new System.IO.FileInfo(PersistenceUtilities.CookedFilename(path, KERBOSCRIPT_EXTENSION, true));
            var kosMlFile = new System.IO.FileInfo(PersistenceUtilities.CookedFilename(path, KOS_MACHINELANGUAGE_EXTENSION, true));

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
        
        public override void AppendToFile(string name, string textToAppend)
        {
            SafeHouse.Logger.SuperVerbose("Archive: AppendToFile: " + name);
            System.IO.FileInfo info = FileSearch(name);

            string fullPath = info == null ? string.Format("{0}{1}", ArchiveFolder, PersistenceUtilities.CookedFilename(name, KERBOSCRIPT_EXTENSION, true)) : info.FullName;

            // Using binary writer so we can bypass the OS behavior about ASCII end-of-lines and always use \n's no matter the OS:
            // Deliberately not catching potential I/O exceptions from this, so they will percolate upward and be seen by the user:
            using (var outfile = new BinaryWriter(File.Open(fullPath, FileMode.Append, FileAccess.Write,FileShare.ReadWrite)))
            {
                byte[] binaryLine = System.Text.Encoding.UTF8.GetBytes((textToAppend+"\n").ToCharArray());
                outfile.Write(binaryLine);
            }
        }

        public override void AppendToFile(string name, byte[] bytesToAppend)
        {
            SafeHouse.Logger.SuperVerbose("Archive: AppendToFile: " + name);
            System.IO.FileInfo info = FileSearch(name);

            string fullPath = info == null ? string.Format("{0}{1}", ArchiveFolder, PersistenceUtilities.CookedFilename(name, KERBOSCRIPT_EXTENSION, true)) : info.FullName;

            // Deliberately not catching potential I/O exceptions from this, so they will percolate upward and be seen by the user:
            using (var outfile = new BinaryWriter(File.Open(fullPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite)))
            {
                outfile.Write(bytesToAppend);
            }
        }
    }
}
