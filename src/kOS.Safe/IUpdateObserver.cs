using System;

namespace kOS.Safe
{
    public interface IUpdateObserver : IDisposable
    {
        void Update(double deltaTime);
    }
}