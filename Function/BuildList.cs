using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using kOS.Suffixed;
using kOS.Utilities;

namespace kOS.Function
{
    [FunctionAttribute("buildlist")]
    public class FunctionBuildList : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            string listType = shared.Cpu.PopValue().ToString();
            ListValue list = new ListValue();

            switch (listType)
            {
                case "bodies":
                    foreach (var body in FlightGlobals.fetch.bodies)
                    {
                        list.Add(new BodyTarget(body, shared.Vessel));
                    }
                    break;
                case "targets":
                    foreach (var vessel in FlightGlobals.Vessels)
                    {
                        if (vessel == shared.Vessel) continue;
                        list.Add(new VesselTarget(vessel, shared.Vessel));
                    }
                    break;
                case "resources":
                case "parts":
                case "engines":
                case "sensors":
                case "elements":
                    list = VesselUtils.PartList(shared.Vessel, listType);
                    break;
            }

            shared.Cpu.PushStack(list);
        }
    }
}
