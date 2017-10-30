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
            string alarmNotes = PopValueAssert(shared).ToString();
            string alarmName = PopValueAssert(shared).ToString();
            double alarmUT = GetDouble(PopValueAssert(shared));
            string alarmType = PopValueAssert(shared).ToString(); //alarm type is read-only, you cannot change it afterwards
            AssertArgBottomAndConsume(shared);

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

                string alarmId = KACWrapper.KAC.CreateAlarm(newAlarmType, alarmName, alarmUT);

                SafeHouse.Logger.Log(string.Format("Trying to create KAC Alarm, UT={0}, Name={1}, Type= {2}", alarmUT, alarmName, alarmType));

                if (!string.IsNullOrEmpty(alarmId))
                {
                    //if the alarm was made get the object so we can update it
                    KACWrapper.KACAPI.KACAlarm alarm = KACWrapper.KAC.Alarms.First(z => z.ID == alarmId);

                    //Now update some of the other properties
                    alarm.Notes = alarmNotes;
                    alarm.AlarmAction = KACWrapper.KACAPI.AlarmActionEnum.PauseGame;
                    alarm.VesselID = shared.Vessel.id.ToString();

                    var result = new KACAlarmWrapper(alarm);

                    ReturnValue = result;
                }
                else
                {
                    ReturnValue = new StringValue(string.Empty);
                    SafeHouse.Logger.Log(string.Format("Failed creating KAC Alarm, UT={0}, Name={1}, Type= {2}", alarmUT, alarmName, alarmType));
                }
            }
            else
            {
                //KAC integration not present.
                ReturnValue = new StringValue(string.Empty);
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

            string alarmTypes = PopValueAssert(shared).ToString();
            AssertArgBottomAndConsume(shared);

            if (!KACWrapper.APIReady)
            {
                ReturnValue = list;
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
                    list.Add(new KACAlarmWrapper(alarm));
            }
            ReturnValue = list;
        }
    }

    [Function("deleteAlarm")]
    public class FunctionDeleteAlarm : FunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            string alarmID = PopValueAssert(shared).ToString();
            AssertArgBottomAndConsume(shared);

            if (KACWrapper.APIReady)
            {
                bool result = KACWrapper.KAC.DeleteAlarm(alarmID);
                ReturnValue = result;
            }
            else
            {
                ReturnValue = false;
                throw new KOSUnavailableAddonException("deleteAlarm()", "Kerbal Alarm Clock");
            }
        }
    }
}