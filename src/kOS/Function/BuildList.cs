using System;
using System.Linq;
using kOS.Safe.Encapsulation;
using kOS.Safe.Function;
using kOS.Suffixed;
using kOS.Utilities;

namespace kOS.Function
{
    [Function("buildlist")]
    public class FunctionBuildList : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            string listType = PopValueAssert(shared).ToString();
            var list = new ListValue();

            switch (listType)
            {
                case "bodies":
                    list = ListValue.CreateList(FlightGlobals.fetch.bodies);
                    break;
                case "targets":
                    foreach (var vessel in FlightGlobals.Vessels)
                    {
                        if (vessel == shared.Vessel) continue;
                        list.Add(new VesselTarget(vessel, shared));
                    }
                    break;
                case "resources":
                case "parts":
                case "engines":
                case "sensors":
                case "elements":
                case "dockingports":
                    list = shared.Vessel.PartList(listType, shared);
                    break;
                case "files":
                    list = ListValue.CreateList(shared.VolumeMgr.CurrentVolume.GetFileList());
                    break;
                case "volumes":
                    list = ListValue.CreateList(shared.VolumeMgr.Volumes.Values.ToList());
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            AssertArgBottomAndConsume(shared);

            ReturnValue = list;
        }
    }
}
