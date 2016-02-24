using System;
using kOS.Safe.Encapsulation;
using kOS.Safe.Exceptions;
using kOS.Safe.Function;
using kOS.Safe.Persistence;
using kOS.Safe.Serialization;
using kOS.Safe.Utilities;
using kOS.Serialization;

namespace kOS.Function
{
    [Function("switch")]
    public class FunctionSwitch : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            object volumeId = PopValueAssert(shared, true);
            AssertArgBottomAndConsume(shared);

            if (shared.VolumeMgr != null)
            {
                Volume volume = volumeId is Volume ? volumeId as Volume : shared.VolumeMgr.GetVolume(volumeId);
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
            string fileName = PopValueAssert(shared, true).ToString();
            AssertArgBottomAndConsume(shared);

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
            object volumeId = PopValueAssert(shared, true);
            string direction = PopValueAssert(shared).ToString();
            string fileName = PopValueAssert(shared, true).ToString();
            AssertArgBottomAndConsume(shared);

            SafeHouse.Logger.Log(string.Format("FunctionCopy: Volume: {0} Direction: {1} Filename: {2}", volumeId, direction, fileName));

            if (shared.VolumeMgr != null)
            {
                Volume origin;
                Volume destination;

                if (direction == "from")
                {
                    origin = volumeId is Volume ? volumeId as Volume : shared.VolumeMgr.GetVolume(volumeId);
                    destination = shared.VolumeMgr.CurrentVolume;
                }
                else
                {
                    origin = shared.VolumeMgr.CurrentVolume;
                    destination = volumeId is Volume ? volumeId as Volume : shared.VolumeMgr.GetVolume(volumeId);
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
            string newName = PopValueAssert(shared, true).ToString();
            // old file name or, when we're renaming a volume, the old volume name or Volume instance
            object volumeIdOrOldName = PopValueAssert(shared, true);
            string objectToRename = PopValueAssert(shared).ToString();
            AssertArgBottomAndConsume(shared);

            if (shared.VolumeMgr != null)
            {
                if (objectToRename == "file")
                {
                    Volume volume = shared.VolumeMgr.CurrentVolume;
                    if (volume != null)
                    {
                        if (volume.GetByName(newName) == null)
                        {
                            if (!volume.RenameFile(volumeIdOrOldName.ToString(), newName))
                            {
                                throw new Exception(string.Format("File '{0}' not found", volumeIdOrOldName));
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
                    Volume volume = volumeIdOrOldName is Volume ? volumeIdOrOldName as Volume : shared.VolumeMgr.GetVolume(volumeIdOrOldName);
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
            object volumeId = PopValueAssert(shared, true);
            string fileName = PopValueAssert(shared, true).ToString();
            AssertArgBottomAndConsume(shared);

            if (shared.VolumeMgr != null)
            {
                Volume volume = volumeId != null ? (volumeId is Volume ? volumeId as Volume : shared.VolumeMgr.GetVolume(volumeId)) : shared.VolumeMgr.CurrentVolume;

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

    [Function("writejson")]
    public class FunctionWriteFile : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            string fileName = PopValueAssert(shared, true).ToString();
            IDumper serialized = PopValueAssert(shared, true) as IDumper;
            AssertArgBottomAndConsume(shared);

            if (serialized == null)
            {
                throw new KOSException("This type is not serializable");
            }

            string serializedString = new SerializationMgr(shared).Serialize(serialized, JsonFormatter.WriterInstance);

            ProgramFile programFile = new ProgramFile(fileName)
            {
                StringContent = serializedString
            };

            if (shared.VolumeMgr != null)
            {
                shared.VolumeMgr.CurrentVolume.SaveFile(programFile);
            }
        }
    }

    [Function("readjson")]
    public class FunctionReadFile : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            string fileName = PopValueAssert(shared, true).ToString();
            AssertArgBottomAndConsume(shared);

            ProgramFile programFile = shared.VolumeMgr.CurrentVolume.GetByName(fileName);

            if (programFile == null)
            {
                throw new KOSException("File does not exist: " + fileName);
            }

            object read = new SerializationMgr(shared).Deserialize(programFile.StringContent, JsonFormatter.ReaderInstance);

            ReturnValue = read;
        }
    }
}
