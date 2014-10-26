using System.Collections.Generic;
using System.Linq;

namespace kOS.Safe.Compilation.KS
{
    public class TriggerCollection
    {
        private readonly Dictionary<string, Trigger> triggers = new Dictionary<string, Trigger>();
        private readonly List<Trigger> newTriggers = new List<Trigger>();

        public bool Contains(string triggerIdentifier)
        {
            return triggers.ContainsKey(triggerIdentifier);
        }

        public Trigger GetTrigger(string triggerIdentifier)
        {
            if (triggers.ContainsKey(triggerIdentifier))
            {
                return triggers[triggerIdentifier];
            }
            var triggerObject = new Trigger(triggerIdentifier);
            triggers.Add(triggerIdentifier, triggerObject);
            newTriggers.Add(triggerObject);
            return triggerObject;
        }

        public List<CodePart> GetParts(IEnumerable<Trigger> triggerList)
        {
            return triggerList.Select(triggerObject => triggerObject.GetCodePart()).ToList();
        }

        public List<CodePart> GetParts()
        {
            return GetParts(triggers.Values.ToList());
        }

        public IEnumerable<CodePart> GetNewParts()
        {
            List<CodePart> parts = GetParts(newTriggers);
            newTriggers.Clear();
            return parts;
        }
    }
}