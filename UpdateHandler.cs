using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace kOS
{
    public class UpdateHandler
    {
        private List<IUpdateObserver> _observers = new List<IUpdateObserver>();
        public double CurrentTime { get; private set; }
        public double LastDeltaTime { get; private set; }

        public UpdateHandler()
        {
            CurrentTime = 0;
            LastDeltaTime = 0;
        }

        public void AddObserver(IUpdateObserver observer)
        {
            _observers.Add(observer);
        }

        public void RemoveObserver(IUpdateObserver observer)
        {
            _observers.Remove(observer);
        }

        public void UpdateObservers(double deltaTime)
        {
            LastDeltaTime = deltaTime;
            CurrentTime += deltaTime;

            foreach (IUpdateObserver observer in _observers)
            {
                observer.Update(deltaTime);
            }
        }
    }
}
