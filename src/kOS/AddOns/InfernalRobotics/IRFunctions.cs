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
    public class FunctionIRListServos : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            var list = new ListValue<IRServoWrapper>();
            if (!IRWrapper.APIReady)
            {
                ReturnValue = list;
                //throw new KOSUnavailableAddonException("IR_listServos()", "Infernal Robotics");
                return;
            }

            var controlGroup = (IRWrapper.IRAPI.IRControlGroup) PopValueAssert(shared);
            AssertArgBottomAndConsume(shared);
            if (controlGroup == null)
            {
                ReturnValue = list;
                return;
            }

            IRWrapper.IRAPI.IRServosList servos = controlGroup.Servos;

            foreach (IRWrapper.IRAPI.IRServo s in servos)
            {
                list.Add(new IRServoWrapper(s, shared));
            }

            ReturnValue = list;
        }
    }

    [Function("IR_listControlGroups")]
    public class FunctionIRListControlGroups : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            var list = new ListValue<IRControlGroupWrapper>();

            AssertArgBottomAndConsume(shared);

            if (!IRWrapper.APIReady)
            {
                SafeHouse.Logger.SuperVerbose ("IRAPI not ready.");
                ReturnValue = list;
                //throw new KOSUnavailableAddonException("listControlGroups()", "Kerbal Alarm Clock");
                return;
            }

            IRWrapper.IRAPI.IRServoGroupsList controlGroups = IRWrapper.IRController.ServoGroups;

            if (controlGroups == null)
            {
                ReturnValue = list;
                //throw new KOSUnavailableAddonException("listControlGroups()", "Kerbal Alarm Clock");
                return;
            }

            foreach (IRWrapper.IRAPI.IRControlGroup cg in controlGroups)
            {
                list.Add(new IRControlGroupWrapper(cg, shared));
            }

            ReturnValue = list;
        }
    }
}