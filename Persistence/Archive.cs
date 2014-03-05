using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using UnityEngine;
using kOS.Utilities;
using kOS.AddOns.RemoteTech2;

namespace kOS.Persistence
{
    public class Archive : Volume
    {
        private string _archiveFolder = GameDatabase.Instance.PluginDataFolder + "/Plugins/PluginData/Archive/";

        public Archive()
        {
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
                using (StreamReader infile = new StreamReader(_archiveFolder + name + ".txt", true))
                {
                    string fileBody = infile.ReadToEnd().Replace("\r\n", "\n");

                    ProgramFile retFile = new ProgramFile(name);
                    retFile.Content = fileBody;
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

            Directory.CreateDirectory(_archiveFolder);

            try
            {
                using (StreamWriter outfile = new StreamWriter(_archiveFolder + file.Filename + ".txt", false))
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
                System.IO.File.Delete(_archiveFolder + name + ".txt");
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
                foreach (var file in Directory.GetFiles(_archiveFolder, "*.txt"))
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

        public override bool CheckRange(Vessel vessel)
        {
            if (vessel != null)
            {
                return (VesselUtils.GetDistanceToHome(vessel) < VesselUtils.GetCommRange(vessel));
            }
            else
            {
                return false;
            }
        }

        public override float RequiredPower()
        {
            const int multiplier = 5;
            const float powerRequired = BASE_POWER * multiplier;

            return powerRequired;
        }
    }
}
