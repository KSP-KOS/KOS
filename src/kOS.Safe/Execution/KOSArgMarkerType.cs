using System;

namespace kOS.Safe.Execution
{
    /// <summary>
    /// ArgMarkerType literally serves no purpose whatsoever other
    /// than to just be a mark of where on the stack is the bottom of
    /// the aruments to something.  If an object is of this type, then
    /// that means it's the argument botttom marker.
    /// </summary>
    public class KOSArgMarkerType
    {
        public override string ToString()
        {
            return "_KOSArgMarker_";
        }
    }
}
