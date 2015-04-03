using System;

namespace kOS.Safe
{
    public interface IUpdateObserver : IDisposable
    {
        void KOSUpdate(double deltaTime);
    }

    public interface IFixedUpdateObserver : IDisposable
    {
        void KOSFixedUpdate(double deltaTime);
    }
}