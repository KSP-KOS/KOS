using System;
using kOS.Safe.Encapsulation.Part;
using UnityEngine;

namespace kOS.Suffixed.Part
{
    public class ModuleEngineAdapter : IModuleEngine
    {
        private enum EngineType
        {
            Engine,
            EngineFx
        }

        private readonly ModuleEnginesFX engineModuleFx;
        private readonly ModuleEngines engineModule;
        private readonly EngineType engineType;

        public ModuleEngineAdapter(ModuleEngines engineModule)
        {
            this.engineModule = engineModule;
            engineType = EngineType.Engine;
        }

        public ModuleEngineAdapter(ModuleEnginesFX engineModuleFx)
        {
            this.engineModuleFx = engineModuleFx;
            engineType = EngineType.EngineFx;
        }

        public void Activate()
        {
            switch (engineType)
            {
                case EngineType.Engine:
                    engineModule.Activate();
                    break;
                case EngineType.EngineFx:
                    engineModuleFx.Activate();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void Shutdown()
        {
            switch (engineType)
            {
                case EngineType.Engine:
                    engineModule.Shutdown();
                    break;
                case EngineType.EngineFx:
                    engineModuleFx.Shutdown();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public float ThrustPercentage
        {
            get
            {
                switch (engineType)
                {
                    case EngineType.Engine:
                        return engineModule.thrustPercentage;

                    case EngineType.EngineFx:
                        return engineModuleFx.thrustPercentage;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            set
            {
                switch (engineType)
                {
                    case EngineType.Engine:
                        engineModule.thrustPercentage = value;
                        break;

                    case EngineType.EngineFx:
                        engineModuleFx.thrustPercentage = value;
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public float MaxThrust
        {
            get
            {
                switch (engineType)
                {
                    case EngineType.Engine:
                        return (float)GetEngineMaxThrust(engineModule);
                        //return engineModule.maxThrust;

                    case EngineType.EngineFx:
                        return (float)GetEngineMaxThrust(engineModuleFx);
                        //return engineModuleFx.maxThrust;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public float AvailableThrust
        {
            get
            {
                switch (engineType)
                {
                    case EngineType.Engine:
                        return (float)GetEngineThrust(engineModule);
                    //return engineModule.maxThrust;

                    case EngineType.EngineFx:
                        return (float)GetEngineThrust(engineModuleFx);
                    //return engineModuleFx.maxThrust;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public float FinalThrust
        {
            get
            {
                switch (engineType)
                {
                    case EngineType.Engine:
                        return engineModule.finalThrust;

                    case EngineType.EngineFx:
                        return engineModuleFx.finalThrust;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
        public static double GetEngineMaxThrust(ModuleEngines engine, float throttle = 1.0f)
        {
            if (engine != null)
            {
                if (!engine.isOperational) return 0.0;
                float flowMod = (float)(engine.part.atmDensity / 1.225f);
                float velMod = 1.0f;
                if (engine.atmChangeFlow && engine.atmCurve != null)
                {
                    flowMod = engine.atmCurve.Evaluate((float)(engine.part.atmDensity / 1.225));
                }
                if (engine.velCurve != null)
                {
                    velMod = velMod * engine.velCurve.Evaluate((float)engine.vessel.mach);
                }
                // thrust is modified fuel flow rate times isp time g times the velocity modifier for jet engines (as of KSP 1.0)
                return Mathf.Lerp(engine.minFuelFlow, engine.maxFuelFlow, throttle) * flowMod * engine.atmosphereCurve.Evaluate((float)engine.part.staticPressureAtm) * engine.g * velMod;
                //thrust += engine.GetCurrentThrust();
            }
            else return 0.0;
        }
        public static double GetEngineThrust(ModuleEngines engine, float throttle = 1.0f)
        {
            if (engine != null)
            {
                if (!engine.isOperational) return 0.0;
                float flowMod = (float)(engine.part.atmDensity / 1.225f);
                float velMod = 1.0f;
                if (engine.atmChangeFlow && engine.atmCurve != null)
                {
                    flowMod *= engine.atmCurve.Evaluate((float)(engine.part.atmDensity / 1.225));
                }
                if (engine.velCurve != null)
                {
                    velMod = velMod * engine.velCurve.Evaluate((float)engine.vessel.mach);
                }
                
                // thrust is modified fuel flow rate times isp time g times the velocity modifier for jet engines (as of KSP 1.0)
                return Mathf.Lerp(engine.minFuelFlow, engine.maxFuelFlow, throttle * engine.thrustPercentage / 100.0f) * flowMod * engine.atmosphereCurve.Evaluate((float)engine.part.staticPressureAtm) * engine.g * velMod;
                //thrust += engine.GetCurrentThrust();
            }
            else return 0.0;
        }

        public float FuelFlow
        {
            get
            {
                switch (engineType)
                {
                    case EngineType.Engine:
                        return engineModule.fuelFlowGui;

                    case EngineType.EngineFx:
                        return engineModuleFx.fuelFlowGui;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public float SpecificImpulse
        {
            get
            {
                switch (engineType)
                {
                    case EngineType.Engine:
                        return engineModule.realIsp;

                    case EngineType.EngineFx:
                        return engineModuleFx.realIsp;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
        public float VacuumSpecificImpluse
        {
            get
            {
                switch (engineType)
                {
                    case EngineType.Engine:
                        return engineModule.atmosphereCurve.Evaluate(0);

                    case EngineType.EngineFx:
                        return engineModuleFx.atmosphereCurve.Evaluate(0);

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
        public float SeaLevelSpecificImpulse
        {
            get
            {
                switch (engineType)
                {
                    case EngineType.Engine:
                        return engineModule.atmosphereCurve.Evaluate(1);

                    case EngineType.EngineFx:
                        return engineModuleFx.atmosphereCurve.Evaluate(1);

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public bool Flameout
        {
            get
            {
                switch (engineType)
                {
                    case EngineType.Engine:
                        return engineModule.flameout;

                    case EngineType.EngineFx:
                        return engineModuleFx.flameout;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public bool Ignition
        {
            get
            {
                switch (engineType)
                {
                    case EngineType.Engine:
                        return engineModule.getIgnitionState;

                    case EngineType.EngineFx:
                        return engineModuleFx.getIgnitionState;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public bool AllowRestart
        {
            get
            {
                switch (engineType)
                {
                    case EngineType.Engine:
                        return engineModule.allowRestart;

                    case EngineType.EngineFx:
                        return engineModuleFx.allowRestart;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public bool AllowShutdown
        {
            get
            {
                switch (engineType)
                {
                    case EngineType.Engine:
                        return engineModule.allowShutdown;

                    case EngineType.EngineFx:
                        return engineModuleFx.allowShutdown;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public bool ThrottleLock
        {
            get
            {
                switch (engineType)
                {
                    case EngineType.Engine:
                        return engineModule.throttleLocked;

                    case EngineType.EngineFx:
                        return engineModuleFx.throttleLocked;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}