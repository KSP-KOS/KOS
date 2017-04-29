using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using UnityEngine;

namespace kOS.Suffixed.Widget
{
    [kOS.Safe.Utilities.KOSNomenclature("StyleRectOffset")]
    public class WidgetStyleRectOffset : Structure
    {
        private RectOffset rectOffset;
        public WidgetStyleRectOffset(RectOffset ro)
        {
            rectOffset = ro;
            RegisterInitializer(InitializeSuffixes);
        }

        private void InitializeSuffixes()
        {
            AddSuffix("H", new SetSuffix<ScalarIntValue>(() => rectOffset.left, value => { rectOffset.left = value; rectOffset.right = value; }));
            AddSuffix("V", new SetSuffix<ScalarIntValue>(() => rectOffset.top, value => { rectOffset.top = value; rectOffset.bottom = value; }));
            AddSuffix("LEFT", new SetSuffix<ScalarIntValue>(() => rectOffset.left, value => { rectOffset.left = value; }));
            AddSuffix("RIGHT", new SetSuffix<ScalarIntValue>(() => rectOffset.right, value => { rectOffset.right = value; }));
            AddSuffix("TOP", new SetSuffix<ScalarIntValue>(() => rectOffset.top, value => { rectOffset.top = value; }));
            AddSuffix("BOTTOM", new SetSuffix<ScalarIntValue>(() => rectOffset.bottom, value => { rectOffset.bottom = value; }));
        }

        public override string ToString()
        {
            return "STYLERECTOFFSET";
        }
    }
}
