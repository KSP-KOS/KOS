using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;

namespace kOS.Suffixed
{
    [kOS.Safe.Utilities.KOSNomenclature("VesselSensors")]
    public class VesselSensors : Structure
    {
        private Vessel vessel;


        public VesselSensors(Vessel target)
        {
            vessel = target;
            InitializeSuffixes();
        }

        private void InitializeSuffixes()
        {
            AddSuffix("ACC", new Suffix<Vector>(() => GetSensorVectorValue("ACC")));
            AddSuffix("PRES", new Suffix<ScalarValue>(() => GetSensorDoubleValue("PRES")));
            AddSuffix("TEMP", new Suffix<ScalarValue>(() => GetSensorDoubleValue("TEMP")));
            AddSuffix("GRAV", new Suffix<Vector>(() => GetSensorVectorValue("GRAV")));
            AddSuffix("LIGHT", new Suffix<ScalarValue>(() => GetSunLightValue()));
        }

        private Vector GetSensorVectorValue (string sensorType)
        {
            foreach (var part in vessel.Parts)
            {
                if (part.State != PartStates.ACTIVE && part.State != PartStates.IDLE) continue;

                foreach (PartModule module in part.Modules)
                {
                    if (module is ModuleEnviroSensor)
                    {
                        var moduleSensorType = module.Fields.GetValue("sensorType").ToString();
                            if (moduleSensorType != sensorType) continue;
                        switch (moduleSensorType)
                        {
                            case "ACC":
                                return new Vector(vessel.acceleration);
                            case "GRAV":
                                return new Vector(FlightGlobals.getGeeForceAtPosition(part.transform.position));
                                
                        }
                    }
                }
            }
            throw new KOSException("Cannot find sensor for " + sensorType);
        }

        private double GetSensorDoubleValue(string sensorType)
        {
            foreach (var part in vessel.Parts)
            {
                if (part.State != PartStates.ACTIVE && part.State != PartStates.IDLE) continue;

                foreach (PartModule module in part.Modules)
                {
                    if (module is ModuleEnviroSensor)
                    {
                        var moduleSensorType = module.Fields.GetValue("sensorType").ToString();
                        if (moduleSensorType != sensorType) continue;
                        switch (moduleSensorType)
                        {
                           
                            case "PRES":
                                return vessel.staticPressurekPa;
                            case "TEMP":
                                return part.temperature;
                                
                            
                        }
                    }
                    
                }
            }
            throw new KOSException("Cannot find sensor for " + sensorType);
        }
        private double GetSunLightValue()
        {
            double kerbolExposure = 0;
            foreach (var part in vessel.Parts)
            {
                foreach (var c in part.FindModulesImplementing<ModuleDeployableSolarPanel>())
                {
                    kerbolExposure += c.sunAOA;
                
                }
            }
            return kerbolExposure;
        }
        


        public override string ToString()
        {
            return string.Format("{0} VesselSensor", base.ToString());
        }
    }
}