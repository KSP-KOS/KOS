using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace kOS.Persistence
{
    public class Archive : Volume
    {
        private readonly string archiveFolder = GameDatabase.Instance.PluginDataFolder + "/Plugins/PluginData/Archive/";

        public Archive()
        {
            Directory.CreateDirectory(archiveFolder);
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
                using (var infile = new StreamReader(archiveFolder + name + ".txt", true))
                {
                    string fileBody = infile.ReadToEnd().Replace("\r\n", "\n");

                    var retFile = new ProgramFile(name) {Content = fileBody};
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

        public override bool SaveFile(ProgramFile file)
        {
            base.SaveFile(file);

            Directory.CreateDirectory(archiveFolder);

            try
            {
                using (var outfile = new StreamWriter(archiveFolder + file.Filename + ".txt", false))
                {
                    string fileBody = file.Content;

                    if (Application.platform == RuntimePlatform.WindowsPlayer)
                    {
                        // Only evil windows gets evil windows line breaks
                        fileBody = fileBody.Replace("\n", "\r\n");
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
                File.Delete(archiveFolder + name + ".txt");
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
                foreach (var file in Directory.GetFiles(archiveFolder, "*.txt"))
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
