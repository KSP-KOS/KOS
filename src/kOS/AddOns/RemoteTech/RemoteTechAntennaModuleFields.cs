using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using kOS.Suffixed;
using kOS.Suffixed.PartModuleField;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace kOS.AddOns.RemoteTech
{
    public class RemoteTechAntennaModuleFields : PartModuleFields
    {
        public const String RTAntennaModule = "ModuleRTAntenna";
        public const String RTTargetField = "target";

        public const String NoTargetString = "no-target";
        public const String ActiveVesselString = "active-vessel";

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
                var api = RemoteTechHook.Instance;
                Guid guid = api.GetAntennaTarget(partModule.part);

                if (guid.Equals(api.GetNoTargetGuid()))
                {
                    return NoTargetString;
                }
                else if (guid.Equals(api.GetActiveVesselGuid()))
                {
                    return ActiveVesselString;
                }
                else
                {
                    IEnumerable<string> groundStations = api.GetGroundStations();
                    foreach (var groundStation in groundStations) {
                        if (guid.Equals(api.GetGroundStationGuid(groundStation))) {
                            return groundStation;
                        }
                    }
                }

                foreach (var body in FlightGlobals.Bodies)
                {
                    if (api.GetCelestialBodyGuid(body).Equals(guid))
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
            var api = RemoteTechHook.Instance;
            if (target is String)
            {
                String targetString = (String)target;
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
                    foreach (var groundStation in groundStations) {
                        if (targetString.Equals(groundStation)) {
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

            throw new KOSInvalidFieldValueException("Acceptable values are: '" + NoTargetString + "', '" + ActiveVesselString +
                "', name of a ground station, name of a body, name of a vessel, Body, Vessel");
        }

        protected new void SetKSPFieldValue(string suffixName, object newValue)
        {
            if (suffixName.Equals(RTTargetField, StringComparison.InvariantCultureIgnoreCase))
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