using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using kOS.Suffixed.Part;

namespace kOS.AddOns.InfernalRobotics
{
    [kOSAddon("IR")]
    [kOS.Safe.Utilities.KOSNomenclature("IRAddon")]
    public class Addon : Suffixed.Addon
    {
        public Addon(SharedObjects shared)
            : base(shared)
        {
            InitializeSuffixes();
        }

        private void InitializeSuffixes()
        {
            AddSuffix("GROUPS", new Suffix<ListValue>(GetServoGroups, "List all ServoGroups"));
            AddSuffix("ALLSERVOS", new Suffix<ListValue>(GetAllServos, "List all Servos"));
            AddSuffix("PARTSERVOS", new OneArgsSuffix<ListValue, PartValue>(GetPartServos, "List Servos from Part"));
        }

        private ListValue GetServoGroups()
        {
            var list = new ListValue();

            if (!IRWrapper.APIReady)
            {
                throw new KOSUnavailableAddonException("IR:GROUPS", "Infernal Robotics");
            }

            var controlGroups = IRWrapper.IRController.ServoGroups;

            if (controlGroups == null)
            {
                //Control Groups are somehow null, just return the empty list
                return list;
            }

            foreach (IRWrapper.IControlGroup cg in controlGroups)
            {
                if (cg.Vessel == null || cg.Vessel == shared.Vessel)
                    list.Add(new IRControlGroupWrapper(cg, shared));
            }

            return list;
        }

        private ListValue GetAllServos()
        {
            var list = new ListValue();

            if (!IRWrapper.APIReady)
            {
                throw new KOSUnavailableAddonException("IR:ALLSERVOS", "Infernal Robotics");
            }

            var controlGroups = IRWrapper.IRController.ServoGroups;

            if (controlGroups == null)
            {
                //Control Groups are somehow null, just return the empty list
                return list;
            }

            foreach (IRWrapper.IControlGroup cg in controlGroups)
            {
                if (cg.Servos == null || (cg.Vessel != null && cg.Vessel != shared.Vessel))
                    continue;

                foreach (IRWrapper.IServo s in cg.Servos)
                {
                    list.Add(new IRServoWrapper(s, shared));
                }
            }

            return list;
        }

        private ListValue GetPartServos(PartValue pv)
        {
            var list = new ListValue();

            if (!IRWrapper.APIReady)
            {
                throw new KOSUnavailableAddonException("IR:PARTSERVOS", "Infernal Robotics");
            }

            var controlGroups = IRWrapper.IRController.ServoGroups;

            if (controlGroups == null)
            {
                //Control Groups are somehow null, just return the empty list
                return list;
            }

            foreach (IRWrapper.IControlGroup cg in controlGroups)
            {
                if (cg.Servos == null || (cg.Vessel != null && cg.Vessel != shared.Vessel))
                    continue;

                foreach (IRWrapper.IServo s in cg.Servos)
                {
                    if (s.UID == pv.Part.craftID)
                        list.Add(new IRServoWrapper(s, shared));
                }
            }

            return list;
        }

        public override BooleanValue Available()
        {
            return IRWrapper.APIReady;
        }
    }
}