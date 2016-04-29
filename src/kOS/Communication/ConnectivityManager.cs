using System;

namespace kOS.Communication
{
    public interface ConnectivityManager
    {
        double GetDelay(Vessel vessel1, Vessel vessel2);
    }
}
