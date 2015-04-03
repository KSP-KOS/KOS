using System.Collections.Generic;

namespace kOS.Safe
{
    public class UpdateHandler
    {
        // Using a Dictionary instead of List to prevent duplications.  If an object tries to
        // insert itself more than once into the observer list, it still only gets in the list
        // once and therefore only gets its Update() called once per update.
        // The value of the KeyValuePair, the int, is unused.
        private readonly HashSet<IUpdateObserver> observers = new HashSet<IUpdateObserver>();
        private readonly HashSet<IFixedUpdateObserver> fixedObservers = new HashSet<IFixedUpdateObserver>();

        public double CurrentFixedTime { get; private set; }
        public double LastDeltaFixedTime { get; private set; }
        public double CurrentTime { get; private set; }
        public double LastDeltaTime { get; private set; }

        public void AddObserver(IUpdateObserver observer)
        {
            observers.Add(observer);
        }

        public void AddFixedObserver(IFixedUpdateObserver observer)
        {
            fixedObservers.Add(observer);
        }

        public void RemoveObserver(IUpdateObserver observer)
        {
            observers.Remove(observer);
        }

        public void RemoveFixedObserver(IFixedUpdateObserver observer)
        {
            fixedObservers.Remove(observer);
        }

        public void UpdateObservers(double deltaTime)
        {
            LastDeltaTime = deltaTime;
            CurrentTime += deltaTime;
            
            var snapshot = new HashSet<IUpdateObserver>(observers);
            foreach (var observer in snapshot)
            {
                observer.KOSUpdate(deltaTime);
            }
        }

        public void UpdateFixedObservers(double deltaTime)
        {
            LastDeltaFixedTime = deltaTime;
            CurrentFixedTime += deltaTime;
            
            var snapshot = new HashSet<IFixedUpdateObserver>(fixedObservers);
            foreach (var observer in snapshot)
            {
                observer.KOSFixedUpdate(deltaTime);
            }
        }
    }
}
