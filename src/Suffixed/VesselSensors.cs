using System;

namespace kOS.Suffixed
{
    public class VesselSensors : SpecialValue
    {
        private readonly Vector acceleration = new Vector(0, 0, 0);
        private readonly Vector geeForce = new Vector(0, 0, 0);
        private readonly double kerbolExposure;
        private readonly double temperature;
        private readonly double pressure;

        public VesselSensors(Vessel target)
        {
            foreach (var part in target.Parts)
            {
                if (part.State != PartStates.ACTIVE && part.State != PartStates.IDLE) continue;

                foreach (PartModule module in part.Modules)
                {
                    if (module is ModuleEnviroSensor)
                    {
                        switch (module.Fields.GetValue("sensorType").ToString())
                        {
                            case "ACC":
                                acceleration =
                                    new Vector(FlightGlobals.getGeeForceAtPosition(part.transform.position) - target.acceleration);
                                break;
                            case "PRES":
                                pressure = FlightGlobals.getStaticPressure();
                                break;
                            case "TEMP":
                                temperature = part.temperature;
                                break;
                            case "GRAV":
                                geeForce = new Vector(FlightGlobals.getGeeForceAtPosition(part.transform.position));
                                break;
                        }
                    }
                    foreach (var c in part.FindModulesImplementing<ModuleDeployableSolarPanel>())
                    {
                        kerbolExposure += c.sunAOA;
                    }
                }
            }
        }

        public override object GetSuffix(string suffixName)
        {
            switch (suffixName)
            {
                case "ACC":
                    return acceleration;
                case "PRES":
                    return pressure;
                case "TEMP":
                    return temperature;
                case "GRAV":
                    return geeForce;
                case "LIGHT":
                    return kerbolExposure;
            }

            return base.GetSuffix(suffixName);
        }
    }
}