using kOS.Safe.Encapsulation;
using kOS.Safe.Persistence;

namespace kOS.Safe.Function.Misc
{
    [Function("logfile")]
    public class FunctionLogFile : SafeFunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            string fileName = PopValueAssert(shared, true).ToString();
            string expressionResult = PopValueAssert(shared).ToString();
            AssertArgBottomAndConsume(shared);

            if (shared.VolumeMgr != null)
            {
                Volume volume = shared.VolumeMgr.CurrentVolume;
                if (volume != null)
                {
                    VolumeFile volumeFile = volume.OpenOrCreate(fileName);

                    if (volumeFile == null || !volumeFile.WriteLn(expressionResult))
                    {
                        throw new KOSFileException("Can't append to file: not enough space or access forbidden");
                    }
                }
                else
                {
                    throw new KOSFileException("Volume not found");
                }
            }
        }
    }
}