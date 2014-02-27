using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using kOS.Suffixed;

namespace kOS.Function
{
    [FunctionAttribute("buildlist")]
    public class FunctionBuildList : FunctionBase
    {
        private static readonly List<string> _shouldBuildPartList = new List<string>() { "resources", "parts", "engines", "sensors", "elements" };

        public override void Execute(SharedObjects shared)
        {
            string listType = shared.Cpu.PopValue().ToString();
            IEnumerable<Part> partList = null;
            ListValue list = new ListValue();

            if (_shouldBuildPartList.Contains(listType))
            {
                partList = shared.Vessel.Parts.ToList();
            }

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
                    list = ResourceValue.PartsToList(partList);
                    break;
                case "parts":
                    list = PartValue.PartsToList(partList);
                    break;
                case "engines":
                    list = EngineValue.PartsToList(partList);
                    break;
                case "sensors":
                    list = SensorValue.PartsToList(partList);
                    break;
                case "elements":
                    list = ElementValue.PartsToList(partList);
                    break;
            }

            shared.Cpu.PushStack(list);
        }
    }
}
