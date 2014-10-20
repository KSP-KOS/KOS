using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using FileInfo = kOS.Safe.Encapsulation.FileInfo;

namespace kOS.Persistence
{
    public class Archive : Volume
    {
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
                using (var infile = new BinaryReader(File.Open(ArchiveFolder + name + ".txt", FileMode.Open)))
                {


                    byte[] fileBody = ProcessBinaryReader(infile);

                    var retFile = new ProgramFile(name);
                    FileCategory whatKind = ProgramFile.IdentifyCategory(fileBody);
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

        public override bool SaveFile(ProgramFile file)
        {
            base.SaveFile(file);

            Directory.CreateDirectory(ArchiveFolder);

            try
            {
                using (var outfile = new BinaryWriter(File.Open(ArchiveFolder + file.Filename + ".txt",FileMode.Create)))
                {
                    
                    byte[] fileBody;
                    if (file.Category == FileCategory.KEXE)
                        fileBody = file.BinaryContent;
                    else
                    {
                        string tempString = file.StringContent;
                        if (Application.platform == RuntimePlatform.WindowsPlayer)
                        {
                            // Only evil windows gets evil windows line breaks, and only if this is some sort of ascii:
                            tempString = tempString.Replace("\n", "\r\n");
                        }
                        fileBody = System.Text.Encoding.UTF8.GetBytes(tempString.ToCharArray());
                    }

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
                base.DeleteByName(name);
                File.Delete(string.Format("{0}{1}.txt", ArchiveFolder, name));
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
                var sourcePath = string.Format("{0}{1}.txt", ArchiveFolder, name);
                var destinationPath = string.Format("{0}{1}.txt", ArchiveFolder, newName);

                File.Move(sourcePath,destinationPath);
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
                foreach (var file in Directory.GetFiles(ArchiveFolder, "*.txt"))
                {
                    var sysFileInfo = new System.IO.FileInfo(file);
                    var fileInfo = new FileInfo(sysFileInfo.Name.Substring(0, sysFileInfo.Name.Length - 4), (int)sysFileInfo.Length);

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
