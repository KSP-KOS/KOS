using System;
using kOS.Utilities;

namespace kOS.Suffixed
{
    public class FlightControl : SpecialValue , IDisposable
    {
        // For rotation x = yaw, y = pitch, and z = roll
        private Vector rotation;
        private Vector translation;
        private float wheelSteer;
        private float wheelThrottle;
        private float mainThrottle;
        private readonly Flushable<bool> neutral;
        private readonly Flushable<bool> killRotation;
        private readonly Vessel vessel;
        private bool bound;

        public FlightControl(Vessel vessel)
        {
            rotation = new Vector(0, 0, 0);
            translation = new Vector(0, 0, 0);
            neutral = new Flushable<bool>(); 
            killRotation = new Flushable<bool>(); 
            bound = true;
            this.vessel = vessel;
            Vessel.OnFlyByWire += OnFlyByWire;
        }

        public Vessel Vessel
        {
            get { return vessel; }
        }

        public override object GetSuffix(string suffixName)
        {
            switch (suffixName)
            {
                case "YAW":
                    return rotation.X;
                case "PITCH":
                    return rotation.Y;
                case "ROLL":
                    return rotation.Z;
                case "FORE":
                    return translation.Z;
                case "STARBOARD":
                    return translation.X;
                case "TOP":
                    return translation.Y;
                case "ROTATION":
                    return rotation;
                case "TRANSLATION":
                    return translation;
                case "NEUTRAL":
                    return neutral;
                case "MAINTHROTTLE":
                    return mainThrottle;
                case "WHEELTHROTTLE":
                    return wheelThrottle;
                case "WHEELSTEER":
                    return wheelSteer;
                case "BOUND":
                    return bound;
                default:
                    return null;
            }
        }

        public override bool SetSuffix(string suffixName, object value)
        {
            UnityEngine.Debug.Log("FlightControl Set: " + suffixName + " Value: " + value);
            Bind();

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
                    rotation.X = Convert.ToSingle(value);
                    break;
                case "PITCH":
                    rotation.Y = Convert.ToSingle(value);
                    break;
                case "ROLL":
                    rotation.Z = Convert.ToSingle(value);
                    break;
                case "STARBOARD":
                    translation.X = Convert.ToSingle(value);
                    break;
                case "TOP":
                    translation.Y = Convert.ToSingle(value);
                    break;
                case "FORE":
                    translation.Z = Convert.ToSingle(value);
                    break;
                case "ROTATION":
                    rotation = (Vector) value;
                    break;
                case "TRANSLATION":
                    translation = (Vector) value;
                    break;
                case "MAINTHROTTLE":
                    mainThrottle = Convert.ToSingle(value);
                    break;
                case "WHEELTHROTTLE":
                    wheelThrottle = Convert.ToSingle(value);
                    break;
                case "WHEELSTEER":
                    wheelSteer = Convert.ToSingle(value);
                    break;
                default:
                    return false;
            }
            return true;
        }

        private void Bind()
        {
            if (bound) return;
            UnityEngine.Debug.Log("FlightControl Bound");
            Vessel.OnFlyByWire += OnFlyByWire;
            bound = true;
        }

        private void Unbind()
        {
            if (!bound) return;
            UnityEngine.Debug.Log("FlightControl Unbound");
            Vessel.OnFlyByWire -= OnFlyByWire;
            bound = false;
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
                Unbind();
                return true;
            }
            neutral.Value = false;
            return false;
        }

        private void OnFlyByWire(FlightCtrlState st)
        {
            if (neutral.IsStale)
            {
                if (neutral.FlushValue)
                {
                    st.Neutralize();
                }
            }

            PushNewSetting(ref st);
        }

        private void PushNewSetting(ref FlightCtrlState st)
        {
            st.X = (float)translation.X;
            st.Y = (float)translation.Y;
            st.Z = (float)translation.Z;

            st.pitch = (float)rotation.Y;
            st.yaw = (float)rotation.X;
            st.roll = (float)rotation.Z;

            st.wheelSteer = wheelSteer;
            st.wheelThrottle = wheelThrottle;
            st.mainThrottle = mainThrottle;
        }

        public void Dispose()
        {
            Unbind();
        }
    }
}