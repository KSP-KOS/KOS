using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using kOS.Safe.Function;
using kOS.Safe;
using kOS.Safe.Encapsulation;

namespace kOS.Safe.Function.Persistence
{
    [Function("create")]
    public class FunctionCreate : SafeFunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            string fileName = PopValueAssert(shared, true).ToString();
            AssertArgBottomAndConsume(shared);

            VolumeFile volumeFile = shared.VolumeMgr.CurrentVolume.Create(fileName);

            ReturnValue = volumeFile;
        }
    }
}
