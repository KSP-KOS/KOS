using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace kOS
{
    public class UpdateHandler
    {
        // Using a Dictionary instead of List to prevent duplications.  If an object tries to
        // insert itself more than once into the observer list, it still only gets in the list
        // once and therefore only gets its Update() called once per update.
        // The value of the KeyValuePair, the int, is unused.
        private Dictionary<IUpdateObserver, int> _observers = new Dictionary<IUpdateObserver, int>();
        
        public double CurrentTime { get; private set; }
        public double LastDeltaTime { get; private set; }

        public UpdateHandler()
        {
            CurrentTime = 0;
            LastDeltaTime = 0;
        }

        public void AddObserver(IUpdateObserver observer)
        {
            try 
            {
                _observers.Add(observer, 0);
            }
            catch (ArgumentException)
            {
                // observer is alredy in the list.  Nothing needs to be done.
            }
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
            Dictionary<IUpdateObserver,int> fixedObserverList = new Dictionary<IUpdateObserver,int>(_observers);
            foreach (KeyValuePair<IUpdateObserver,int> observer in fixedObserverList)
            {
                observer.Key.Update(deltaTime);
            }
        }
    }
}
