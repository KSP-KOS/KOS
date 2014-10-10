using System.Collections.Generic;
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
        }

        protected override void InitializeSuffixes()
        {
            base.InitializeSuffixes();
            AddSuffix("ACTIVE", new SetSuffix<ModuleEnviroSensor,bool>(sensor, model => model.sensorActive, (model, value) => model.sensorActive = value));
            AddSuffix("TYPE", new Suffix<ModuleEnviroSensor,string>(sensor, model => model.sensorType));
            AddSuffix("DISPLAY", new Suffix<ModuleEnviroSensor,string>(sensor, model => model.readoutInfo));
            AddSuffix("POWERCONSUMPTION", new Suffix<ModuleEnviroSensor,float>(sensor, model => model.powerConsumption));
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