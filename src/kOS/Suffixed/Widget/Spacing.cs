using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using UnityEngine;

namespace kOS.Suffixed
{
    [kOS.Safe.Utilities.KOSNomenclature("Spacing")]
    public class Spacing : Widget
    {
        public float amount { get; set; }

        public Spacing(Box parent, float v) : base(parent)
        {
            RegisterInitializer(InitializeSuffixes);
            amount = v;
        }

        protected override GUIStyle BaseStyle()
        {
            return HighLogic.Skin.label; // not that changing a spacing style makes any sense.
        }

        private void InitializeSuffixes()
        {
            AddSuffix("AMOUNT", new SetSuffix<ScalarValue>(() => amount, v => amount = v));
        }

        public override void DoGUI()
        {
            if (amount < 0)
                GUILayout.FlexibleSpace();
            else
                GUILayout.Space(amount);
        }

        public override string ToString()
        {
            return "SPACING(" + amount + ")";
        }
    }
}
