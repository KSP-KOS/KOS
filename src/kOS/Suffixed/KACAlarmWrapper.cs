using System;
using System.Linq;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS_KACWrapper;

namespace kOS.Suffixed
{
    public class KACAlarmWrapper : Structure
    {
        private KACWrapper.KACAPI.KACAlarm alarm;

        public KACAlarmWrapper(KACWrapper.KACAPI.KACAlarm init) 
        { 
            alarm = init;
            InitializeSuffixes();
        }
        public KACAlarmWrapper(String alarmID) 
        {
            alarm = KACWrapper.KAC.Alarms.First(z=>z.ID==alarmID);
            InitializeSuffixes();
        }
        private void InitializeSuffixes()
        {
            AddSuffix("ID", new Suffix<string>(() => alarm.ID));
            AddSuffix("NAME", new SetSuffix<string>(() => alarm.Name, value => alarm.Name = value));
            AddSuffix("VESSELID", new SetSuffix<string>(() => alarm.VesselID, value => alarm.VesselID = value));
            AddSuffix("NOTES", new SetSuffix<string>(() => alarm.Name, value => alarm.Name = value));

            AddSuffix("REMAINING", new Suffix<double>(() => (double)alarm.Remaining));

            AddSuffix("ALARMTIME", new SetSuffix<double>(() => alarm.AlarmTime, value => alarm.AlarmTime = value));
            AddSuffix("ALARMMARGIN", new SetSuffix<double>(() => alarm.AlarmMargin, value => alarm.AlarmMargin = value));

            AddSuffix("REPEATALARM", new SetSuffix<Boolean>(() => alarm.RepeatAlarm, value => alarm.RepeatAlarm = value));

            AddSuffix("REPEATALARMPERIOD", new SetSuffix<double>(() => alarm.RepeatAlarmPeriod, value => alarm.RepeatAlarmPeriod = value));

            AddSuffix("XferOriginBodyName", new SetSuffix<string>(() => alarm.XferOriginBodyName, value => alarm.XferOriginBodyName = value));
            AddSuffix("XferTargetBodyName", new SetSuffix<string>(() => alarm.XferTargetBodyName, value => alarm.XferTargetBodyName = value));

        }
    }
}

