using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Utilities;
using System;
using System.Linq;

namespace kOS.AddOns.KerbalAlarmClock
{
    [kOS.Safe.Utilities.KOSNomenclature("KACAlarm")]
    public class KACAlarmWrapper : Structure
    {
        private readonly KACWrapper.KACAPI.KACAlarm alarm;

        public KACAlarmWrapper(KACWrapper.KACAPI.KACAlarm init)
        {
            alarm = init;
            InitializeSuffixes();
        }

        public KACAlarmWrapper(string alarmID)
        {
            alarm = KACWrapper.KAC.Alarms.First(z => z.ID == alarmID);
            InitializeSuffixes();
        }

        private void InitializeSuffixes()
        {
            AddSuffix("ID", new Suffix<StringValue>(() => alarm.ID));
            AddSuffix("NAME", new SetSuffix<StringValue>(() => alarm.Name, value => alarm.Name = value));

            AddSuffix("NOTES", new SetSuffix<StringValue>(() => alarm.Notes, value => alarm.Notes = value));

            AddSuffix("ACTION", new SetSuffix<StringValue>(GetAlarmAction, SetAlarmAction));

            AddSuffix("TYPE", new Suffix<StringValue>(() => alarm.AlarmType.ToString()));

            AddSuffix("REMAINING", new Suffix<ScalarValue>(GetTimeToAlarm));

            AddSuffix("TIME", new SetSuffix<ScalarValue>(() => alarm.AlarmTime, value => alarm.AlarmTime = value));
            AddSuffix("MARGIN", new SetSuffix<ScalarValue>(() => alarm.AlarmMargin, value => alarm.AlarmMargin = value));

            AddSuffix("REPEAT", new SetSuffix<BooleanValue>(() => alarm.RepeatAlarm, value => alarm.RepeatAlarm = value));

            AddSuffix("REPEATPERIOD", new SetSuffix<ScalarValue>(() => alarm.RepeatAlarmPeriod, value => alarm.RepeatAlarmPeriod = value));

            AddSuffix("ORIGINBODY", new SetSuffix<StringValue>(() => alarm.XferOriginBodyName, value => alarm.XferOriginBodyName = value));
            AddSuffix("TARGETBODY", new SetSuffix<StringValue>(() => alarm.XferTargetBodyName, value => alarm.XferTargetBodyName = value));
        }

        public override string ToString()
        {
            return string.Format("{0} Alarm at {1}: {2}{3}",
                alarm.AlarmType.ToString(),
                KSPUtil.dateTimeFormatter.PrintTimeStamp(alarm.AlarmTime, true, true),
                alarm.Name,
                (alarm.Notes != null && alarm.Notes.Length > 0) ? ("\n" + alarm.Notes ) : "" );
        }

        private ScalarValue GetTimeToAlarm()
        {
            //workaround for alarm.Remaining type mismatch
            return alarm.AlarmTime - Planetarium.GetUniversalTime();
        }

        private StringValue GetAlarmAction()
        {
            //For some reason had to do it this way, otherwise ACTION suffix returned incorrect values
            return alarm.AlarmAction.ToString();
        }

        private void SetAlarmAction(StringValue newAlarmAction)
        {
            try
            {
                var result = (KACWrapper.KACAPI.AlarmActionEnum)Enum.Parse(typeof(KACWrapper.KACAPI.AlarmActionEnum), newAlarmAction);
                alarm.AlarmAction = result;
            }
            catch (ArgumentException)
            {
                SafeHouse.Logger.LogWarning(string.Format("Failed parsing {0} into KACAPI.AlarmActionEnum", newAlarmAction));
            }
        }
    }
}