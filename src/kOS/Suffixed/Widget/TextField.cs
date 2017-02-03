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

        private WidgetStyle toolTipStyle;

        public TextField(Box parent, string text) : base(parent,text,parent.FindStyle("textField"))
        {
            toolTipStyle = FindStyle("labelTipOverlay");
            RegisterInitializer(InitializeSuffixes);
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
            string newtext = GUILayout.TextField(VisibleText(), ReadOnlyStyle);
            if (newtext != VisibleText()) {
                SetVisibleText(newtext);
                Changed = true;
            }
            if (newtext == "") {
                GUI.Label(GUILayoutUtility.GetLastRect(), VisibleTooltip(), toolTipStyle.ReadOnly);
            }
        }

        public override string ToString()
        {
            return "TEXTFIELD(" + StoredText().Ellipsis(10) + ")";
        }
    }
}
