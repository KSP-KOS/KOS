
namespace kOS.Safe.Encapsulation
{
    /// <summary>
    /// A Pointer class that stores a "path" to a value using keys and indecies.<br/>
    /// Used by the Pointer instructions "CREATEPTR", "GETPTR", "SETPTR". <br/>
    /// </summary>
    [kOS.Safe.Utilities.KOSNomenclature("Pointer")]
    public class PointerValue : Structure
    {
        public readonly List<Structure> Segments;

        public PointerValue(IEnumerable<Structure> segments)
        {
            Segments = new List<Structure>(segments);
        }

        public override string ToString()
        {
            return "<Pointer>";
        }
    }
}