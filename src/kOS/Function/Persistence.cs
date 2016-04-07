using kOS.Safe.Encapsulation;
using kOS.Safe.Exceptions;
using kOS.Safe.Function;
using kOS.Safe.Persistence;
using kOS.Safe.Serialization;
using kOS.Safe.Utilities;
using kOS.Serialization;
using System;
using KSP.IO;
using kOS.Safe;
using kOS.Safe.Compilation;
using System.Collections.Generic;

namespace kOS.Function
{
    /*
     * A couple of syntaxes from kRISC.tpg were deprecated when subdirectories where introduced. It will be possible to
     * remove these function below as well any metions of delete/rename file/rename volume/copy from kRISC.tpg in the future.
     */
    [Function("copy_deprecated")]
    public class FunctionCopyDeprecated : FunctionWithCopy
    {
        public override void Execute(SharedObjects shared)
        {
            PopValueAssert(shared, true).ToString();
            PopValueAssert(shared, true).ToString();
            AssertArgBottomAndConsume(shared);

            throw new KOSDeprecationException("1.0.0", "`COPY FILENAME FROM VOLUMEID.` syntax", "`COPY(FROMPATH, TOPATH)`");
        }
    }

    [Function("rename_file_deprecated")]
    public class FunctionRenameFileDeprecated : FunctionWithCopy
    {
        public override void Execute(SharedObjects shared)
        {
            PopValueAssert(shared, true).ToString();
            PopValueAssert(shared, true).ToString();
            AssertArgBottomAndConsume(shared);

            throw new KOSDeprecationException("1.0.0", "`RENAME FILE OLDNAME TO NEWNAME.` syntax", "`MOVE(FROMPATH, TOPATH)`");
        }
    }

    [Function("rename_volume_deprecated")]
    public class FunctionRenameVolumeDeprecated : FunctionWithCopy
    {
        public override void Execute(SharedObjects shared)
        {
            PopValueAssert(shared, true).ToString();
            PopValueAssert(shared, true).ToString();
            AssertArgBottomAndConsume(shared);

            throw new KOSDeprecationException("1.0.0", "`RENAME VOLUME OLDNAME TO NEWNAME.` syntax", "`SET VOLUME:NAME TO NEWNAME.`");
        }
    }

    [Function("delete_deprecated")]
    public class FunctionDeleteDeprecated : FunctionWithCopy
    {
        public override void Execute(SharedObjects shared)
        {
            PopValueAssert(shared, true).ToString();
            PopValueAssert(shared, true).ToString();
            AssertArgBottomAndConsume(shared);

            throw new KOSDeprecationException("1.0.0", "`DELETE FILENAME FROM VOLUMEID.` syntax", "`DELETE(PATH)`");
        }
    }

    [Function("path")]
    public class FunctionPath : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            int remaining = CountRemainingArgs(shared);

            GlobalPath path;

            if (remaining == 0)
            {
                path = GlobalPath.FromVolumePath(shared.VolumeMgr.CurrentDirectory.Path, shared.VolumeMgr.CurrentVolume);
            } else
            {
                string pathString = PopValueAssert(shared, true).ToString();
                path = shared.VolumeMgr.GlobalPathFromString(pathString);
            }

            AssertArgBottomAndConsume(shared);

