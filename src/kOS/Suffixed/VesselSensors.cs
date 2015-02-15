using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;

namespace kOS.Suffixed
{
    public class VesselSensors : Structure
    {
        private Vector acceleration = new Vector(0, 0, 0);
        private Vector geeForce = new Vector(0, 0, 0);
        private double kerbolExposure;
        private double temperature;
        private double pressure;

        public VesselSensors(Vessel target)
        {
            FindSensors(target);
            InitializeSuffixes();
        }

        private void InitializeSuffixes()
        {
            AddSuffix("ACC", new Suffix<Vector>(() => acceleration));
            AddSuffix("PRES", new Suffix<double>(() => pressure));
            AddSuffix("TEMP", new Suffix<double>(() => temperature));
            AddSuffix("GRAV", new Suffix<Vector>(() => geeForce));
            AddSuffix("LIGHT", new Suffix<double>(() => kerbolExposure));
        }

        private void FindSensors(Vessel target)
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
                                acceleration = new Vector(target.acceleration);
                                break;
                            case "PRES":
                                pressure = target.staticPressure;
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

        public override string ToString()
        {
            return string.Format("{0} VesselSensor", base.ToString());
        }
    }
}