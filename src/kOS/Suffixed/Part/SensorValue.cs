using System.Collections.Generic;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;

namespace kOS.Suffixed.Part
{
    [kOS.Safe.Utilities.KOSNomenclature("Sensor")]
    public class SensorValue : PartValue
    {
        private readonly ModuleEnviroSensor sensor;

        /// <summary>
        /// Do not call! VesselTarget.ConstructPart uses this, would use `friend VesselTarget` if this was C++!
        /// </summary>
        internal SensorValue(SharedObjects shared, global::Part part, PartValue parent, DecouplerValue decoupler, ModuleEnviroSensor sensor) :
            base(shared, part, parent, decoupler)
        {
            this.sensor = sensor;
            RegisterInitializer(SensorInitializeSuffixes);
        }

        private void SensorInitializeSuffixes()
        {
            AddSuffix("ACTIVE", new SetSuffix<BooleanValue>(() => sensor.sensorActive, value => sensor.sensorActive = value));
            AddSuffix("TYPE", new Suffix<StringValue>(() => sensor.sensorType.ToString()));
            AddSuffix("DISPLAY", new Suffix<StringValue>(() => sensor.readoutInfo));
            AddSuffix("POWERCONSUMPTION", new Suffix<ScalarValue>(GetPowerConsumption));
            AddSuffix("TOGGLE", new NoArgsVoidSuffix(() => sensor.Toggle()));
        }

        public static ListValue PartsToList(IEnumerable<global::Part> parts, SharedObjects sharedObj)
        {
            var toReturn = new ListValue();
            var vessel = VesselTarget.CreateOrGetExisting(sharedObj);
            foreach (var part in parts)
            {
                if(part.Modules.Contains<ModuleEnviroSensor>())
                    toReturn.Add(vessel[part]);
            }
            return toReturn;
        }

        public ScalarValue GetPowerConsumption()
        {
            if (sensor.resHandler != null)
            {
                return sensor.resHandler.GetAverageInput();
            }
            return 0;
        }
    }
}
