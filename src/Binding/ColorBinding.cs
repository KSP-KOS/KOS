using kOS.Suffixed;
using kOS.Execution;

namespace kOS.Binding
{
    [kOSBinding("ksp")]
    public class ColorBinding : Binding
    {
        public override void AddTo(SharedObjects shared)
        {
            Shared = shared;
            // This is only defining the 8 basic primary mixtures that you get from
            // treating the 3 R,G,B values as binary bits (all the way on or all the way off).
            // The in-between values, like orange, brown, and so on, are deliberately left out
            // so we don't eat up too many keywords and deny them to the users as identifiers.
            
            Shared.BindingMgr.AddGetter("WHITE", cpu => new RgbaColor(1.0f, 1.0f, 1.0f));
            Shared.BindingMgr.AddGetter("BLACK", cpu => new RgbaColor(0.0f, 0.0f, 0.0f));
            Shared.BindingMgr.AddGetter("RED", cpu => new RgbaColor(1.0f, 0.0f, 0.0f));
            Shared.BindingMgr.AddGetter("GREEN", cpu => new RgbaColor(0.0f, 1.0f, 0.0f));
            Shared.BindingMgr.AddGetter("BLUE", cpu => new RgbaColor(0.0f, 0.0f, 1.0f));
            Shared.BindingMgr.AddGetter("YELLOW", cpu => new RgbaColor(1.0f, 1.0f, 0.0f));
            Shared.BindingMgr.AddGetter("MAGENTA", cpu => new RgbaColor(1.0f, 0.0f, 1.0f));
            Shared.BindingMgr.AddGetter("CYAN", cpu => new RgbaColor(0.0f, 1.0f, 1.0f));

            // Other synonym spellings repeating the above colors:
            Shared.BindingMgr.AddGetter("PURPLE", cpu => new RgbaColor(1.0f, 0.0f, 1.0f));
            Shared.BindingMgr.AddGetter("GRAY", cpu => new RgbaColor(0.5f, 0.5f, 0.5f));

        }
    }
}
