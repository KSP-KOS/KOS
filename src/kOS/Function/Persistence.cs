using System;
using kOS.Safe.Function;
using kOS.Safe.Persistence;
using kOS.Safe.Utilities;
using KSP.IO;

namespace kOS.Function
{
    [Function("switch")]
    public class FunctionSwitch : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            object volumeId = shared.Cpu.PopValue(true);

            if (shared.VolumeMgr != null)
            {
                Volume volume = shared.VolumeMgr.GetVolume(volumeId);
                if (volume != null)
                {
                    shared.VolumeMgr.SwitchTo(volume);
                }
                else
                {
                    throw new Exception("Volume not found");
                }
            }
        }
    }
    
    [Function("edit")]
    public class FunctionEdit : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            string fileName = shared.Cpu.PopValue(true).ToString();

            // If no filename extension, then give it one:
            fileName = PersistenceUtilities.CookedFilename(fileName, Volume.KERBOSCRIPT_EXTENSION);
            
            if (shared.VolumeMgr != null)
            {
                Volume vol = shared.VolumeMgr.CurrentVolume;
                shared.Window.OpenPopupEditor( vol, fileName );
            }
        }
    }

    [Function("copy")]
    public class FunctionCopy : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            object volumeId = shared.Cpu.PopValue(true);
            string direction = shared.Cpu.PopValue().ToString();
            string fileName = shared.Cpu.PopValue(true).ToString();

            Debug.Logger.Log(string.Format("FunctionCopy: Volume: {0} Direction: {1} Filename: {2}", volumeId, direction, fileName));

            if (shared.VolumeMgr != null)
            {
                Volume origin;
                Volume destination;

                if (direction == "from")
                {
                    origin = shared.VolumeMgr.GetVolume(volumeId);
                    destination = shared.VolumeMgr.CurrentVolume;
                }
                else
                {
                    origin = shared.VolumeMgr.CurrentVolume;
                    destination = shared.VolumeMgr.GetVolume(volumeId);
                }

                if (origin != null && destination != null)
                {
                    if (origin == destination)
                    {
                        throw new Exception("Cannot copy from a volume to the same volume.");
                    }

                    ProgramFile file = origin.GetByName(fileName);
                    if (file != null)
                    {
                        if (!destination.SaveFile(new ProgramFile(file)))
                        {
                            throw new Exception("File copy failed");
                        }
                    }
                    else
                    {
                        throw new Exception(string.Format("File '{0}' not found", fileName));
                    }
                }
                else
                {
                    throw new Exception(string.Format("Volume {0} not found", volumeId));
                }
            }
        }
    }

    [Function("rename")]
    public class FunctionRename : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            string newName = shared.Cpu.PopValue(true).ToString();
            object oldName = shared.Cpu.PopValue(true);
            string objectToRename = shared.Cpu.PopValue().ToString();

            if (shared.VolumeMgr != null)
            {
                if (objectToRename == "file")
                {
                    Volume volume = shared.VolumeMgr.CurrentVolume;
                    if (volume != null)
                    {
                        if (volume.GetByName(newName) == null)
                        {
                            if (!volume.RenameFile(oldName.ToString(), newName))
                            {
                                throw new Exception(string.Format("File '{0}' not found", oldName));
                            }
                        }
                        else
                        {
                            throw new Exception(string.Format("File '{0}' already exists.", newName));
                        }
                    }
                    else
                    {
                        throw new Exception("Volume not found");
                    }
                }
                else
                {
                    Volume volume = shared.VolumeMgr.GetVolume(oldName);
                    if (volume != null)
                    {
                        if (volume.Renameable)
                        {
                            volume.Name = newName;
                        }
                        else
                        {
                            throw new Exception("Volume cannot be renamed");
                        }
                    }
                    else
                    {
                        throw new Exception("Volume not found");
                    }
                }
            }
        }
    }

    [Function("delete")]
    public class FunctionDelete : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            object volumeId = shared.Cpu.PopValue(true);
            string fileName = shared.Cpu.PopValue(true).ToString();

            if (shared.VolumeMgr != null)
            {
                Volume volume = volumeId != null ? shared.VolumeMgr.GetVolume(volumeId) : shared.VolumeMgr.CurrentVolume;

                if (volume != null)
                {
                    if (!volume.DeleteByName(fileName))
                    {
                        throw new Exception(string.Format("File '{0}' not found", fileName));
                    }
                }
                else
                {
                    throw new Exception("Volume not found");
                }
            }
        }
    }
}
