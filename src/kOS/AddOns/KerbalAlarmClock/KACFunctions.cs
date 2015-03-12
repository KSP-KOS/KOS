using kOS.Function;
using kOS.Safe.Encapsulation;
using kOS.Safe.Exceptions;
using kOS.Safe.Function;
using kOS.Safe.Utilities;
using System;
using System.Linq;

namespace kOS.AddOns.KerbalAlarmClock
{
    [Function("addAlarm")]
    public class FunctionAddAlarm : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            string alarmNotes = shared.Cpu.PopValue().ToString();
            string alarmName = shared.Cpu.PopValue().ToString();
            double alarmUT = GetDouble(shared.Cpu.PopValue());
            string alarmType = shared.Cpu.PopValue().ToString(); //alarm type is read-only, you cannot change it afterwards

            if (KACWrapper.APIReady)
            {
                KACWrapper.KACAPI.AlarmTypeEnum newAlarmType;
                try
                {
                    newAlarmType = (KACWrapper.KACAPI.AlarmTypeEnum)Enum.Parse(typeof(KACWrapper.KACAPI.AlarmTypeEnum), alarmType);
                }
                catch (ArgumentException)
                {
                    SafeHouse.Logger.LogWarning(string.Format("Failed parsing {0} into KACAPI.AlarmTypeEnum", alarmType));
                    //failed parsing alarmType, defaulting to Raw
                    newAlarmType = KACWrapper.KACAPI.AlarmTypeEnum.Raw;
                }

                String aID = KACWrapper.KAC.CreateAlarm(newAlarmType, alarmName, alarmUT);

                SafeHouse.Logger.Log(string.Format("Trying to create KAC Alarm, UT={0}, Name={1}, Type= {2}", alarmUT, alarmName, alarmType));

                if (!string.IsNullOrEmpty(aID))
                {
                    //if the alarm was made get the object so we can update it
                    KACWrapper.KACAPI.KACAlarm a = KACWrapper.KAC.Alarms.First(z => z.ID == aID);

                    //Now update some of the other properties
                    a.Notes = alarmNotes;
                    a.AlarmAction = KACWrapper.KACAPI.AlarmActionEnum.PauseGame;
                    a.VesselID = shared.Vessel.id.ToString();

                    var result = new KACAlarmWrapper(a, shared);

                    shared.Cpu.PushStack(result);
                }
                else
                {
                    shared.Cpu.PushStack(string.Empty);
                    SafeHouse.Logger.Log(string.Format("Failed creating KAC Alarm, UT={0}, Name={1}, Type= {2}", alarmUT, alarmName, alarmType));
                }
            }
            else
            {
                //KAC integration not present.
                shared.Cpu.PushStack(string.Empty);
                throw new KOSUnavailableAddonException("addAlarm()", "Kerbal Alarm Clock");
            }
        }
    }

    [Function("listAlarms")]
    public class FunctionListAlarms : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            var list = new ListValue();

            string alarmTypes = shared.Cpu.PopValue().ToString();

            if (!KACWrapper.APIReady)
            {
                shared.Cpu.PushStack(list);
                throw new KOSUnavailableAddonException("listAlarms()", "Kerbal Alarm Clock");
            }

            //Get the list of alarms from the KAC Object
            KACWrapper.KACAPI.KACAlarmList alarms = KACWrapper.KAC.Alarms;

            foreach (KACWrapper.KACAPI.KACAlarm alarm in alarms)
            {
                // if its not my alarm or a general alarm, ignore it
                if (!string.IsNullOrEmpty(alarm.VesselID) && alarm.VesselID != shared.Vessel.id.ToString())
                {
                    continue;
                }
                
                if (alarmTypes.ToUpperInvariant() == "ALL" || alarm.AlarmTime.ToString() == alarmTypes)
                    list.Add(new KACAlarmWrapper(alarm, shared));
            }
            shared.Cpu.PushStack(list);
        }
    }

    [Function("deleteAlarm")]
    public class FunctionDeleteAlarm : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            string alarmID = shared.Cpu.PopValue().ToString();

            if (KACWrapper.APIReady)
            {
                bool result = KACWrapper.KAC.DeleteAlarm(alarmID);
                shared.Cpu.PushStack(result);
            }
            else
            {
                shared.Cpu.PushStack(false);
                throw new KOSUnavailableAddonException("deleteAlarm()", "Kerbal Alarm Clock");
            }
        }
    }
}