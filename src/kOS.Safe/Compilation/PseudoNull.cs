using System;

namespace kOS.Safe.Compilation
{
    // Because nulls don't have real Types,
    // use this for a fake "type" to reperesent null:
    public class PseudoNull : IEquatable<object>
    {
        // all instances of PseudoNull should be considered identical:
        public override bool Equals(object o)
        {
            return o is PseudoNull;
        }

        public override int GetHashCode() { return 0; }
        public override string ToString()
        {
             return "<null>";
        }
    }
}
