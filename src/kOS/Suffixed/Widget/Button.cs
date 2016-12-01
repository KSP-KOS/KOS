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
        public bool isToggle { get; set; }
        public bool isExclusive { get; set; }
        public KOSDelegate onPressed;

        public Button(Box parent, string text) : base(parent, text)
        {
            isToggle = false;
            RegisterInitializer(InitializeSuffixes);
        }

        protected override GUIStyle BaseStyle()
        {
            return isToggle ? HighLogic.Skin.toggle : HighLogic.Skin.button;
        }

        public static Button NewCheckbox(Box parent, string text, bool on)
        {
            var r = new Button(parent, text);
            r.Pressed = on;
            r.PressedVisible = on;
            r.SetToggleMode(true);
            return r;
        }

        public static Button NewRadioButton(Box parent, string text, bool on)
        {
            var r = new Button(parent, text);
            r.Pressed = on;
            r.PressedVisible = on;
            r.SetToggleMode(true);
            r.isExclusive = true;
            return r;
        }

        private void InitializeSuffixes()
        {
            AddSuffix("PRESSED", new SetSuffix<BooleanValue>(() => TakePress(), value => { Pressed = value; Communicate(() => PressedVisible = value); }));
            /* Can't work out how to call kOS code from C# in DoOnPressed() below.
             * AddSuffix("ONPRESSED", new SetSuffix<KOSDelegate>(() => onPressed, value => onPressed = value));
             */
            AddSuffix("SETTOGGLE", new OneArgsSuffix<BooleanValue>(SetToggleMode));
            AddSuffix("EXCLUSIVE", new SetSuffix<BooleanValue>(() => isExclusive, value => isExclusive = value));
        }

        public void SetToggleMode(BooleanValue on)
        {
            if (isToggle != on)
                isToggle = on;
        }

        public bool TakePress()
        {
            var r = Pressed;
            if (!isToggle && Pressed) {
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
            if (isToggle) {
                var newpressed = GUILayout.Toggle(PressedVisible, VisibleContent(), style);
                PressedVisible = newpressed;
                if (isExclusive && newpressed && parent != null) {
                    parent.UnpressVisibleAllBut(this);
                }
                if (Pressed != newpressed) {
                    Communicate(() => Pressed = newpressed);
                    if (newpressed)
                        Communicate(() => DoOnPressed());
                }
            } else {
                if (GUILayout.Toggle(PressedVisible, VisibleContent(), style)) {
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
            if (onPressed != null) {
                UnityEngine.Debug.Log("DoOnPressed: " + onPressed.ToString());
                onPressed.Call(new Structure[0]);
            }
        }

        public override string ToString()
        {
            return "BUTTON(" + VisibleText().Ellipsis(10) + ")";
        }
    }
}
