using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using UnityEngine;

namespace kOS.Suffixed.Widget
{
    [kOS.Safe.Utilities.KOSNomenclature("Button")]
    public class Button : Label
    {
        private bool pressed;
        public bool Pressed
        {
            get { return pressed; }
            private set { if (value != pressed) { pressed = value; ScheduleCallbacks(); } else { pressed = value; } }
        }

        public bool PressedVisible { get; private set; }
        public bool IsToggle { get; set; }
        public bool IsExclusive { get; set; }
        public UserDelegate UserOnChanged { get ; set; }
        public UserDelegate UserOnPressed { get; set; }
        public UserDelegate UserOnReleased { get; set; }

        public Button(Box parent, string text) : this(parent, text, parent.FindStyle("button"))
        {
        }

        public Button(Box parent, string text, WidgetStyle buttonStyle) : base(parent, text, buttonStyle)
        {
            IsToggle = false;
            RegisterInitializer(InitializeSuffixes);
        }

        public static Button NewCheckbox(Box parent, string text, bool on)
        {
            var r = new Button(parent, text, parent.FindStyle("toggle"));
            r.Pressed = on;
            r.PressedVisible = on;
            r.SetToggleMode(true);
            return r;
        }

        public static Button NewRadioButton(Box parent, string text, bool on)
        {
            var r = new Button(parent, text, parent.FindStyle("toggle"));
            r.Pressed = on;
            r.PressedVisible = on;
            r.SetToggleMode(true);
            r.IsExclusive = true;
            return r;
        }

        private void InitializeSuffixes()
        {
            AddSuffix("PRESSED", new SetSuffix<BooleanValue>(() => TakePress(), value => SetPressed(value)));
            AddSuffix("TOGGLE", new SetSuffix<BooleanValue>(() => IsToggle, value => SetToggleMode(value)));
            AddSuffix("EXCLUSIVE", new SetSuffix<BooleanValue>(() => IsExclusive, value => IsExclusive = value));
            AddSuffix("ONCHANGED", new SetSuffix<UserDelegate>(
                () => UserOnChanged ?? NoDelegate.Instance, value => UserOnChanged = (value is NoDelegate ? null : value) ));
            AddSuffix("ONPRESSED", new SetSuffix<UserDelegate>(
                () => UserOnPressed ?? NoDelegate.Instance, value => UserOnPressed = (value is NoDelegate ? null : value) ));
            AddSuffix("ONRELEASED", new SetSuffix<UserDelegate>(
                () => UserOnReleased ?? NoDelegate.Instance, value => UserOnReleased = (value is NoDelegate ? null : value) ));
        }

        /// <summary>
        /// Should be called whenever the button value gets changed.
        /// If there is a user callback registered to the change event, it
        /// schedules it to get called on the next fixed update.
        /// </summary>
        protected virtual void ScheduleCallbacks()
        {
            // By default, this Button class keeps the button depressed until the script reads its
            // state, and then it pops out because it was read.  Triggering a callback should count
            // as the script "reading" the value, so it should pop out when that happens too.
            bool causeRelease = false;

            // This is being called from inside the Setter of the Pressed property, so
            // the backing field 'pressed' is used here not the property 'Pressed', to
            // avoid any potential strange recursion or threaded timing issues:
            if (UserOnChanged != null)
            {
                UserOnChanged.TriggerNextUpdate(new BooleanValue(pressed));
                if (pressed)
                    causeRelease = true;
            }
            if (UserOnPressed != null && pressed)
            {
                UserOnPressed.TriggerNextUpdate();
                causeRelease = true;
            }
            if (UserOnReleased != null && !pressed)
                UserOnReleased.TriggerNextUpdate();

            if (causeRelease)
            {
                // BE CAREFUL HERE!  This method is invoked during the Property Setter of `Pressed`,
                // and calling TakePress() will cause the Pressed property to get set again (to false).
                // If the flagging above isn't just right, there's a potential for infinite recursion
                // here within the property setter.  (It works only because causeRelease is only true
                // when the value becomes true, and TakePress() will only set it to false.  So they
                // won't recurse back and forth.)
                TakePress();
            }
        }

        public void SetToggleMode(BooleanValue on)
        {
            if (IsToggle != on)
                IsToggle = on;
        }

        public void SetPressed(bool value)
        {
            if (Pressed != value) {
                Pressed = value;
                if (PressedVisible != value) {
                    Communicate(() => PressedVisible = value);
                    if (IsExclusive && value && parent != null) {
                        Communicate(() => { if (parent != null) parent.UnpressVisibleAllBut(this); });
                    }
                }
            }
        }

        public bool TakePress()
        {
            bool r = Pressed;
            if (!IsToggle && Pressed) {
                Pressed = false;
                Communicate(() => SetPressedVisible(false));
            }
            return r;
        }

        public void SetPressedVisible(bool on)
        {
            if (PressedVisible != on) {
                PressedVisible = on;
                if (IsExclusive && PressedVisible && parent != null) {
                    parent.UnpressVisibleAllBut(this);
                }
                Communicate(() => Pressed = on);
            }
        }

        public override void DoGUI()
        {
            if (IsToggle) {
                bool newpressed = GUILayout.Toggle(PressedVisible, VisibleContent(), ReadOnlyStyle);
                if (IsExclusive && !newpressed) return; // stays pressed
                SetPressedVisible(newpressed);
            } else {
                if (GUILayout.Toggle(PressedVisible, VisibleContent(), ReadOnlyStyle)) {
                    if (!PressedVisible) {
                        PressedVisible = true;
                        Communicate(() => Pressed = true);
                    }
                }
            }
        }

        public override string ToString()
        {
            return "BUTTON(" + VisibleText().Ellipsis(10) + ")";
        }
    }
}
