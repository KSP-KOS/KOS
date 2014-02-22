using System.Collections.Generic;
using System.Linq;

namespace kOS.Suffixed
{
    public class ElementValue : SpecialValue
    {
        private readonly string name;
        private readonly IList<global::Part> parts;
        private readonly uint uid;

        public ElementValue(IEnumerable<global::Part> parts)
        {
            this.parts = parts.ToList();
            var vessel = this.parts.First().vessel;
            name = vessel.vesselName;
            uid = vessel.rootPart.uid;
        }

        public override object GetSuffix(string suffixName)
        {
            switch (suffixName)
            {
                case "NAME":
                    return name;
                case "UID":
                    return uid;
                case "PARTCOUNT":
                    return parts.Count;
                case "RESOURCES":
                    return ResourceValue.PartsToList(parts);
            }
            return base.GetSuffix(suffixName);
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