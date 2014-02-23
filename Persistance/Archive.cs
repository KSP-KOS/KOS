using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using kOS.Debug;
using kOS.RemoteTech2;
using kOS.Utilities;
using BinaryReader = KSP.IO.BinaryReader;

namespace kOS.Persistance
{
    public class Archive : Volume
    {
        private readonly Vessel vessel;
        public string ArchiveFolder = GameDatabase.Instance.PluginDataFolder + "/Plugins/PluginData/Archive/";

        public Archive(Vessel vessel)
        {
            this.vessel = vessel;

            Renameable = false;
            Name = "Archive";

            LoadAll();
        }

        public override bool IsRoomFor(File newFile)
        {
            return true;
        }

        private void LoadAll()
        {
            Directory.CreateDirectory(ArchiveFolder);

            // Attempt to migrate files from old archive drive
            if (!KSP.IO.File.Exists<File>(HighLogic.fetch.GameSaveFolder + "/arc")) return;
            var reader = BinaryReader.CreateForType<File>(HighLogic.fetch.GameSaveFolder + "/arc");

            int fileCount = reader.ReadInt32();

            for (var i = 0; i < fileCount; i++)
            {
                try
                {
                    string filename = reader.ReadString();
                    string body = reader.ReadString();

                    var file = new File(filename);
                    file.Deserialize(body);

                    Files.Add(file);
                    SaveFile(file);
                }
                catch (EndOfStreamException)
                {
                    break;
                }
            }

            reader.Close();

            KSP.IO.File.Delete<File>(HighLogic.fetch.GameSaveFolder + "/arc");
        }

        public override File GetByName(string name)
        {
            try
            {
                using (var infile = new StreamReader(ArchiveFolder + name + ".txt", true))
                {
                    var fileBody = infile.ReadToEnd().Replace("\r\n", "\n");

                    var retFile = new File(name);
                    retFile.Deserialize(fileBody);

                    base.DeleteByName(name);
                    Files.Add(retFile);

                    return retFile;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        public override bool SaveFile(File file)
        {
            base.SaveFile(file);

            if (!CheckRange())
            {
                throw new KOSException("Volume is out of range.");
            }

            Directory.CreateDirectory(ArchiveFolder);

            try
            {
                using (var outfile = new StreamWriter(ArchiveFolder + file.Filename + ".txt", false))
                {
                    var fileBody = file.Serialize();

                    if (Application.platform == RuntimePlatform.WindowsPlayer)
                    {
                        // Only evil windows gets evil windows line breaks
                        fileBody = fileBody.Replace("\n", "\r\n");
                    }

                    outfile.Write(fileBody);
                }
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        public override void DeleteByName(string name)
        {
            System.IO.File.Delete(ArchiveFolder + name + ".txt");
        }

        public override IList<FileInfo> GetFileList()
        {
            var retList = new List<FileInfo>();

            try
            {
                foreach (var file in Directory.GetFiles(ArchiveFolder, "*.txt"))
                {
                    var sysFileInfo = new System.IO.FileInfo(file);
                    var fileInfo = new FileInfo(sysFileInfo.Name.Substring(0, sysFileInfo.Name.Length - 4),
                                                (int) sysFileInfo.Length);

                    retList.Add(fileInfo);
                }
            }
            catch (DirectoryNotFoundException)
            {
            }

            return retList;
        }

        public override bool CheckRange()
        {
            if (RemoteTechHook.Instance != null)
                return RemoteTechHook.Instance.HasConnectionToKSC(vessel.id);
            else
                return (VesselUtils.GetDistanceToHome(vessel) < VesselUtils.GetCommRange(vessel));
        }

        public override float RequiredPower()
        {
            const int multiplier = 5;
            const float powerRequired = BASE_POWER * multiplier;

            return powerRequired;
        }
    }
}