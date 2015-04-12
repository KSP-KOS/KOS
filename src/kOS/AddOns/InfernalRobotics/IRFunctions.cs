using kOS.Function;
using kOS.Safe.Encapsulation;
using kOS.Safe.Exceptions;
using kOS.Safe.Function;
using kOS.Safe.Utilities;
using System;
using System.Linq;

namespace kOS.AddOns.InfernalRobotics
{
    [Function("IR_listServos")]
    public class FunctionListServos : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            var list = new ListValue();
            if (!IRWrapper.APIReady)
            {
                shared.Cpu.PushStack(list);
                //throw new KOSUnavailableAddonException("IR_listServos()", "Infernal Robotics");
                return;
            }

            IRWrapper.IRAPI.IRControlGroup controlGroup = (IRWrapper.IRAPI.IRControlGroup) shared.Cpu.PopValue();

            if (controlGroup == null)
            {
                shared.Cpu.PushStack(list);
                return;
            }

            IRWrapper.IRAPI.IRServosList servos = controlGroup.Servos;

            foreach (IRWrapper.IRAPI.IRServo s in servos)
            {
                list.Add(new IRServoWrapper(s, shared));
            }

            shared.Cpu.PushStack(list);
        }
    }

    [Function("IR_listControlGroups")]
    public class FunctionListControlGroups : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            var list = new ListValue();
            if (!IRWrapper.APIReady)
            {
                shared.Cpu.PushStack(list);
                //throw new KOSUnavailableAddonException("listControlGroups()", "Kerbal Alarm Clock");
                return;
            }

            IRWrapper.IRAPI.IRServoGroupsList controlGroups = IRWrapper.IRController.ServoGroups;

            foreach (IRWrapper.IRAPI.IRControlGroup cg in controlGroups)
            {
                list.Add(new IRControlGroupWrapper(cg, shared));
            }

            shared.Cpu.PushStack(list);
        }
    }
}