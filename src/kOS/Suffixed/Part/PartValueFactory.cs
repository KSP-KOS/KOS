using kOS.Safe.Encapsulation;
using System.Collections.Generic;
using System.Linq;

namespace kOS.Suffixed.Part
{
    public class PartValueFactory
    {
        public static ListValue Construct(IEnumerable<global::Part> parts, SharedObjects shared) =>
            ListValue.CreateList(parts.Select(part => Construct(part, shared)));

        public static ListValue<PartValue> ConstructGeneric(IEnumerable<global::Part> parts, SharedObjects shared) =>
            ListValue<PartValue>.CreateList(parts.Select(part => Construct(part, shared)));

        public static PartValue Construct(global::Part part, SharedObjects shared) =>
            VesselTarget.CreateOrGetExisting(shared)[part];
    }
}
