namespace kOS.Safe.Encapsulation.Part
{
    public interface IModuleEngine
    {
        void Activate();
        void Shutdown();
        float ThrustPercentage { get; set; }
        float MaxThrust { get; }
        float AvailableThrust { get; }
        float FinalThrust { get; }
        float FuelFlow { get; }
        float SpecificImpulse { get; }
        float VacuumSpecificImpluse { get; }
        float SeaLevelSpecificImpulse { get; }
        bool Flameout { get; }
        bool Ignition { get; }
        bool AllowRestart { get; }
        bool AllowShutdown { get; }
        bool ThrottleLock { get; }
        float IspAtAtm(double atmPressure);
        float MaxThrustAtAtm(double atmPressure);
        float AvailableThrustAtAtm(double atmPressure);
    }
}