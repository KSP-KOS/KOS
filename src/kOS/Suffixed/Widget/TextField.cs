using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using UnityEngine;


namespace kOS.Suffixed
{
    [kOS.Safe.Utilities.KOSNomenclature("TextField")]
    public class TextField : Label
    {
        public bool Changed { get; set; }
        public bool Confirmed { get; set; }

        private static GUIStyle toolTipStyle = null;

        public TextField(Box parent, string text) : base(parent,text)
        {
            if (toolTipStyle == null) {
                toolTipStyle = new GUIStyle(HighLogic.Skin.label);
                toolTipStyle.normal.textColor = new Color(0.6f, 0.6f, 0.6f, 0.2f);
            }
            RegisterInitializer(InitializeSuffixes);
        }

        protected override GUIStyle BaseStyle()
        {
            return HighLogic.Skin.textField;
        }

        private void InitializeSuffixes()
        {
            AddSuffix("CHANGED", new SetSuffix<BooleanValue>(() => TakeChange(), value => Changed = value));
            AddSuffix("CONFIRMED", new SetSuffix<BooleanValue>(() => TakeConfirm(), value => Confirmed = value));
        }

        public bool TakeChange()
        {
            bool r = Changed;
            Changed = false;
            return r;
        }

        public bool TakeConfirm()
        {
            bool r = Confirmed;
            Confirmed = false;
            return r;
        }

        int uiID = -1;

        public override void DoGUI()
        {
            if (Event.current.keyCode == KeyCode.Return && GUIUtility.keyboardControl == uiID) {
                Communicate(() => Confirmed = true);
                GUIUtility.keyboardControl = -1;
            }
            uiID = GUIUtility.GetControlID(FocusType.Passive) + 1; // Dirty kludge.
            string newtext = GUILayout.TextField(VisibleText(), Style);
            if (newtext != VisibleText()) {
                SetVisibleText(newtext);
                Changed = true;
            }
            if (newtext == "") {
                GUI.Label(GUILayoutUtility.GetLastRect(), VisibleTooltip(), toolTipStyle);
            }
        }

        public override string ToString()
        {
            return "TEXTFIELD(" + StoredText().Ellipsis(10) + ")";
        }
    }
}
