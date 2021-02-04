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

        // For all TextField widgets, this counter hands out unique ID's.
        private static int highestIdSoFar = 0;

        private int myId;
        private string myIdName = null;

        public TextField(Box parent, string text) : base(parent,text,parent.FindStyle("textField"))
        {
            emptyHintStyle = FindStyle("emptyHintStyle");
            RegisterInitializer(InitializeSuffixes);
            AssignMyIdName();
        }

        private void AssignMyIdName()
        {
            if (myIdName == null)
            {
                myId = ++highestIdSoFar;
                // Placing the unique part of the string (the number) first rather
                // than last, to speed up string compares by discovering "does not equal"
                // sooner.  This is because this will get string-compared frequently in OnGUI().
                myIdName = string.Format("{0}_kOSTF", myId);
            }
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
            // This condition is hypothetically impossible as the constructor should have done this:
            if (myIdName == null)
                AssignMyIdName();

            System.Console.WriteLine(string.Format("eraseme: TextField DoGUI - myIdName={0}, name of focused={1}", myIdName, GUI.GetNameOfFocusedControl()));
            bool shouldConfirm = false;
            if (GUI.GetNameOfFocusedControl().Equals(myIdName))
            {
                System.Console.WriteLine("eraseme: TextField has keyboard focus. setting hadfocus to true.");
                if (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter)
                    shouldConfirm = true;
                hadFocus = true;
            }
            else
            {
                System.Console.WriteLine("eraseme: TextField has lost keyboard focus. setting hadfocus to false.");
                if (hadFocus)
                    shouldConfirm = true;
                hadFocus = false;
            }
            if (shouldConfirm)
            {
                System.Console.WriteLine("eraseme: going to fire OnConfirm");
                Communicate(() => Confirmed = true);
            }
            
            GUI.SetNextControlName(myIdName);
            string newtext = GUILayout.TextField(VisibleText(), ReadOnlyStyle);

            if (newtext != VisibleText()) {
                SetVisibleText(newtext);
                Changed = true;
            }
            if (newtext == "") {
                GUI.Label(GUILayoutUtility.GetLastRect(), VisibleTooltip(), emptyHintStyle.ReadOnly);
            }
            System.Console.WriteLine("eraseme: TextField DoGUI ending.");
        }

        public override string ToString()
        {
            return "TEXTFIELD(" + StoredText().Ellipsis(10) + ")";
        }
    }
}
