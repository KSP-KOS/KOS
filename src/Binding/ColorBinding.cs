using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using kOS.Suffixed;
using kOS.Execution;

namespace kOS.Binding
{
    [kOSBinding("ksp")]
    public class ColorBinding : Binding
    {
        public override void AddTo(SharedObjects shared)
        {
            _shared = shared;
            // This is only defining the 8 basic primary mixtures that you get from
            // treating the 3 R,G,B values as binary bits (all the way on or all the way off).
            // The in-between values, like orange, brown, and so on, are deliberately left out
            // so we don't eat up too many keywords and deny them to the users as identifiers.
            
            _shared.BindingMgr.AddGetter("WHITE",   delegate(CPU cpu) { return new RgbaColor(1.0f, 1.0f, 1.0f); });
            _shared.BindingMgr.AddGetter("BLACK",   delegate(CPU cpu) { return new RgbaColor(0.0f, 0.0f, 0.0f); });
            _shared.BindingMgr.AddGetter("RED",     delegate(CPU cpu) { return new RgbaColor(1.0f, 0.0f, 0.0f); });
            _shared.BindingMgr.AddGetter("GREEN",   delegate(CPU cpu) { return new RgbaColor(0.0f, 1.0f, 0.0f); });
            _shared.BindingMgr.AddGetter("BLUE",    delegate(CPU cpu) { return new RgbaColor(0.0f, 0.0f, 1.0f); });
            _shared.BindingMgr.AddGetter("YELLOW",  delegate(CPU cpu) { return new RgbaColor(1.0f, 1.0f, 0.0f); });
            _shared.BindingMgr.AddGetter("MAGENTA", delegate(CPU cpu) { return new RgbaColor(1.0f, 0.0f, 1.0f); });
            _shared.BindingMgr.AddGetter("CYAN",    delegate(CPU cpu) { return new RgbaColor(0.0f, 1.0f, 1.0f); });

            // Other synonym spellings repeating the above colors:
            _shared.BindingMgr.AddGetter("PURPLE",  delegate(CPU cpu) { return new RgbaColor(1.0f, 0.0f, 1.0f); });
            _shared.BindingMgr.AddGetter("GRAY",    delegate(CPU cpu) { return new RgbaColor(0.5f, 0.5f, 0.5f); });

        }
    }
}
