using kOS.Safe.Persistence;
using System;

namespace kOS.Safe.Function.Persistence
{
    [Function("rename")]
    public class FunctionRename : SafeFunctionBase
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
                        if (volume.Open(newName) == null)
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
}