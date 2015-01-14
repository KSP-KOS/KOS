﻿using System.Collections.Generic;
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
            AddSuffix("UID", new Suffix<string>(() => vessel.rootPart.uid().ToString()));
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

        public override bool KOSEquals(object other)
        {
            ElementValue that = other as ElementValue;
            if (that == null) return false;
            return this.name.Equals(that.name) && this.parts.Equals(that.parts);
            // @erendrake - I'm not sure if this is correct, because I have no clue what
            // LIST ELEMENTS IN FOO, which is what this is, is actually supposed to mean.
            // So I just made it compare all the member fields, not knowing what it means.
        } 

        public override string ToString()
        {
            return "ELEMENT(" + name + ", " + parts.Count + ")";
        }
    }
}