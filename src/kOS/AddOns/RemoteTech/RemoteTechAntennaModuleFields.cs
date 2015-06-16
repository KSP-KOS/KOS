using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using kOS.Safe.Utilities;
using kOS.Suffixed;
using kOS.Suffixed.Part;
using kOS.Suffixed.PartModuleField;
using UnityEngine;
using Math = kOS.Safe.Utilities.Math;

namespace kOS.AddOns.RemoteTech
{
    public class RemoteTechAntennaModuleFields : PartModuleFields
    {
        // those Guids are hardcoded in RemoteTech
        public const String NoTargetGuid = "00000000000000000000000000000000";
        public const String ActiveVesselGuid = "35b89a0d664c43c6bec8d0840afc97b2";
        public const String MissionControlGuid = "5105f5a9d62841c6ad4b21154e8fc488";

        public const String RTAntennaModule = "ModuleRTAntenna";
        public const String RTOriginalField = "RTAntennaTarget";
        public const String RTTargetField = "target";

        public const String NoTargetString = "no-target";
        public const String ActiveVesselString = "active-vessel";
        public const String MissionControlString = "mission-control";

        public RemoteTechAntennaModuleFields(PartModule partModule, SharedObjects shared)
            : base(partModule, shared)
        {
            // overwrite suffixes with our own
            InitializeSuffixesAfterConstruction();
        }

        private void InitializeSuffixesAfterConstruction()
        {
            AddSuffix("ALLFIELDS", new Suffix<ListValue>(() => AllFields("({0}) {1}, is {2}")));
            AddSuffix("ALLFIELDNAMES", new Suffix<ListValue>(AllFieldNames));
            AddSuffix("HASFIELD", new OneArgsSuffix<bool, string>(HasField));
            AddSuffix("GETFIELD", new OneArgsSuffix<object, string>(GetKSPFieldValue));
            AddSuffix("SETFIELD", new TwoArgsSuffix<string, object>(SetKSPFieldValue));
        }

        public new bool HasField(string fieldName)
        {
            return fieldName.Equals(RemoteTechAntennaModuleFields.RTTargetField) || base.HasField(fieldName);
        }

        protected override ListValue AllFields(string formatter)
        {
            var returnValue = base.AllFields(formatter);

            returnValue.Add(String.Format(formatter, "settable",
                    RTTargetField.ToLower(), "String | Body | Vessel"));

            return returnValue;
        }

        protected new ListValue AllFieldNames()
        {
            var returnValue = base.AllFieldNames();

            returnValue.Add(RTTargetField.ToLower());

            return returnValue;
        }

        protected new BaseField GetField(string cookedGuiName)
        {
            return partModule.Fields.Cast<BaseField>().
                FirstOrDefault(field => String.Equals(field.guiName, cookedGuiName, StringComparison.CurrentCultureIgnoreCase));
        }

        protected new object GetKSPFieldValue(string suffixName)
        {
            if (suffixName.Equals(RTTargetField, StringComparison.InvariantCultureIgnoreCase))
            {
                BaseField field = GetField(RTOriginalField);
                Guid guid = (Guid)field.GetValue(partModule);
                String guidString = guid.ToString("N");

                if (guidString.Equals(NoTargetGuid))
                {
                    return NoTargetString;
                }
                else if (guidString.Equals(ActiveVesselGuid))
                {
                    return ActiveVesselString;
                }
                else if (guidString.Equals(MissionControlGuid))
                {
                    return MissionControlString;
                }

                foreach (var body in FlightGlobals.Bodies)
                {
                    if (CelestialBodyGuid(body).Equals(guid))
                    {
                        return new BodyTarget(body, this.shared);
                    }
                }

                foreach (var vessel in FlightGlobals.Vessels)
                {
                    if (vessel.id.Equals(guid))
                    {
                        return new VesselTarget(vessel, this.shared);
                    }
                }

                // just print the guid if we can't figure out what it is
                return guid.ToString();

            }
            return base.GetKSPFieldValue(suffixName);
        }

        private Guid GetTargetGuid(object target)
        {
            if (target is String)
            {
                String targetString = (String)target;
                if (targetString.Equals(NoTargetString, StringComparison.InvariantCultureIgnoreCase))
                {
                    return new Guid(NoTargetGuid);
                }
                else if (targetString.Equals(ActiveVesselString, StringComparison.InvariantCultureIgnoreCase))
                {
                    return new Guid(ActiveVesselGuid);
                }
                else if (targetString.Equals(MissionControlString, StringComparison.InvariantCultureIgnoreCase))
                {
                    return new Guid(MissionControlGuid);
                }
                else
                {
                    var body = FlightGlobals.Bodies.Where(b => b.bodyName.Equals(targetString, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                    if (body != null)
                    {
                        return CelestialBodyGuid(body);
                    }
                    else
                    {
                        var vessel = FlightGlobals.Vessels.Where(v => v.vesselName.Equals(targetString, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                        if (vessel != null)
                        {
                            if (partModule.vessel.id.Equals(vessel.id))
                            {
                                throw new KOSInvalidFieldValueException("Current vessel can't be the target");
                            }

                            return vessel.id;
                        }
                    }
                }
            }
            else if (target is BodyTarget)
            {
                BodyTarget targetBody = (BodyTarget)target;
                return CelestialBodyGuid(targetBody.Body);
            }
            else if (target is VesselTarget)
            {
                VesselTarget targetVessel = (VesselTarget)target;

                if (partModule.vessel.id.Equals(targetVessel.Vessel.id))
                {
                    throw new KOSInvalidFieldValueException("Current vessel can't be the target");
                }

                return targetVessel.Vessel.id;
            }

            throw new KOSInvalidFieldValueException("'" + NoTargetString + "', '" + ActiveVesselString + "', '" + MissionControlString +
                "', Body or Vessel expected");

        }

        protected new void SetKSPFieldValue(string suffixName, object newValue)
        {
            if (suffixName.Equals(RTTargetField, StringComparison.InvariantCultureIgnoreCase))
            {
                Guid guid = GetTargetGuid(newValue);

                BaseField field = GetField(RTOriginalField);
                field.SetValue(guid, partModule);
            }
            else
            {
                base.SetKSPFieldValue(suffixName, newValue);
            }
        }

        // taken from RemoteTech's RTUtil
        public static Guid CelestialBodyGuid(CelestialBody cb)
        {
            char[] name = cb.GetName().ToCharArray();
            var s = new StringBuilder();
            for (int i = 0; i < 16; i++)
            {
                s.Append(((short)name[i % name.Length]).ToString("x"));
            }
            return new Guid(s.ToString());
        }
    }
}

