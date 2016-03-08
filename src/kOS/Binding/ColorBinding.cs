using kOS.Safe.Binding;
using kOS.Suffixed;

namespace kOS.Binding
{
    [Binding("ksp")]
    public class ColorBinding : Binding
    {
        public override void AddTo(SharedObjects shared)
        {
            // This is only defining the 8 basic primary mixtures that you get from
            // treating the 3 R,G,B values as binary bits (all the way on or all the way off).
            // The in-between values, like orange, brown, and so on, are deliberately left out
            // so we don't eat up too many keywords and deny them to the users as identifiers.
            
            shared.BindingMgr.AddGetter("WHITE", () => new RgbaColor(1.0f, 1.0f, 1.0f));
            shared.BindingMgr.AddGetter("BLACK", () => new RgbaColor(0.0f, 0.0f, 0.0f));
            shared.BindingMgr.AddGetter("RED", () => new RgbaColor(1.0f, 0.0f, 0.0f));
            shared.BindingMgr.AddGetter("GREEN", () => new RgbaColor(0.0f, 1.0f, 0.0f));
            shared.BindingMgr.AddGetter("BLUE", () => new RgbaColor(0.0f, 0.0f, 1.0f));
            shared.BindingMgr.AddGetter("YELLOW", () => new RgbaColor(1.0f, 1.0f, 0.0f));
            shared.BindingMgr.AddGetter(new [] {"MAGENTA","PURPLE"}, () => new RgbaColor(1.0f, 0.0f, 1.0f));
            shared.BindingMgr.AddGetter("CYAN", () => new RgbaColor(0.0f, 1.0f, 1.0f));
            shared.BindingMgr.AddGetter(new [] {"GREY", "GRAY"}, () => new RgbaColor(0.5f, 0.5f, 0.5f));

        }
    }
}
