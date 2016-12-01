using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using UnityEngine;

namespace kOS.Suffixed
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
            AddSuffix("PRESSED", new SetSuffix<BooleanValue>(() => TakePress(), value => { Pressed = value; Communicate(() => PressedVisible = value); }));
            /* Can't work out how to call kOS code from C# in DoOnPressed() below.
             * AddSuffix("ONPRESSED", new SetSuffix<KOSDelegate>(() => onPressed, value => onPressed = value));
             */
            AddSuffix("SETTOGGLE", new OneArgsSuffix<BooleanValue>(SetToggleMode));
            AddSuffix("EXCLUSIVE", new SetSuffix<BooleanValue>(() => IsExclusive, value => IsExclusive = value));
        }

        public void SetToggleMode(BooleanValue on)
        {
            if (IsToggle != on)
                IsToggle = on;
        }

        public bool TakePress()
        {
            bool r = Pressed;
            if (!IsToggle && Pressed) {
                Pressed = false;
                Communicate(() => PressedVisible = false);
            }
            return r;
        }

        public void SetPressedVisible(bool on)
        {
            PressedVisible = on;
            if (PressedVisible != on)
                Communicate(() => Pressed = on);
        }

        public override void DoGUI()
        {
            if (IsToggle) {
                bool newpressed = GUILayout.Toggle(PressedVisible, VisibleContent(), ReadOnlyStyle);
                PressedVisible = newpressed;
                if (IsExclusive && newpressed && parent != null) {
                    parent.UnpressVisibleAllBut(this);
                }
                if (Pressed != newpressed) {
                    Communicate(() => Pressed = newpressed);
                    if (newpressed)
                        Communicate(() => DoOnPressed());
                }
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
            UnityEngine.Debug.Log("DoOnPressed");
            if (OnPressed != null) {
                UnityEngine.Debug.Log("DoOnPressed: " + OnPressed.ToString());
                OnPressed.Call(new Structure[0]);
            }
        }

        public override string ToString()
        {
            return "BUTTON(" + VisibleText().Ellipsis(10) + ")";
        }
    }
}
