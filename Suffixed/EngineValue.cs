using System.Collections.Generic;
using kOS.Utilities;

namespace kOS.Suffixed
{
    public class SensorValue : PartValue
    {
        private readonly ModuleEnviroSensor sensor;

        public SensorValue(Part part, ModuleEnviroSensor sensor) : base(part)
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
        public new static MixedListValue PartsToList(IEnumerable<Part> parts)
        {
            var toReturn = new MixedListValue();
            foreach (var part in parts)
            {
                foreach (PartModule module in part.Modules)
                {
                    var sensor = module as ModuleEnviroSensor;
                    if (sensor == null) continue;
                    toReturn.Add(new SensorValue(part, sensor));
                }
            }
            return toReturn;
        }
    }

    public class EngineValue : PartValue
    {
        private readonly ModuleEngines engines;

        public EngineValue(Part part, ModuleEngines engines):base(part)
        {
            this.engines = engines;
        }

        public override object GetSuffix(string suffixName)
        {
            switch (suffixName)
            {
                case "THRUST":
                    return engines.maxThrust;
                case "ISP":
                    return engines.realIsp;
            }
            return base.GetSuffix(suffixName);
        }

        public new static MixedListValue PartsToList(IEnumerable<Part> parts)
        {
            var toReturn = new MixedListValue();
            foreach (var part in parts)
            {
                foreach (PartModule module in part.Modules)
                {
                    var engineModule = module as ModuleEngines;
                    if (engineModule == null) continue;

                    if (engineModule.getIgnitionState)
                    {
                        toReturn.Add(new EngineValue(part, engineModule));
                    }
                }
            }
            return toReturn;
        }
    }
}