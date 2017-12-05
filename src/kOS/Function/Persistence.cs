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
    [Function("edit")]
    public class FunctionEdit : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            object pathObject = PopValueAssert(shared, true);
            AssertArgBottomAndConsume(shared);

            GlobalPath path = shared.VolumeMgr.GlobalPathFromObject(pathObject);
            Volume vol = shared.VolumeMgr.GetVolumeFromPath(path);
            shared.Window.OpenPopupEditor(vol, path);

        }
    }
}