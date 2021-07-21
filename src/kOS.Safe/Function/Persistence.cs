using kOS.Safe.Compilation;
using kOS.Safe.Encapsulation;
using kOS.Safe.Exceptions;
using kOS.Safe.Persistence;
using kOS.Safe.Serialization;
using System;

namespace kOS.Safe.Function
{
    /*
     * A couple of syntaxes from kRISC.tpg were deprecated when subdirectories where introduced. It will be possible to
     * remove these function below as well any metions of delete/rename file/rename volume/copy from kRISC.tpg in the future.
     */

    [Function("copy_deprecated")]
    public class FunctionCopyDeprecated : SafeFunctionBase
    {
        public override void Execute(SafeSharedObjects shared)
        {
            object arg3 = PopValueAssert(shared, true);
            object arg2 = PopValueAssert(shared, true);
            object arg1 = PopValueAssert(shared, true);
            AssertArgBottomAndConsume(shared);

            string fromName, toName;

            if (arg2.ToString() == "from")
            {
                fromName = arg3.ToString() + ":/" + arg1.ToString();
                toName = "";
            }
            else
            {
                fromName = arg1.ToString();
                toName = arg3.ToString() + ":/" + fromName;
            }

            shared.Logger.LogWarningAndScreen(
                string.Format("WARNING: COPY {0} {1} {2} is deprecated as of kOS v1.0.0.  Use COPYPATH(\"{3}\", \"{4}\") instead.",
                              arg1.ToString(), arg2.ToString(), arg3.ToString(), fromName, toName));

            // Redirect into a call to the copypath function, so as to keep all
            // the copy file logic there in one unified location.  This is slightly slow,
            // but we don't care because this is just to support deprecation:
            shared.Cpu.PushArgumentStack(new kOS.Safe.Execution.KOSArgMarkerType());
            shared.Cpu.PushArgumentStack(fromName);
            shared.Cpu.PushArgumentStack(toName);
            shared.Cpu.CallBuiltinFunction("copypath");
        }
    }

    [Function("rename_file_deprecated")]
    public class FunctionRenameFileDeprecated : SafeFunctionBase
    {
        public override void Execute(SafeSharedObjects shared)
        {
            object newArg = PopValueAssert(shared, true);
            object oldArg = PopValueAssert(shared, true);
            PopValueAssert(shared, true); // This gets ignored because we already know its "file".

            string newName = newArg.ToString();
            string oldName = oldArg.ToString();
            AssertArgBottomAndConsume(shared);

            shared.Logger.LogWarningAndScreen(
                string.Format("WARNING: RENAME FILE {0} TO {1} is deprecated as of kOS v1.0.0.  Use MOVEPATH(\"{2}\", \"{3}\") instead.",
                              oldName, newName, oldName, newName));

            // Redirect into a call to the movepath function, so as to keep all
            // the file logic there in one unified location.  This is slightly slow,
            // but we don't care because this is just to support deprecation:
            shared.Cpu.PushArgumentStack(new kOS.Safe.Execution.KOSArgMarkerType());
            shared.Cpu.PushArgumentStack(oldName);
            shared.Cpu.PushArgumentStack(newName);
            shared.Cpu.CallBuiltinFunction("movepath");
        }
    }

    [Function("rename_volume_deprecated")]
    public class FunctionRenameVolumeDeprecated : SafeFunctionBase
    {
        public override void Execute(SafeSharedObjects shared)
        {
            object newArg = PopValueAssert(shared, true);
            object oldArg = PopValueAssert(shared, true);
            PopValueAssert(shared, true); // This gets ignored because we already know its "volume".

            string newName = newArg.ToString();
            string oldName = oldArg.ToString();
            AssertArgBottomAndConsume(shared);

            Volume volume = oldArg is Volume ? oldArg as Volume : shared.VolumeMgr.GetVolume(oldName);

            shared.Logger.LogWarningAndScreen(
                string.Format("WARNING: RENAME VOLUME {0} TO {1} is deprecated as of kOS v1.0.0.  Use SET VOLUME({2}):NAME TO \"{3}\" instead.",
                              oldName, newName, volume.Name, newName));

            volume.Name = newName;
        }
    }

