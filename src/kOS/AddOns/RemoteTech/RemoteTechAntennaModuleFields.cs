using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using kOS.Suffixed;
using kOS.Suffixed.PartModuleField;
using System;
using System.Collections.Generic;
using System.Linq;

namespace kOS.AddOns.RemoteTech
{
    [kOS.Safe.Utilities.KOSNomenclature("RTAddonAntennaModule")]
    public class RemoteTechAntennaModuleFields : PartModuleFields
    {
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
            AddSuffix("SETFIELD", new TwoArgsSuffix<StringValue, Structure>(SetKSPFieldValue));
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
                var api = RemoteTechHook.Instance;
                Guid guid = api.GetAntennaTarget(partModule.part);

                if (guid.Equals(api.GetNoTargetGuid()))
                {
                    return new StringValue(NoTargetString);
                }
                else if (guid.Equals(api.GetActiveVesselGuid()))
                {
                    return new StringValue(ActiveVesselString);
                }
                else
                {
                    IEnumerable<string> groundStations = api.GetGroundStations();
                    foreach (var groundStation in groundStations)
                    {
                        if (guid.Equals(api.GetGroundStationGuid(groundStation)))
                        {
                            return new StringValue(groundStation);
                        }
                    }
                }

                foreach (var body in FlightGlobals.Bodies)
                {
                    if (api.GetCelestialBodyGuid(body).Equals(guid))
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

        private Guid GetTargetGuid(Structure target)
        {
            var api = RemoteTechHook.Instance;
            if (target is StringValue)
            {
                string targetString = target.ToString();
                if (targetString.Equals(NoTargetString, StringComparison.InvariantCultureIgnoreCase))
                {
                    return api.GetNoTargetGuid();
                }
                else if (targetString.Equals(ActiveVesselString, StringComparison.InvariantCultureIgnoreCase))
                {
                    return api.GetActiveVesselGuid();
                }
                else
                {
                    IEnumerable<string> groundStations = api.GetGroundStations();
                    foreach (var groundStation in groundStations)
                    {
                        if (targetString.Equals(groundStation))
                        {
                            return api.GetGroundStationGuid(groundStation);
                        }
                    }

                    var body = FlightGlobals.Bodies.Where(b => b.bodyName.Equals(targetString, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                    if (body != null)
                    {
                        return api.GetCelestialBodyGuid(body);
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
                return api.GetCelestialBodyGuid(targetBody.Body);
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

            throw new KOSInvalidFieldValueException("Acceptable values are: \"" + NoTargetString + "\", \"" + ActiveVesselString +
                "\", name of a ground station, name of a body, name of a vessel, Body, Vessel");
        }

        protected override void SetKSPFieldValue(StringValue suffixName, Structure newValue)
        {
            if (Equals(suffixName, new StringValue(RTTargetField)))
            {
                Guid guid = GetTargetGuid(newValue);

                RemoteTechHook.Instance.SetAntennaTarget(partModule.part, guid);
            }
            else
            {
                base.SetKSPFieldValue(suffixName, newValue);
            }
        }
    }
}