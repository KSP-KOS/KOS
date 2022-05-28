using System;
using kOS.Safe.Communication;

namespace kOS.Communication
{
    public class PlanetariumTimeProvider : CurrentTimeProvider
    {
        public double CurrentTime()
        {
            return Planetarium.GetUniversalTime();
        }

        // a dummy call required by CurrentTimeProvider interface.
        public void SetTime(double ignoreMe)
        {
        }
    }
}

