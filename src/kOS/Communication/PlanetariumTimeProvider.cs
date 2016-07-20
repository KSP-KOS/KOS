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
    }
}

