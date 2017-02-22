using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using UnityEngine;

namespace kOS.Suffixed.Widget
{
    [kOS.Safe.Utilities.KOSNomenclature("Button")]
    public class Button : Label
    {
        public bool Pressed { get; private set; }
        public bool PressedVisible { get; private set; }
        public bool IsToggle { get; set; }
        public bool IsExclusive { get; set; }
        public KOSDelegate OnPressed;

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
            /* Can't work out how to call kOS code from C# in DoOnPressed() below.
             * AddSuffix("ONPRESSED", new SetSuffix<KOSDelegate>(() => onPressed, value => onPressed = value));
             */
            AddSuffix("TOGGLE", new SetSuffix<BooleanValue>(() => IsToggle, value => SetToggleMode(value)));
            AddSuffix("EXCLUSIVE", new SetSuffix<BooleanValue>(() => IsExclusive, value => IsExclusive = value));
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
                        Communicate(() => DoOnPressed());
                    }
                }
            }
        }

        private void DoOnPressed()
        {
            // Not used currently - we can't call kOS code like this.
            if (OnPressed != null) {
                OnPressed.CallPassingArgs(new Structure[0]);
            }
        }

        public override string ToString()
        {
            return "BUTTON(" + VisibleText().Ellipsis(10) + ")";
        }
    }
}
