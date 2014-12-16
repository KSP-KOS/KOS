using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Utilities;

namespace kOS.Suffixed
{
    public class ElementValue : Structure
    {
        private readonly string name;
        private readonly IList<global::Part> parts;

        public ElementValue(IEnumerable<global::Part> parts)
        {
            this.parts = parts.ToList();
            var vessel = this.parts.First().vessel;
            name = vessel.vesselName;

            AddSuffix("NAME", new Suffix<string>(() => vessel.vesselName));
            AddSuffix("UID", new Suffix<uint>(() => vessel.rootPart.uid()));
            AddSuffix("PARTCOUNT", new Suffix<int>(() => parts.Count()));
            AddSuffix("PARTS", new Suffix<ListValue>(() => PartsToList(parts)));
        }

        public static ListValue PartsToList(IEnumerable<global::Part> parts)
        {
            var toReturn = new ListValue();

            foreach (var flightParts in parts.GroupBy(p => p.missionID))
            {
                toReturn.Add(new ElementValue(flightParts));
            }
            return toReturn;
        }

        public override string ToString()
        {
            return "ELEMENT(" + name + ", " + parts.Count + ")";
        }
    }
}