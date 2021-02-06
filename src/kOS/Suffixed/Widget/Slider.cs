using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Execution;
using UnityEngine;

namespace kOS.Suffixed.Widget
{
    [kOS.Safe.Utilities.KOSNomenclature("Slider")]
    public class Slider : Widget
    {
        private bool horizontal { get; set; }
        private float val;
        // The only special behaviour Value has is that it will notice when val has
        // changed and if it has, trigger the on change callback hook:
        private float Value
        {
            get { return val; }
            set
            {
                float oldVal = val;
                val = value;
                if (oldVal != val)
                    ScheduleOnChange();
            }
        }
        private float valueVisible { get; set; }
        private float min { get; set; }
        private float max { get; set; }
        private WidgetStyle thumbStyle;
        private UserDelegate UserOnChange { get; set; }

        public Slider(Box parent, bool h_not_v, float v, float from, float to) : base(parent, parent.FindStyle(h_not_v ? "horizontalSlider" : "verticalSlider"))
        {
            RegisterInitializer(InitializeSuffixes);
            horizontal = h_not_v;
            val = v;
            valueVisible = v;
            min = from;
            max = to;
            thumbStyle = parent.FindStyle(horizontal ? "horizontalSliderThumb" : "verticalSliderThumb");
        }

        private void InitializeSuffixes()
        {
            AddSuffix("VALUE", new SetSuffix<ScalarValue>(() => Value, v => { if (Value != v) { Value = v; Communicate(() => valueVisible = v); } }));
            AddSuffix("MIN", new SetSuffix<ScalarValue>(() => min, v => min = v));
            AddSuffix("MAX", new SetSuffix<ScalarValue>(() => max, v => max = v));
            AddSuffix("ONCHANGE", new SetSuffix<UserDelegate>(() => CallbackGetter(UserOnChange), v => UserOnChange = CallbackSetter(v)));
        }

        public override void DoGUI()
        {
            float newvalue;
            myId = GUIUtility.GetControlID(FocusType.Passive);
            string myIdString = myId.ToString();
            GUI.SetNextControlName(myIdString);
            if (horizontal)
                newvalue = GUILayout.HorizontalSlider(valueVisible, min, max, ReadOnlyStyle, thumbStyle.ReadOnly);
            else
                newvalue = GUILayout.VerticalSlider(valueVisible, min, max, ReadOnlyStyle, thumbStyle.ReadOnly);
            if (newvalue != valueVisible) {
                valueVisible = newvalue;
                GUI.FocusControl(myIdString);
                Communicate(() => Value = newvalue);
            }
        }

        /// <summary>
        /// If a callback hook is present for onChange, then fire it off:
        /// </summary>
        private void ScheduleOnChange()
        {
            if (UserOnChange != null)
            {
                if (guiCaused)
                    UserOnChange.TriggerOnFutureUpdate(InterruptPriority.CallbackOnce, new ScalarDoubleValue((double)val));
                else
                    UserOnChange.TriggerOnNextOpcode(InterruptPriority.NoChange, new ScalarDoubleValue((double)val));
            }
        }

        public override string ToString()
        {
            return string.Format("SLIDER({0:0.00})",val);
        }
    }
}
