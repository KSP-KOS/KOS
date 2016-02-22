using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;


namespace kOS.Suffixed
{
    public class AddonList : Structure
    {
        private readonly SharedObjects shared;

        public AddonList(SharedObjects shared)
        {
            this.shared = shared;

            InitializeSuffixes();
        }

        private void InitializeSuffixes()
        {
            AddSuffix("KAC", new Suffix<Addon>(() => new AddOns.KerbalAlarmClock.Addon(shared)));
            AddSuffix("RT", new Suffix<Addon>(() => new AddOns.RemoteTech.Addon(shared)));
            AddSuffix("AGX", new Suffix<Addon>(() => new AddOns.ActionGroupsExtended.Addon(shared)));
            AddSuffix("IR", new Suffix<Addon>(() => new AddOns.InfernalRobotics.Addon(shared)));
        }

        public override string ToString()
        {
            return string.Format("{0} AddonList", base.ToString());
        }
    }
}