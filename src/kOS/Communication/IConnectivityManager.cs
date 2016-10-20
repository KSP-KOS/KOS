using System;

namespace kOS.Communication
{
    public interface IConnectivityManager
    {
        double GetDelay(Vessel vessel1, Vessel vessel2);
        double GetDelayToHome(Vessel vessel);
        double GetDelayToControl(Vessel vessel);
        bool HasConnectionToHome(Vessel vessel);
        bool HasControl(Vessel vessel);
        bool HasConnection(Vessel vessel1, Vessel vessel2);
        bool IsEnabled { get; }
    }
}
