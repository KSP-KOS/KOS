using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using UnityEngine;

namespace kOS.Suffixed
{
    [kOS.Safe.Utilities.KOSNomenclature("StyleState")]
    public class WidgetStyleState : Structure
    {
        private GUIStyleState state;
        public WidgetStyleState(GUIStyleState ss)
        {
            state = ss;
            RegisterInitializer(InitializeSuffixes);
        }

        RgbaColor TextColor
        {
            get
            {
                var c = state.textColor;
                return new RgbaColor(c.r, c.g, c.b, c.a);
            }
            set
            {
                state.textColor = value.Color;
            }
        }

        private void InitializeSuffixes()
        {
            AddSuffix("BG", new SetSuffix<StringValue>(() => "", value => state.background = Widget.GetTexture(value)));
            AddSuffix("TEXTCOLOR", new SetSuffix<RgbaColor>(() => TextColor, value => TextColor = value));
        }

        public override string ToString()
        {
            return "STYLESTATE";
        }
    }
}
