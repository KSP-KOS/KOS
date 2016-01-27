using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using kOS.Suffixed;
using kOS.Suffixed.PartModuleField;
using System;
using System.Linq;
using System.Text;

namespace kOS.AddOns.RemoteTech
{
    public class RemoteTechAntennaModuleFields : PartModuleFields
    {
        // those Guids are hardcoded in RemoteTech
        public const string NoTargetGuid = "00000000000000000000000000000000";

        public const string ActiveVesselGuid = "35b89a0d664c43c6bec8d0840afc97b2";
        public const string MissionControlGuid = "5105f5a9d62841c6ad4b21154e8fc488";

        public const string RTAntennaModule = "ModuleRTAntenna";
        public const string RTOriginalField = "RTAntennaTarget";
        public const string RTTargetField = "target";

        public const string NoTargetString = "no-target";
        public const string ActiveVesselString = "active-vessel";
        public const string MissionControlString = "mission-control";

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
            AddSuffix("HASFIELD", new OneArgsSuffix<BooleanValue, StringValue>(HasField));
            AddSuffix("GETFIELD", new OneArgsSuffix<Structure, StringValue>(GetKSPFieldValue));
            AddSuffix("SETFIELD", new TwoArgsSuffix<StringValue, object>(SetKSPFieldValue));
        }

        public override BooleanValue HasField(StringValue fieldName)
        {
            if (fieldName.Equals(RTTargetField)) return true;
            return base.HasField(fieldName);
        }

        protected override ListValue AllFields(string formatter)
        {
            var returnValue = base.AllFields(formatter);

            returnValue.Add(new StringValue(string.Format(formatter, "settable",
                    RTTargetField.ToLower(), "String | Body | Vessel")));

            return returnValue;
        }

        protected override ListValue AllFieldNames()
        {
            var returnValue = base.AllFieldNames();

            returnValue.Add(new StringValue(RTTargetField.ToLower()));

            return returnValue;
        }

        protected new BaseField GetField(string cookedGuiName)
        {
            return partModule.Fields.Cast<BaseField>().
                FirstOrDefault(field => string.Equals(field.guiName, cookedGuiName, StringComparison.CurrentCultureIgnoreCase));
        }

        protected new Structure GetKSPFieldValue(StringValue suffixName)
        {
            if (Equals(suffixName, new StringValue(RTTargetField)))
            {
                BaseField field = GetField(RTOriginalField);
                Guid guid = (Guid)field.GetValue(partModule);
                string guidString = guid.ToString("N");

                if (guidString.Equals(NoTargetGuid))
                {
                    return new StringValue(NoTargetString);
                }
                else if (guidString.Equals(ActiveVesselGuid))
                {
                    return new StringValue(ActiveVesselString);
                }
                else if (guidString.Equals(MissionControlGuid))
                {
                    return new StringValue(MissionControlString);
                }

                foreach (var body in FlightGlobals.Bodies)
                {
                    if (CelestialBodyGuid(body).Equals(guid))
                    {
                        return new BodyTarget(body, shared);
                    }
                }

                foreach (var vessel in FlightGlobals.Vessels)
                {
                    if (vessel.id.Equals(guid))
                    {
                        return new VesselTarget(vessel, shared);
                    }
                }

                // just print the guid if we can't figure out what it is
                return new StringValue(guid.ToString());
            }
            return base.GetKSPFieldValue(suffixName);
        }

        private Guid GetTargetGuid(object target)
        {
            var str = target as string;
            if (str != null)
            {
                string targetString = str;
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
                    var body = FlightGlobals.Bodies.FirstOrDefault(b => b.bodyName.Equals(targetString, StringComparison.InvariantCultureIgnoreCase));
                    if (body != null)
                    {
                        return CelestialBodyGuid(body);
                    }
                    else
                    {
                        var vessel = FlightGlobals.Vessels.FirstOrDefault(v => v.vesselName.Equals(targetString, StringComparison.InvariantCultureIgnoreCase));
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

        protected override void SetKSPFieldValue(StringValue suffixName, object newValue)
        {
            if (Equals(suffixName, new StringValue(RTTargetField)))
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