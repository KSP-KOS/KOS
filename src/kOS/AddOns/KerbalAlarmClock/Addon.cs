using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;

namespace kOS.AddOns.KerbalAlarmClock
{
    [kOS.Safe.Utilities.KOSNomenclature("KACAddon")]
    public class Addon : Suffixed.Addon
    {
        public Addon(SharedObjects shared) : base ("KAC", shared)
        {
            InitializeSuffixes();
        }

        private void InitializeSuffixes()
        {
            AddSuffix("ALARMS", new Suffix<ListValue>(GetAlarms, "List all alarms"));
        }

        private ListValue GetAlarms()
        {
            var list = new ListValue();

            if (!KACWrapper.APIReady)
            {
                return list;
            }

            //Get the list of alarms from the KAC Object
            KACWrapper.KACAPI.KACAlarmList alarms = KACWrapper.KAC.Alarms;

            foreach (KACWrapper.KACAPI.KACAlarm alarm in alarms)
            {
                list.Add(new KACAlarmWrapper(alarm));
            }
            return list;
        }

        public override BooleanValue Available()
        {
            return KACWrapper.APIReady;
        }

    }
}