using System;

namespace kOS.Suffixed
{
    public class FlightControl : SpecialValue , IDisposable
    {
        // For rotation x = yaw, y = pitch, and z = roll
        private Vector rotation;
        private Vector translation;
        private bool neutral;
        private float wheelSteer;
        private float wheelThrottle;
        private float mainThrottle;
        private bool killRotation;
        private FlightCtrlState flightStats;
        public Vessel Target { get; private set; }

        public FlightControl(Vessel target)
        {
            rotation = new Vector(0, 0, 0);
            translation = new Vector(0, 0, 0);
            Target = target;
            Target.OnFlyByWire += OnFlyByWire;
            
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
                case "NEUTRAL":
                    return neutral;
                case "MAINTHROTTLE":
                    return mainThrottle;
                case "WHEELTHROTTLE":
                    return wheelThrottle;
                case "WHEELSTEER":
                    return wheelSteer;
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

        private bool CheckKillRotation(string suffixName, object value)
        {
            if (suffixName == "KILLROTATION")
            {
                killRotation = bool.Parse(value.ToString());
                if (killRotation)
                {
                    rotation.X = 0;
                    rotation.Y = 0;
                    rotation.Z = 0;
                    translation.X = 0;
                    translation.Y = 0;
                    translation.Z = 0;
                    wheelSteer = 0;
                    wheelThrottle = 0;
                    neutral = false;
                }
                return true;
            }
            killRotation = false;
            return false;
        }

        private bool CheckNeutral(string suffix, object value)
        {
            if (suffix == "NEUTRALIZE")
            {
                neutral = bool.Parse(value.ToString());
                if (neutral)
                {
                    rotation.X = 0;
                    rotation.Y = 0;
                    rotation.Z = 0;
                    translation.X = 0;
                    translation.Y = 0;
                    translation.Z = 0;
                    wheelSteer = 0;
                    wheelThrottle = 0;
                    killRotation = false;
                }
                return true;
            }
            neutral = false;
            return false;
        }

        private void OnFlyByWire(FlightCtrlState st)
        {
            st.CopyFrom(flightStats);
            flightStats = st;
        }

        public void Dispose()
        {
            Target.OnFlyByWire -= OnFlyByWire;
        }
    }
}