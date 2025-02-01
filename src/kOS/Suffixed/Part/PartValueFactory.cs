using kOS.Safe.Encapsulation;
using System.Collections.Generic;
using System.Linq;

namespace kOS.Suffixed.Part
{
    public class PartValueFactory
    {
        // Any time that we create a list, we have to be very careful about null references
        // because ListValue will attempt to assert that the value must 
        public static ListValue Construct(IEnumerable<global::Part> parts, SharedObjects shared) {
            return ListValue.CreateList(parts.Select(part => Construct(part, shared)).Where(p => p != null));
        }

        public static ListValue ConstructGeneric(IEnumerable<global::Part> parts, SharedObjects shared) {
            return new ListValue(parts.Select(part => Construct(part, shared)).Where(p => p != null));
        }

        public static PartValue Construct(global::Part part, SharedObjects shared) {
            return VesselTarget.CreateOrGetExisting(part.vessel, shared)[part];
        }
    }
}
