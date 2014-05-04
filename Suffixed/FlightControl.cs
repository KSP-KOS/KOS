using System;
using System.Collections.Generic;
using System.Diagnostics;
using kOS.AddOns.RemoteTech2;
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
        private readonly List<string> doubleSuffixes;
        private readonly List<string> vectorSuffixes;

        public FlightControl(Vessel vessel)
        {
            rotation = new Vector(0, 0, 0);
            translation = new Vector(0, 0, 0);
            neutral = new Flushable<bool>(); 
            killRotation = new Flushable<bool>(); 
            bound = false;
            this.vessel = vessel;

            doubleSuffixes = new List<string> { "YAW", "PITCH", "ROLL", "STARBOARD", "TOP", "FORE", "MAINTHROTTLE", "WHEELTHROTTLE", "WHEELSTEER" };
            vectorSuffixes = new List<string> { "ROTATION", "TRANSLATION" };
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
            double doubleValue = 0;
            Vector vectorValue = null;
            
            Bind();

            if (CheckNeutral(suffixName, value))
            {
                return true;
            }

            if (CheckKillRotation(suffixName, value))
            {
                return true;
            }

            if (doubleSuffixes.Contains(suffixName))
            {
                doubleValue = Convert.ToDouble(value);
                if (double.IsNaN(doubleValue))
                    return true;
            }

            if (vectorSuffixes.Contains(suffixName))
            {
                vectorValue = (Vector)value;
                if (!Utils.IsValidVector(vectorValue))
                    return true;
            }

            switch (suffixName)
            {
                case "YAW":
                    rotation.X = Utils.Clamp(doubleValue, -1, 1);
                    break;
                case "PITCH":
                    rotation.Y = Utils.Clamp(doubleValue, -1, 1);
                    break;
                case "ROLL":
                    rotation.Z = Utils.Clamp(doubleValue, -1, 1);
                    break;
                case "STARBOARD":
                    translation.X = Utils.Clamp(doubleValue, -1, 1);
                    break;
                case "TOP":
                    translation.Y = Utils.Clamp(doubleValue, -1, 1);
                    break;
                case "FORE":
                    translation.Z = Utils.Clamp(doubleValue, -1, 1);
                    break;
                case "ROTATION":
                    rotation = vectorValue;
                    break;
                case "TRANSLATION":
                    translation = vectorValue;
                    break;
                case "MAINTHROTTLE":
                    mainThrottle = (float)Utils.Clamp(doubleValue, 0, 1);
                    break;
                case "WHEELTHROTTLE":
                    wheelThrottle = (float)Utils.Clamp(doubleValue, 0, 1);
                    break;
                case "WHEELSTEER":
                    wheelSteer = (float)Utils.Clamp(doubleValue, -1, 1);
                    break;
                default:
                    return false;
            }
            return true;
        }

        private void Bind()
        {
            UnityEngine.Debug.Log("kOS: FlightControl Binding");
            if (bound) return;

            if (RemoteTechHook.IsAvailable(Vessel.id))
            {
                RemoteTechHook.Instance.AddSanctionedPilot(Vessel.id, OnFlyByWire);
            }
            else
            {
                Vessel.OnFlyByWire += OnFlyByWire;
            }
            bound = true;
            UnityEngine.Debug.Log("kOS: FlightControl Bound");
        }

        public void Unbind()
        {
            UnityEngine.Debug.Log("kOS: FlightControl Unbinding");
            if (!bound) return;

            if (RemoteTechHook.IsAvailable())
            {
                RemoteTechHook.Instance.RemoveSanctionedPilot(Vessel.id, OnFlyByWire);
            }
            else
            {
                Vessel.OnFlyByWire -= OnFlyByWire;
            }
            bound = false;
            UnityEngine.Debug.Log("kOS: FlightControl Unbound");
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
            if (!bound) return;
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