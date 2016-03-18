using kOS.Safe.Encapsulation;
using kOS.Safe.Exceptions;

namespace kOS.Safe.Function.Persistence
{
    [Function("open")]
    public class FunctionOpen : SafeFunctionBase
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

            ReturnValue = volumeFile;
        }
    }
}