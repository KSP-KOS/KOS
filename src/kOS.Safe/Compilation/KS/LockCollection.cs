using System.Collections.Generic;
using System.Linq;

namespace kOS.Safe.Compilation.KS
{
    public class LockCollection
    {
        private readonly Dictionary<string, Lock> locks = new Dictionary<string, Lock>();
        private readonly List<Lock> newLocks = new List<Lock>();

        public bool Contains(string lockIdentifier)
        {
            return locks.ContainsKey(lockIdentifier);
        }

        public Lock GetLock(string lockIdentifier)
        {
            if (locks.ContainsKey(lockIdentifier))
            {
                return locks[lockIdentifier];
            }
            var lockObject = new Lock(lockIdentifier);
            locks.Add(lockIdentifier, lockObject);
            newLocks.Add(lockObject);
            return lockObject;
        }

        public IEnumerable<Lock> GetLockList()
        {
            return locks.Values.ToList();
        }

        public List<CodePart> GetParts(IEnumerable<Lock> lockList)
        {
            return lockList.Select(lockObject => lockObject.GetCodePart()).ToList();
        }

        public List<CodePart> GetParts()
        {
            return GetParts(locks.Values.ToList());
        }

        public IEnumerable<CodePart> GetNewParts()
        {
            // new locks
            List<CodePart> parts = GetParts(newLocks);

            // updated locks
            foreach (Lock lockObject in locks.Values)
            {
                // if the lock is new then clear the new functions list
                if (newLocks.Contains(lockObject))
                {
                    lockObject.ClearNewFunctions();
                }
                else if (lockObject.HasNewFunctions())
                {
                    // if the lock has new functions then create a new code part for them
                    parts.Add(lockObject.GetNewFunctionsCodePart());
                }
            }

            newLocks.Clear();

            return parts;
        }
    }
}