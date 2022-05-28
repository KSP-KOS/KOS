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
            myId = GUIUtility.GetControlID(FocusType.Keyboard);
            string myIdString = myId.ToString();
            GUI.SetNextControlName(myIdString);
            string newtext = GUILayout.TextField(VisibleText(), ReadOnlyStyle);
            if (myId >= 0 && myIdString != null) // skips on the first pass, and on in-between OnGUI passes where sometimes the control Id's are -1
            {
                if (GUI.GetNameOfFocusedControl().Equals(myIdString))
                {
                    Event thisEvent = Event.current;
                    if ((thisEvent.keyCode == KeyCode.Return || thisEvent.keyCode == KeyCode.KeypadEnter)
                        &&
                        // Next condition is because Unity populates the keyCode even when the event has nothing to do 
                        // with keypresses, like on the repaint and layout events that constantly fire every time it
                        // paints the window.  If we do thisEvent.Use() when the event is a repaint or layout instead of
                        // an actual key event, then Unity aborts drawing the window and it goes away from the screen:
                       (thisEvent.type == EventType.KeyDown || thisEvent.type == EventType.Used))
                    {
                        shouldConfirm = true;
                        thisEvent.Use();
                    }
                    hadFocus = true;
                }
                else
                {
                    if (hadFocus)
                        shouldConfirm = true;
                    hadFocus = false;
                }
            }
            if (shouldConfirm)
            {
                Communicate(() => Confirmed = true);
            }


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
