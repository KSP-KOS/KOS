using System;
using kOS.Utilities;

namespace kOS.Suffixed
{
    public class FlightControl : SpecialValue , IDisposable
    {
        // For rotation x = yaw, y = pitch, and z = roll
        private readonly Flushable<Vector> rotation;
        private readonly Flushable<Vector> translation;
        private readonly Flushable<bool> neutral;
        private readonly Flushable<float> wheelSteer;
        private readonly Flushable<float> wheelThrottle;
        private readonly Flushable<float> mainThrottle;
        private readonly Flushable<bool> killRotation;
        private readonly Vessel target;

        public FlightControl(Vessel target)
        {
            rotation = new Flushable<Vector> {Value = new Vector(0, 0, 0)};
            translation = new Flushable<Vector> {Value = new Vector(0, 0, 0)};
            neutral = new Flushable<bool>();
            wheelSteer = new Flushable<float>();
            wheelThrottle = new Flushable<float>();
            mainThrottle = new Flushable<float>();
            killRotation = new Flushable<bool>();
            
            this.target = target;
            this.target.OnFlyByWire += OnFlyByWire;
        }

        public override object GetSuffix(string suffixName)
        {
            switch (suffixName)
            {
                case "YAW":
                    return rotation.Value.X;
                case "PITCH":
                    return rotation.Value.Y;
                case "ROLL":
                    return rotation.Value.Z;
                case "FORE":
                    return translation.Value.Z;
                case "STARBOARD":
                    return translation.Value.X;
                case "TOP":
                    return translation.Value.Y;
                case "ROTATION":
                    return rotation.Value;
                case "TRANSLATION":
                    return translation.Value;
                case "NEUTRAL":
                    return neutral.Value;
                case "MAINTHROTTLE":
                    return mainThrottle.Value;
                case "WHEELTHROTTLE":
                    return wheelThrottle.Value;
                case "WHEELSTEER":
                    return wheelSteer.Value;
                default:
                    return null;
            }
        }

        public override bool SetSuffix(string suffixName, object value)
        {
            if (CheckNeutral(suffixName, value))
            {
                return true;
            }

            if (CheckKillRotation(suffixName, value))
            {
                return true;
            }

            switch (suffixName)
            {
                case "YAW":
                    rotation.Value.X = Convert.ToSingle(value);
                    break;
                case "PITCH":
                    rotation.Value.Y = Convert.ToSingle(value);
                    break;
                case "ROLL":
                    rotation.Value.Z = Convert.ToSingle(value);
                    break;
                case "STARBOARD":
                    translation.Value.X = Convert.ToSingle(value);
                    break;
                case "TOP":
                    translation.Value.Y = Convert.ToSingle(value);
                    break;
                case "FORE":
                    translation.Value.Z = Convert.ToSingle(value);
                    break;
                case "ROTATION":
                    rotation.Value = (Vector) value;
                    break;
                case "TRANSLATION":
                    translation.Value = (Vector) value;
                    break;
                case "MAINTHROTTLE":
                    mainThrottle.Value = Convert.ToSingle(value);
                    break;
                case "WHEELTHROTTLE":
                    wheelThrottle.Value = Convert.ToSingle(value);
                    break;
                case "WHEELSTEER":
                    wheelSteer.Value = Convert.ToSingle(value);
                    break;
                default:
                    return false;
            }
            return true;
        }

        private bool CheckKillRotation(string suffixName, object value)
        {
            if (suffixName == "KILLROTATION")
            {
                killRotation.Value = bool.Parse(value.ToString());
                return true;
            }
            killRotation.Value = false;
            return false;
        }

        private bool CheckNeutral(string suffix, object value)
        {
            if (suffix == "NEUTRALIZE")
            {
                neutral.Value = bool.Parse(value.ToString());
                return true;
            }
            neutral.Value = false;
            return false;
        }

        private void OnFlyByWire(FlightCtrlState st)
        {
            if (neutral.IsStale)
            {
                st.Neutralize();
            }
            else
            {
                PushNewSetting(st);
            }
            SynchronizeWithFlightCtrl(st);
            }

        private void PushNewSetting(FlightCtrlState st)
        {
            if (translation.IsStale)
            {
                st.X = (float) translation.FlushValue.X;
                st.Y = (float) translation.FlushValue.Y;
                st.Z = (float) translation.FlushValue.Z;
            }
            if (rotation.IsStale)
            {
                st.pitch = (float) rotation.FlushValue.Y;
                st.yaw = (float) translation.FlushValue.X;
                st.roll = (float) translation.FlushValue.Z;
            }
            if (wheelSteer.IsStale) st.wheelSteer = wheelSteer.FlushValue;
            if (wheelThrottle.IsStale) st.wheelThrottle = wheelThrottle.FlushValue;
            if (mainThrottle.IsStale) st.mainThrottle = mainThrottle.FlushValue;
        }

        private void SynchronizeWithFlightCtrl(FlightCtrlState st)
        {
            rotation.Value.X = st.yaw;
            rotation.Value.Y = st.pitch;
            rotation.Value.Z = st.roll;
            translation.Value.X = st.X;
            translation.Value.Y = st.Y;
            translation.Value.Z = st.Z;
            wheelSteer.Value = st.wheelSteer;
            wheelThrottle.Value = st.wheelThrottle;
            mainThrottle.Value = st.mainThrottle;
        }

        public void Dispose()
        {
            target.OnFlyByWire -= OnFlyByWire;
        }
    }
}