    [Function("delete_deprecated")]
    public class FunctionDeleteDeprecated : SafeFunctionBase
    {
        public override void Execute(SafeSharedObjects shared)
        {
            object volumeId = PopValueAssert(shared, true);
            object fileName = PopValueAssert(shared, true);
            AssertArgBottomAndConsume(shared);

            string pathName;
            if (volumeId != null)
                pathName = volumeId.ToString() + ":/" + fileName.ToString();
            else
                pathName = fileName.ToString();

            shared.Logger.LogWarningAndScreen(
                string.Format("WARNING: DELETE {0}{1} is deprecated as of kOS v1.0.0.  Use DELETEPATH(\"{2}\") instead.",
                              fileName.ToString(), (volumeId == null ? "" : (" FROM " + volumeId.ToString())), pathName));

            // Redirect into a call to the deletepath function, so as to keep all
            // the file logic there in one unified location.  This is slightly slow,
            // but we don't care because this is just to support deprecation:
            shared.Cpu.PushArgumentStack(new kOS.Safe.Execution.KOSArgMarkerType());
            shared.Cpu.PushArgumentStack(pathName);
            shared.Cpu.CallBuiltinFunction("deletepath");
        }
    }

    [Function("scriptpath")]
    public class FunctionScriptPath : SafeFunctionBase
    {
        public override void Execute(SafeSharedObjects shared)
        {
            AssertArgBottomAndConsume(shared);

            int currentOpcode = shared.Cpu.GetCallTrace()[0];
            Opcode opcode = shared.Cpu.GetOpcodeAt(currentOpcode);

            ReturnValue = new PathValue(opcode.SourcePath, shared);
        }
    }

    [Function("switch")]
    public class FunctionSwitch : SafeFunctionBase
    {
        public override void Execute(SafeSharedObjects shared)
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

    [Function("cd", "chdir")]
    public class FunctionCd : SafeFunctionBase
    {
        public override void Execute(SafeSharedObjects shared)
        {
            int remaining = CountRemainingArgs(shared);

            VolumeDirectory directory;

            if (remaining == 0)
            {
                directory = shared.VolumeMgr.CurrentVolume.Root;
            }
            else
            {
                object pathObject = PopValueAssert(shared, true);

                GlobalPath path = shared.VolumeMgr.GlobalPathFromObject(pathObject);
                Volume volume = shared.VolumeMgr.GetVolumeFromPath(path);

                directory = volume.Open(path) as VolumeDirectory;

                if (directory == null)
                {
                    throw new KOSException("Invalid directory: " + pathObject);
                }
            }

            AssertArgBottomAndConsume(shared);

            shared.VolumeMgr.CurrentDirectory = directory;
        }
    }

    [Function("copypath")]
    public class FunctionCopyPath : SafeFunctionBase
    {
        public override void Execute(SafeSharedObjects shared)
        {
            object destinationPathObject = PopValueAssert(shared, true);
            object sourcePathObject = PopValueAssert(shared, true);
            AssertArgBottomAndConsume(shared);

            GlobalPath sourcePath = shared.VolumeMgr.GlobalPathFromObject(sourcePathObject);
            GlobalPath destinationPath = shared.VolumeMgr.GlobalPathFromObject(destinationPathObject);

            ReturnValue = shared.VolumeMgr.Copy(sourcePath, destinationPath);
        }
    }

    [Function("movepath")]
    public class FunctionMove : SafeFunctionBase
    {
        public override void Execute(SafeSharedObjects shared)
        {
            object destinationPathObject = PopValueAssert(shared, true);
            object sourcePathObject = PopValueAssert(shared, true);
            AssertArgBottomAndConsume(shared);

            GlobalPath sourcePath = shared.VolumeMgr.GlobalPathFromObject(sourcePathObject);
            GlobalPath destinationPath = shared.VolumeMgr.GlobalPathFromObject(destinationPathObject);

            ReturnValue = shared.VolumeMgr.Move(sourcePath, destinationPath);
        }
    }

