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
            
            // Iterate over a frozen snapshot of _observers rather than  _observers itself,
            // because _observers can be altered during the course of the loop:
            List<IUpdateObserver> fixedObserverList = new List<IUpdateObserver>(_observers);
            foreach (IUpdateObserver observer in fixedObserverList)
            {
                observer.Update(deltaTime);
            }
        }
    }
}
