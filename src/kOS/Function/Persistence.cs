using kOS.Safe.Encapsulation;
using kOS.Safe.Exceptions;
using kOS.Safe.Function;
using kOS.Safe.Persistence;
using kOS.Safe.Serialization;
using kOS.Safe.Utilities;
using kOS.Serialization;
using System;

namespace kOS.Function
{
    [Function("edit")]
    public class FunctionEdit : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            string fileName = PopValueAssert(shared, true).ToString();
            AssertArgBottomAndConsume(shared);

            if (shared.VolumeMgr != null)
            {
                Volume vol = shared.VolumeMgr.CurrentVolume;
                var volumeFile = vol.OpenOrCreate(fileName);
                shared.Window.OpenPopupEditor(vol, volumeFile.Name);
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

                    VolumeFile file = origin.Open(fileName);
                    if (file != null)
                    {
                        if (destination.Save(file.Name, file.ReadAll()) == null)
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

    [Function("writejson")]
    public class FunctionWriteJson : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            string fileName = PopValueAssert(shared, true).ToString();
            SerializableStructure serialized = PopValueAssert(shared, true) as SerializableStructure;
            AssertArgBottomAndConsume(shared);

            if (serialized == null)
            {
                throw new KOSException("This type is not serializable");
            }

            string serializedString = new SerializationMgr(shared).Serialize(serialized, JsonFormatter.WriterInstance);

            FileContent fileContent = new FileContent(serializedString);

            if (shared.VolumeMgr != null)
            {
                shared.VolumeMgr.CurrentVolume.Save(fileName, fileContent);
            }
        }
    }

    [Function("readjson")]
    public class FunctionReadJson : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            string fileName = PopValueAssert(shared, true).ToString();
            AssertArgBottomAndConsume(shared);

            VolumeFile volumeFile = shared.VolumeMgr.CurrentVolume.Open(fileName);

            if (volumeFile == null)
            {
                throw new KOSException("File does not exist: " + fileName);
            }

            object read = new SerializationMgr(shared).Deserialize(volumeFile.ReadAll().String, JsonFormatter.ReaderInstance);

            ReturnValue = read;
        }
    }
}