using System.Collections.Generic;

namespace kOS.Suffixed.Part
{
    public class SensorValue : PartValue
    {
        private readonly ModuleEnviroSensor sensor;

        public SensorValue(global::Part part, ModuleEnviroSensor sensor, SharedObjects sharedObj) : base(part,sharedObj)
        {
            this.sensor = sensor;
        }

        public override object GetSuffix(string suffixName)
        {
            switch (suffixName)
            {
                case "ACTIVE":
                    return sensor.sensorActive;
                case "TYPE":
                    return sensor.sensorType;
                case "READOUT":
                    return sensor.readoutInfo;
            }
            return base.GetSuffix(suffixName);
        }

        public override bool SetSuffix(string suffixName, object value)
        {
            switch (suffixName)
            {
                case "ACTIVE":
                    var activeState = value as bool?;
                    if (!activeState.HasValue)
                    {
                        return false;
                    }
                    sensor.sensorActive = activeState.Value;
                    return true;
            }
            return base.SetSuffix(suffixName, value);
        }

        public new static ListValue PartsToList(IEnumerable<global::Part> parts, SharedObjects sharedObj)
        {
            var toReturn = new ListValue();
            foreach (var part in parts)
            {
                foreach (PartModule module in part.Modules)
                {
                    var sensor = module as ModuleEnviroSensor;
                    if (sensor == null) continue;
                    toReturn.Add(new SensorValue(part, sensor, sharedObj));
                }
            }
            return toReturn;
        }
    }
}