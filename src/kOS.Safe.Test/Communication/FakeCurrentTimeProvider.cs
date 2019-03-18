using System;
using kOS.Safe.Communication;

namespace kOS.Safe.Test.Communication
{
    public class FakeCurrentTimeProvider : CurrentTimeProvider
    {
        public double FakeTime { get; set; }

        public double CurrentTime()
        {
            return FakeTime;
        }

        public void SetTime(double newTime)
        {
            FakeTime = newTime;
        }

    }
}

