using System;
using kOS.Safe.Encapsulation.Part;

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
                        return engineModule.maxThrust;

                    case EngineType.EngineFx:
                        return engineModuleFx.maxThrust;

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
        public float SeaLeveSpecificImpulse
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