            ReturnValue = new PathValue(path, shared);
        }
    }

    [Function("volume")]
    public class FunctionVolume : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            int remaining = CountRemainingArgs(shared);

            Volume volume;

            if (remaining == 0)
            {
                volume = shared.VolumeMgr.CurrentVolume;
            } else
            {
                object volumeId = PopValueAssert(shared, true);
                volume = shared.VolumeMgr.GetVolume(volumeId);
            }

            AssertArgBottomAndConsume(shared);

            ReturnValue = volume;
        }
    }

    [Function("scriptpath")]
    public class FunctionScriptPath : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            AssertArgBottomAndConsume(shared);

            int currentOpcode = shared.Cpu.GetCallTrace()[0];
            Opcode opcode = shared.Cpu.GetOpcodeAt(currentOpcode);

            ReturnValue = new PathValue(opcode.SourcePath, shared);
        }
    }

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
            string pathString = PopValueAssert(shared, true).ToString();
            AssertArgBottomAndConsume(shared);

            GlobalPath path = shared.VolumeMgr.GlobalPathFromString(pathString);
            Volume vol = shared.VolumeMgr.GetVolumeFromPath(path);
            shared.Window.OpenPopupEditor(vol, path);

        }
    }

    [Function("cd")]
    public class FunctionCd : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            int remaining = CountRemainingArgs(shared);

            VolumeDirectory directory;

            if (remaining == 0)
            {
                directory = shared.VolumeMgr.CurrentVolume.Root;
            } else
            {
                string pathString = PopValueAssert(shared, true).ToString();

                GlobalPath path = shared.VolumeMgr.GlobalPathFromString(pathString);
                Volume volume = shared.VolumeMgr.GetVolumeFromPath(path);

                directory = volume.Open(path) as VolumeDirectory;

                if (directory == null)
                {
                    throw new KOSException("Invalid directory: " + pathString);
                }

            }

            AssertArgBottomAndConsume(shared);

            shared.VolumeMgr.CurrentDirectory = directory;
        }
    }

    public abstract class FunctionWithCopy : FunctionBase
    {
        protected void Copy(IVolumeManager volumeManager, GlobalPath sourcePath, GlobalPath destinationPath)
        {
            Volume sourceVolume = volumeManager.GetVolumeFromPath(sourcePath);
            Volume destinationVolume = volumeManager.GetVolumeFromPath(destinationPath);

            VolumeItem source = sourceVolume.Open(sourcePath);
            VolumeItem destination = destinationVolume.Open(destinationPath);

            if (source == null)
            {
                throw new KOSPersistenceException("Path does not exist: " + sourcePath);
            }

            if (source is VolumeDirectory)
            {
                if (destination is VolumeFile)
                {
                    throw new KOSPersistenceException("Can't copy directory into a file");
                }

                CopyDirectory(volumeManager, sourcePath, destinationPath);
            } else
            {
                if (destination is VolumeFile || destination == null)
                {
                    Volume targetVolume = volumeManager.GetVolumeFromPath(destinationPath);
                    CopyFile(source as VolumeFile, destinationPath, targetVolume);
                } else
                {
                    CopyFileToDirectory(source as VolumeFile, destination as VolumeDirectory);
                }
            }
        }

        protected void CopyDirectory(IVolumeManager volumeManager, GlobalPath sourcePath, GlobalPath destinationPath)
        {
            Volume sourceVolume = volumeManager.GetVolumeFromPath(sourcePath);
            Volume destinationVolume = volumeManager.GetVolumeFromPath(destinationPath);

            VolumeDirectory source = sourceVolume.Open(sourcePath) as VolumeDirectory;

            VolumeDirectory destination;

            if (destinationVolume.Exists(destinationPath))
            {
                destination = destinationVolume.CreateDirectory(destinationPath.Combine(sourcePath.Name));

                if (destination == null)
                {
                    throw new KOSException("Path was expected to point to a directory: " + destinationPath);
                }
            } else
            {
                destination = destinationVolume.CreateDirectory(destinationPath);
            }

            foreach (KeyValuePair<string, VolumeItem> pair in source.List())
            {
                if (pair.Value is VolumeDirectory)
                {
                    CopyDirectory(volumeManager, sourcePath.Combine(pair.Key), destinationPath.Combine(pair.Key));
                } else
                {
                    CopyFileToDirectory(pair.Value as VolumeFile, destination);
                }
            }
        }

        protected void CopyFile(VolumeFile volumeFile, GlobalPath destinationPath, Volume targetVolume)
        {
            if (targetVolume.Save(destinationPath, volumeFile.ReadAll()) == null)
            {
                throw new KOSPersistenceException("Couldn not copy file");
            }
        }

        protected void CopyFileToDirectory(VolumeFile volumeFile, VolumeDirectory volumeDirectory)
        {
            if (volumeDirectory.Volume.Save(volumeDirectory.Path.Combine(volumeFile.Name), volumeFile.ReadAll()) == null)
            {
                throw new KOSPersistenceException("Couldn not copy file");
            }
        }
    }

    [Function("copy")]
    public class FunctionCopy : FunctionWithCopy
    {
        public override void Execute(SharedObjects shared)
        {
            string destinationPathString = PopValueAssert(shared, true).ToString();
            string sourcePathString = PopValueAssert(shared, true).ToString();
            AssertArgBottomAndConsume(shared);

            SafeHouse.Logger.Log(string.Format("FunctionCopy: {0} {1}", sourcePathString, destinationPathString));

            GlobalPath sourcePath = shared.VolumeMgr.GlobalPathFromString(sourcePathString);
            GlobalPath destinationPath = shared.VolumeMgr.GlobalPathFromString(destinationPathString);

            Copy(shared.VolumeMgr, sourcePath, destinationPath);
        }
    }

    [Function("move")]
    public class FunctionMove : FunctionWithCopy
    {
        public override void Execute(SharedObjects shared)
        {
            string destinationPathString = PopValueAssert(shared, true).ToString();
            string sourcePathString = PopValueAssert(shared, true).ToString();
            AssertArgBottomAndConsume(shared);

            SafeHouse.Logger.Log(string.Format("FunctionCopy: {0} {1}", sourcePathString, destinationPathString));

            GlobalPath sourcePath = shared.VolumeMgr.GlobalPathFromString(sourcePathString);
            GlobalPath destinationPath = shared.VolumeMgr.GlobalPathFromString(destinationPathString);

            Copy(shared.VolumeMgr, sourcePath, destinationPath);

            Volume sourceVolume = shared.VolumeMgr.GetVolumeFromPath(sourcePath);
            sourceVolume.Delete(sourcePath);
        }
    }

    [Function("delete")]
    public class FunctionDelete : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            string pathString = PopValueAssert(shared, true).ToString();
            AssertArgBottomAndConsume(shared);

            GlobalPath path = shared.VolumeMgr.GlobalPathFromString(pathString);
            Volume volume = shared.VolumeMgr.GetVolumeFromPath(path);
            volume.Delete(path);

            if (!volume.Delete(path))
            {
                throw new Exception(string.Format("Could not remove '{0}'", path));
            }
        }
    }

    [Function("writejson")]
    public class FunctionWriteJson : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            string pathString = PopValueAssert(shared, true).ToString();
            SerializableStructure serialized = PopValueAssert(shared, true) as SerializableStructure;
            AssertArgBottomAndConsume(shared);

            if (serialized == null)
            {
                throw new KOSException("This type is not serializable");
            }

            string serializedString = new SerializationMgr(shared).Serialize(serialized, JsonFormatter.WriterInstance);

            FileContent fileContent = new FileContent(serializedString);

            GlobalPath path = shared.VolumeMgr.GlobalPathFromString(pathString);
            Volume volume = shared.VolumeMgr.GetVolumeFromPath(path);

            ReturnValue = volume.Save(path, fileContent);
        }
    }

    [Function("readjson")]
    public class FunctionReadJson : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            string pathString = PopValueAssert(shared, true).ToString();
            AssertArgBottomAndConsume(shared);

            GlobalPath path = shared.VolumeMgr.GlobalPathFromString(pathString);
            Volume volume = shared.VolumeMgr.GetVolumeFromPath(path);

            VolumeFile volumeFile = volume.Open(pathString) as VolumeFile;

            if (volumeFile == null)
            {
                throw new KOSException("File does not exist: " + path);
            }

            Structure read = new SerializationMgr(shared).Deserialize(volumeFile.ReadAll().String, JsonFormatter.ReaderInstance) as SerializableStructure;
            ReturnValue = read;
        }
    }

    [Function("exists")]
    public class FunctionExists : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            string pathString = PopValueAssert(shared, true).ToString();
            AssertArgBottomAndConsume(shared);

            GlobalPath path = shared.VolumeMgr.GlobalPathFromString(pathString);
            Volume volume = shared.VolumeMgr.GetVolumeFromPath(path);

            ReturnValue = volume.Exists(path);
        }
    }

    [Function("open")]
    public class FunctionOpen : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            string pathString = PopValueAssert(shared, true).ToString();
            AssertArgBottomAndConsume(shared);

            GlobalPath path = shared.VolumeMgr.GlobalPathFromString(pathString);
            Volume volume = shared.VolumeMgr.GetVolumeFromPath(path);

            VolumeItem volumeItem = volume.Open(path);

            if (volumeItem == null)
            {
                throw new KOSException("File or directory does not exist: " + path);
            }

            ReturnValue = volumeItem;
        }
    }

    [Function("create")]
    public class FunctionCreate : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            string pathString = PopValueAssert(shared, true).ToString();
            AssertArgBottomAndConsume(shared);

            GlobalPath path = shared.VolumeMgr.GlobalPathFromString(pathString);
            Volume volume = shared.VolumeMgr.GetVolumeFromPath(path);

            VolumeFile volumeFile = volume.CreateFile(path);

            ReturnValue = volumeFile;
        }
    }

    [Function("createdir")]
    public class FunctionCreateDirectory : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            string pathString = PopValueAssert(shared, true).ToString();
            AssertArgBottomAndConsume(shared);

            GlobalPath path = shared.VolumeMgr.GlobalPathFromString(pathString);
            Volume volume = shared.VolumeMgr.GetVolumeFromPath(path);

            VolumeDirectory volumeDirectory = volume.CreateDirectory(path);

            ReturnValue = volumeDirectory;
        }
    }
}