using System;
using LibNoise.Unity.Operator;

namespace kOS.Suffixed
{
    public class FlightControl : SpecialValue , IDisposable 
    {
        private int yaw = 0;
        private int pitch = 0;
        private int roll = 0;
        private int xTranslate = 0;
        private int yTranslate = 0;
        private int zTranslate = 0;
        public Vessel Target { get; private set; }

        public FlightControl(Vessel target)
        {
            Target = target;
            Target.OnFlyByWire += OnFlyByWire;
        }

        public override object GetSuffix(string suffixName)
        {
            switch (suffixName)
            {
                case "YAW":
                    return yaw;
                case "PITCH":
                    return pitch;
                case "ROLL":
                    return roll;
                case "XTRANSLATE":
                    return xTranslate;
                case "YTRANSLATE":
                    return yTranslate;
                case "ZTRANSLATE":
                    return zTranslate;
                default:
                    return null;
            }
        }

        public override bool SetSuffix(string suffixName, object value)
        {
            switch (suffixName)
            {
                case "YAW":
                    yaw = (int)value;
                    break;
                case "PITCH":
                    pitch = (int)value;
                    break;
                case "ROLL":
                    roll = (int)value;
                    break;
                case "XTRANSLATE":
                    xTranslate = (int)value;
                    break;
                case "YTRANSLATE":
                    yTranslate = (int)value;
                    break;
                case "ZTRANSLATE":
                    zTranslate = (int)value;
                    break;
                default:
                    return false;
            }
            return true;
        }

        private void OnFlyByWire(FlightCtrlState st)
        {
            st.
        }

        public void Dispose()
        {
            Target.OnFlyByWire -= OnFlyByWire;
        }
    }
}