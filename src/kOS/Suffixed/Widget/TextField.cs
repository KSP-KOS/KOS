using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Execution;
using UnityEngine;


namespace kOS.Suffixed.Widget
{
    [kOS.Safe.Utilities.KOSNomenclature("TextField")]
    public class TextField : Label
    {
        private bool changed;
        public bool Changed
        {
            get { return changed; }
            set
            {
                bool oldVal = changed;
                changed = value;
                if (changed && !oldVal)
                    ScheduleOnChange();
            }
        }

        private bool confirmed;
        public bool Confirmed
        {
            get { return confirmed; }
            set
            {
                bool oldVal = confirmed;
                confirmed = value;
                if (confirmed && !oldVal)
                    ScheduleOnConfirm();
            }
        }

        private UserDelegate UserOnChange { get; set; }
        private UserDelegate UserOnConfirm { get; set; }

        private WidgetStyle emptyHintStyle;

        /// <summary>
        /// Tracks Unity's ID of this gui widget for the sake of seeing if the widget has focus.
        /// </summary>
        private int uiID = -1;

        /// <summary>
        /// True if this gui widget had the keyboard focus on the previous OnGUI pass:
        /// </summary>
        private bool hadFocus = false;

        public TextField(Box parent, string text) : base(parent,text,parent.FindStyle("textField"))
        {
            emptyHintStyle = FindStyle("emptyHintStyle");
            RegisterInitializer(InitializeSuffixes);
        }

        private void InitializeSuffixes()
        {
            AddSuffix("CHANGED", new SetSuffix<BooleanValue>(() => TakeChange(), value => Changed = value));
            AddSuffix("CONFIRMED", new SetSuffix<BooleanValue>(() => TakeConfirm(), value => Confirmed = value));
            AddSuffix("ONCHANGE", new SetSuffix<UserDelegate>(() => CallbackGetter(UserOnChange), value => UserOnChange = CallbackSetter(value)));
            AddSuffix("ONCONFIRM", new SetSuffix<UserDelegate>(() => CallbackGetter(UserOnConfirm), value => UserOnConfirm = CallbackSetter(value)));
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

        private void ScheduleOnConfirm()
        {
            if (UserOnConfirm != null)
            {
                if (guiCaused)
                    UserOnConfirm.TriggerOnFutureUpdate(InterruptPriority.CallbackOnce, new StringValue(Text));
                else
                    UserOnConfirm.TriggerOnNextOpcode(InterruptPriority.NoChange, new StringValue(Text));
                Confirmed = false;
            }
        }

        private void ScheduleOnChange()
        {
            if (UserOnChange != null)
            {
                if (guiCaused)
                    UserOnChange.TriggerOnFutureUpdate(InterruptPriority.CallbackOnce, new StringValue(Text));
                else
                    UserOnChange.TriggerOnNextOpcode(InterruptPriority.NoChange, new StringValue(Text));
                Changed = false;
            }
        }

        public override void DoGUI()
        {
            bool shouldConfirm = false;
            if (GUIUtility.keyboardControl == uiID)
            {
                if (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter)
                    shouldConfirm = true;
                hadFocus = true;
            }
            else
            {
                if (hadFocus)
                    shouldConfirm = true;
                hadFocus = false;
            }
            if (shouldConfirm)
            {
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
                GUI.Label(GUILayoutUtility.GetLastRect(), VisibleTooltip(), emptyHintStyle.ReadOnly);
            }
        }

        public override string ToString()
        {
            return "TEXTFIELD(" + StoredText().Ellipsis(10) + ")";
        }
    }
}
