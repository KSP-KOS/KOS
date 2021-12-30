using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using UnityEngine;

namespace kOS.Suffixed.Widget
{
    [kOS.Safe.Utilities.KOSNomenclature("Spacing")]
    public class Spacing : Widget
    {
        private float amount { get; set; }

        public Spacing(Box parent, float v) : base(parent, null) // spacing has no style associated.
        {
            RegisterInitializer(InitializeSuffixes);
            amount = v;
        }

        private void InitializeSuffixes()
        {
            AddSuffix("AMOUNT", new SetSuffix<ScalarValue>(() => amount, v => amount = v));
        }

        public override void DoGUI()
        {
            GUI.SetNextControlName(myId.ToString());
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
