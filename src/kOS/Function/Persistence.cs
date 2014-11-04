﻿using System;
using kOS.Persistence;

namespace kOS.Function
{
    [FunctionAttribute("switch")]
    public class FunctionSwitch : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            object volumeId = shared.Cpu.PopValue();

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
    
    [FunctionAttribute("edit")]
    public class FunctionEdit : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            string fileName = shared.Cpu.PopValue().ToString();

            if (shared.VolumeMgr != null)
            {
                Volume vol = shared.VolumeMgr.CurrentVolume;
                shared.Window.OpenPopupEditor( vol, fileName );
            }
        }
    }

    [FunctionAttribute("copy")]
    public class FunctionCopy : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            object volumeId = shared.Cpu.PopValue();
            string direction = shared.Cpu.PopValue().ToString();
            string fileName = shared.Cpu.PopValue().ToString();

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

    [FunctionAttribute("rename")]
    public class FunctionRename : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            string newName = shared.Cpu.PopValue().ToString();
            object oldName = shared.Cpu.PopValue();
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

    [FunctionAttribute("delete")]
    public class FunctionDelete : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            object volumeId = shared.Cpu.PopValue();
            string fileName = shared.Cpu.PopValue().ToString();

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
