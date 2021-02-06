using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Execution;
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
        public UserDelegate UserOnToggle { get ; set; }
        public UserDelegate UserOnClick { get ; set; }

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
            AddSuffix("PRESSED", new SetSuffix<BooleanValue>(GetPressed, value => SetPressed(value)));
            AddSuffix("TAKEPRESS", new Suffix<BooleanValue>(() => new BooleanValue(TakePress())));
            AddSuffix("TOGGLE", new SetSuffix<BooleanValue>(() => IsToggle, value => SetToggleMode(value)));
            AddSuffix("EXCLUSIVE", new SetSuffix<BooleanValue>(() => IsExclusive, value => IsExclusive = value));
            AddSuffix("ONTOGGLE", new SetSuffix<UserDelegate>(() => CallbackGetter(UserOnToggle), value => UserOnToggle = CallbackSetter(value)));
            AddSuffix("ONCLICK", new SetSuffix<UserDelegate>(() => CallbackGetter(UserOnClick), value => UserOnClick = CallbackSetter(value)));
        }

        /// <summary>
        /// Should be called whenever the button value gets changed.
        /// If there is a user callback registered to the toggle event, it
        /// schedules it to get called on the next fixed update.
        /// </summary>
        protected virtual void ScheduleCallbacks()
        {
            // By default, this Button class keeps the button depressed until the script reads its
            // state, and then it pops out because it was read.  Triggering a callback should count
            // as the script "reading" the value, so it should pop out when that happens too.
            bool causeRelease = false;

            // DO NOT USE PROPERTY "Pressed" in this method!
            // USE backing Field "pressed" instead!!!
            //
            // This is being called from inside the Setter of the Pressed property, so
            // the backing field 'pressed' is used here not the property 'Pressed', to
            // avoid any potential strange recursion or threaded timing issues:

            if (UserOnToggle != null)
            {
                if (guiCaused)
                    UserOnToggle.TriggerOnFutureUpdate(InterruptPriority.CallbackOnce, new BooleanValue(pressed));
                else
                    UserOnToggle.TriggerOnNextOpcode(InterruptPriority.NoChange, new BooleanValue(pressed));
            }

            if (parent != null && parent.UserOnRadioChange != null)
            {
                // For a radio button set, whichever button became true will
                // also cause the parent box to fire the change event hook.
                // (Don't fire it for the button that became false or it will fire
                // twice per change:)
                if (IsExclusive && IsToggle && pressed)
                    parent.ScheduleOnRadioChange(this);
            }

            // Toggles generate clicks on every button state change, while non-toggle buttons
            // should only generate click events on the button-goes-in state,
            // not the button-goes-out state that should auto-activate when it's read:
            if (UserOnClick != null && (IsToggle || pressed))
            {
                if (guiCaused)
                    UserOnClick.TriggerOnNextOpcode(InterruptPriority.CallbackOnce);
                else
                    UserOnClick.TriggerOnNextOpcode(InterruptPriority.NoChange);

                if (!IsToggle)
                    causeRelease = true;
            }

            // <---- More callback triggers would go here if we add them later

            // Don't actually cause the release of the button until here
            // at the bottom after all the hooks have fired using the value prior to release:
            if (causeRelease)
            {
                // TakePress uses the property Pressed, not the field pressed, meaning it
                // can cause a recursive reaction where it calls this method again.
                // Be careful to keep the conditions above for UserOnClick just right so
                // the release won't re-trigger the onclick.
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

        public BooleanValue GetPressed()
        {
            return Pressed;
        }

        public bool TakePress()
        {
            bool r = Pressed;
            if (!IsToggle && Pressed){
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
            // Toggles and one-shot buttons are handled differently.
            // Toggles stay pressed until clicked again.
            // one-shot buttons release as soon as the click is noticed by the script.
            if (IsToggle) {
                myId = GUIUtility.GetControlID(FocusType.Passive);
                string myIdString = myId.ToString();
                GUI.SetNextControlName(myIdString);
                bool newpressed = GUILayout.Toggle(PressedVisible, VisibleContent(), ReadOnlyStyle);
                if (IsExclusive && !newpressed) return; // stays pressed
                if (newpressed != pressed) // if it just toggled on or toggled off 
                    GUI.FocusControl(myIdString);
                SetPressedVisible(newpressed);
            } else {
                myId = GUIUtility.GetControlID(FocusType.Passive);
                string myIdString = myId.ToString();
                GUI.SetNextControlName(myIdString);
                if (GUILayout.Toggle(PressedVisible, VisibleContent(), ReadOnlyStyle)) {
                    if (!PressedVisible) {
                        PressedVisible = true;
                        GUI.FocusControl(myIdString);
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
