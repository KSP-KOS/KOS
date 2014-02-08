using System.Collections.Generic;

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

        public new static ListValue PartsToList(IEnumerable<Part> parts)
        {
            var toReturn = new ListValue();
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

        public EngineValue(Part part, ModuleEngines engines) : base(part)
        {
            this.engines = engines;
        }

        public override object GetSuffix(string suffixName)
        {
            switch (suffixName)
            {
                case "MAXTHRUST":
                    return engines.maxThrust;
                case "THRUST":
                    return engines.finalThrust;
                case "FUELFLOW":
                    return engines.fuelFlowGui;
                case "ISP":
                    return engines.realIsp;
                case "FLAMEOUT":
                    return engines.getFlameoutState;
                case "IGNITION":
                    return engines.getIgnitionState;
            }
            return base.GetSuffix(suffixName);
        }

        public new static ListValue PartsToList(IEnumerable<Part> parts)
        {
            var toReturn = new ListValue();
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