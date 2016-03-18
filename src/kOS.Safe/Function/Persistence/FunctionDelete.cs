using kOS.Safe.Persistence;
using System;

namespace kOS.Safe.Function.Persistence
{
    [Function("delete")]
    public class FunctionDelete : SafeFunctionBase
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
                    if (!volume.Delete(fileName))
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