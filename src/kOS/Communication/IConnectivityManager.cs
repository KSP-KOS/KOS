using System;

namespace kOS.Communication
{
    public interface IConnectivityManager
    {
        bool IsEnabled { get; }
        bool NeedAutopilotResubscribe { get; }
        double GetDelay(Vessel vessel1, Vessel vessel2);
        double GetDelayToHome(Vessel vessel);
        double GetDelayToControl(Vessel vessel);
        bool HasConnectionToHome(Vessel vessel);
        bool HasControl(Vessel vessel);
        bool HasConnection(Vessel vessel1, Vessel vessel2);
        void AddAutopilotHook(Vessel vessel, FlightInputCallback hook);
        void RemoveAutopilotHook(Vessel vessel, FlightInputCallback hook);

    }
}
