using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace kOS.Suffixed
{
    public class VesselSensors : SpecialValue
    {
        Vector acceleration = new Vector(0, 0, 0);
        Single pressure = 0;
        Single temperature = 0;
        Vector geeForce = new Vector(0, 0, 0);
        Single KerbolExposure = 0;

        public VesselSensors(Vessel target)
        {
            foreach (Part part in target.Parts)
            {
                if (part.State == PartStates.ACTIVE || part.State == PartStates.IDLE)
                {
                    foreach (PartModule module in part.Modules)
                    {
                        if (module is ModuleEnviroSensor)
                        {
                            switch (module.Fields.GetValue("sensorType").ToString())
                            {
                                case "ACC":
                                    acceleration = new Vector(FlightGlobals.getGeeForceAtPosition(part.transform.position) - target.acceleration);
                                    break;
                                case "PRES":
                                    pressure = (Single)FlightGlobals.getStaticPressure();
                                    break;
                                case "TEMP":
                                    temperature = part.temperature;
                                    break;
                                case "GRAV":
                                    geeForce = new Vector(FlightGlobals.getGeeForceAtPosition(part.transform.position));
                                    break;
                            }
                        }
                        foreach (ModuleDeployableSolarPanel c in part.FindModulesImplementing<ModuleDeployableSolarPanel>())
                        {
                            KerbolExposure += (Single)c.sunAOA;
                        }
                    }
                }
            }
        }

        public override object GetSuffix(string suffixName)
        {
            if (suffixName == "ACC") return acceleration;
            if (suffixName == "PRES") return pressure;
            if (suffixName == "TEMP") return temperature;
            if (suffixName == "GRAV") return geeForce;
            if (suffixName == "LIGHT") return KerbolExposure;

            return base.GetSuffix(suffixName);
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }
}
