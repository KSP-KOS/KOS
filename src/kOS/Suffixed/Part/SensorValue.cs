﻿using System.Collections.Generic;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;

namespace kOS.Suffixed.Part
{
    public class SensorValue : PartValue
    {
        private readonly ModuleEnviroSensor sensor;

        public SensorValue(global::Part part, ModuleEnviroSensor sensor, SharedObjects sharedObj) : base(part,sharedObj)
        {
            this.sensor = sensor;
            SensorInitializeSuffixes();
        }

        private void SensorInitializeSuffixes()
        {
            AddSuffix("ACTIVE", new SetSuffix<bool>(() => sensor.sensorActive, value => sensor.sensorActive = value));
            AddSuffix("TYPE", new Suffix<string>(() => sensor.sensorType));
            AddSuffix("DISPLAY", new Suffix<string>(() => sensor.readoutInfo));
            AddSuffix("POWERCONSUMPTION", new Suffix<float>(() => sensor.powerConsumption));
            AddSuffix("TOGGLE", new NoArgsSuffix(() => sensor.Toggle()));
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