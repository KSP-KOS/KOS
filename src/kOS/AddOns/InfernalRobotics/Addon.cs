﻿using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;

namespace kOS.AddOns.InfernalRobotics
{
    [kOS.Safe.Utilities.KOSNomenclature("IRAddon")]
    public class Addon : Suffixed.Addon
    {
        public Addon(SharedObjects shared) : base ("IR", shared)
        {
            InitializeSuffixes();
        }

        private void InitializeSuffixes()
        {
            AddSuffix("GROUPS", new Suffix<ListValue>(GetServoGroups, "List all ServoGroups"));
            AddSuffix("ALLSERVOS", new Suffix<ListValue>(GetAllServos, "List all Servos"));
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
                if (cg.Servos == null || (cg.Vessel!=null && cg.Vessel != shared.Vessel))
                    continue;
                
                foreach (IRWrapper.IServo s in cg.Servos)
                {
                    list.Add (new IRServoWrapper (s, shared));
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