    [Function("deletepath")]
    public class FunctionDeletePath : SafeFunctionBase
    {
        public override void Execute(SafeSharedObjects shared)
        {
            object pathObject = PopValueAssert(shared, true);
            AssertArgBottomAndConsume(shared);

            GlobalPath path = shared.VolumeMgr.GlobalPathFromObject(pathObject);
            Volume volume = shared.VolumeMgr.GetVolumeFromPath(path);

            ReturnValue = volume.Delete(path);
        }
    }

    [Function("writejson")]
    public class FunctionWriteJson : SafeFunctionBase
    {
        public override void Execute(SafeSharedObjects shared)
        {
            object pathObject = PopValueAssert(shared, true);
            SerializableStructure serialized = PopValueAssert(shared, true) as Structure;
            AssertArgBottomAndConsume(shared);

            if (serialized == null)
            {
                throw new KOSException("This type is not serializable");
            }

            string serializedString = new SafeSerializationMgr(shared).Serialize(serialized, JsonFormatter.WriterInstance);

            FileContent fileContent = new FileContent(serializedString);

            GlobalPath path = shared.VolumeMgr.GlobalPathFromObject(pathObject);
            Volume volume = shared.VolumeMgr.GetVolumeFromPath(path);

            ReturnValue = volume.SaveFile(path, fileContent);
        }
    }

    [Function("readjson")]
    public class FunctionReadJson : SafeFunctionBase
    {
        public override void Execute(SafeSharedObjects shared)
        {
            object pathObject = PopValueAssert(shared, true);
            AssertArgBottomAndConsume(shared);

            GlobalPath path = shared.VolumeMgr.GlobalPathFromObject(pathObject);
            Volume volume = shared.VolumeMgr.GetVolumeFromPath(path);

            VolumeFile volumeFile = volume.Open(path) as VolumeFile;

            if (volumeFile == null)
            {
                throw new KOSException("File does not exist: " + path);
            }

            Structure read = new SafeSerializationMgr(shared).Deserialize(volumeFile.ReadAll().String, JsonFormatter.ReaderInstance) as Structure;
            ReturnValue = read;
        }
    }

    [Function("exists")]
    public class FunctionExists : SafeFunctionBase
    {
        public override void Execute(SafeSharedObjects shared)
        {
            object pathObject = PopValueAssert(shared, true);
            AssertArgBottomAndConsume(shared);

            GlobalPath path = shared.VolumeMgr.GlobalPathFromObject(pathObject);
            Volume volume = shared.VolumeMgr.GetVolumeFromPath(path);

            ReturnValue = volume.Exists(path);
        }
    }

    [Function("open")]
    public class FunctionOpen : SafeFunctionBase
    {
        public override void Execute(SafeSharedObjects shared)
        {
            object pathObject = PopValueAssert(shared, true);
            AssertArgBottomAndConsume(shared);

            GlobalPath path = shared.VolumeMgr.GlobalPathFromObject(pathObject);
            Volume volume = shared.VolumeMgr.GetVolumeFromPath(path);

            VolumeItem volumeItem = volume.Open(path);

            if (volumeItem == null)
                ReturnValue = new BooleanValue(false);
            else
                ReturnValue = volumeItem;
        }
    }

    [Function("create")]
    public class FunctionCreate : SafeFunctionBase
    {
        public override void Execute(SafeSharedObjects shared)
        {
            object pathObject = PopValueAssert(shared, true);
            AssertArgBottomAndConsume(shared);

            GlobalPath path = shared.VolumeMgr.GlobalPathFromObject(pathObject);
            Volume volume = shared.VolumeMgr.GetVolumeFromPath(path);

            VolumeFile volumeFile = volume.CreateFile(path);

            ReturnValue = volumeFile;
        }
    }

    [Function("createdir")]
    public class FunctionCreateDirectory : SafeFunctionBase
    {
        public override void Execute(SafeSharedObjects shared)
        {
            object pathObject = PopValueAssert(shared, true);
            AssertArgBottomAndConsume(shared);

            GlobalPath path = shared.VolumeMgr.GlobalPathFromObject(pathObject);
            Volume volume = shared.VolumeMgr.GetVolumeFromPath(path);

            VolumeDirectory volumeDirectory = volume.CreateDirectory(path);

            ReturnValue = volumeDirectory;
        }
    }
}