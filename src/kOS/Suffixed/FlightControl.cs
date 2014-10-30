using System;
using System.Collections.Generic;
using kOS.AddOns.RemoteTech2;
using kOS.Safe.Encapsulation;
using kOS.Safe.Utilities;
using kOS.Utilities;

namespace kOS.Suffixed
{
    public class FlightControl : Structure , IDisposable
    {
        private const float SETTING_EPILSON = 0.01f;
        // For rotation x = yaw, y = pitch, and z = roll
        private float yaw;
        private float yawTrim;
        private float pitch;
        private float pitchTrim;
        private float roll;
        private float rollTrim;
        private float fore;
        private float starboard;
        private float top;
        private float wheelSteer;
        private float wheelSteerTrim;
        private float wheelThrottle;
        private float wheelThrottleTrim;
        private float mainThrottle;
        private readonly Flushable<bool> neutral;
        private readonly Flushable<bool> killRotation;
        private bool bound;
        private readonly List<string> floatSuffixes;
        private readonly List<string> vectorSuffixes;

        public FlightControl(Vessel vessel)
        {
            neutral = new Flushable<bool>(); 
            killRotation = new Flushable<bool>(); 
            bound = false;
            Vessel = vessel;

            floatSuffixes = new List<string>
            {
                "YAW", "PITCH", "ROLL", 
                "YAWTRIM", "PITCHTRIM", "ROLLTRIM", 
                "STARBOARD", "TOP", "FORE", 
                "MAINTHROTTLE", "USERMAINTHROTTLE",
                "WHEELTHROTTLE", "WHEELSTEER",
                "WHEELTHROTTLETRIM", "WHEELSTEERTRIM"
            };
            vectorSuffixes = new List<string> { "ROTATION", "TRANSLATION" };
        }

        public Vessel Vessel { get; private set; }

        public override object GetSuffix(string suffixName)
        {
            switch (suffixName)
            {
                case "YAW":
                    return yaw;
                case "YAWTRIM":
                    return yawTrim;
                case "PITCH":
                    return pitch;
                case "PITCHTRIM":
                    return pitchTrim;
                case "ROLL":
                    return roll;
                case "ROLLTRIM":
                    return rollTrim;
                case "FORE":
                    return fore;
                case "STARBOARD":
                    return starboard;
                case "TOP":
                    return top;
                case "ROTATION":
                    return new Vector(yaw , pitch , roll );
                case "TRANSLATION":
                    return new Vector(starboard , top , fore );
                case "NEUTRAL":
                    return neutral;
                case "MAINTHROTTLE":
                    return mainThrottle;
                case "WHEELTHROTTLE":
                    return wheelThrottle;
                case "WHEELTHROTTLETRIM":
                    return wheelThrottleTrim;
                case "WHEELSTEER":
                    return wheelSteer;
                case "WHEELSTEERTRIM":
                    return wheelSteerTrim;
                case "PILOTYAW":
                    return Vessel.ctrlState.yaw;
                case "PILOTYAWTRIM":
                    return Vessel.ctrlState.yawTrim;
                case "PILOTPITCH":
                    return Vessel.ctrlState.pitch;
                case "PILOTPITCHTRIM":
                    return Vessel.ctrlState.pitchTrim;
                case "PILOTROLL":
                    return Vessel.ctrlState.roll;
                case "PILOTROLLTRIM":
                    return Vessel.ctrlState.rollTrim;
                case "PILOTFORE":
                    return Vessel.ctrlState.Z;
                case "PILOTSTARBOARD":
                    return Vessel.ctrlState.X;
                case "PILOTTOP":
                    return Vessel.ctrlState.Y;
                case "PILOTROTATION":
                    return new Vector(
                        Vessel.ctrlState.yaw , 
                        Vessel.ctrlState.pitch , 
                        Vessel.ctrlState.roll 
                        );
                case "PILOTTRANSLATION":
                    return new Vector(
                        Vessel.ctrlState.X , 
                        Vessel.ctrlState.Y , 
                        Vessel.ctrlState.Z 
                        );
                case "PILOTMAINTHROTTLE":
                    return Vessel.ctrlState.mainThrottle;
                case "PILOTWHEELTHROTTLE":
                    return Vessel.ctrlState.wheelThrottle;
                case "PILOTWHEELTHROTTLETRIM":
                    return Vessel.ctrlState.wheelThrottleTrim;
                case "PILOTWHEELSTEER":
                    return Vessel.ctrlState.wheelSteer;
                case "PILOTWHEELSTEERTRIM":
                    return Vessel.ctrlState.wheelSteerTrim;
                case "BOUND":
                    return bound;
                default:
                    return null;
            }
        }

        public override bool SetSuffix(string suffixName, object value)
        {
            float floatValue = 0;
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

            if (floatSuffixes.Contains(suffixName))
            {
                if (!ValueToFloat(value, ref floatValue)) return true;
            }

            if (vectorSuffixes.Contains(suffixName))
            {
                if (!ValueToVector(value, ref vectorValue)) return true;
            }

            switch (suffixName)
            {
                case "YAW":
                    yaw = Utils.Clamp(floatValue, -1, 1);
                    break;
                case "PITCH":
                    pitch = Utils.Clamp(floatValue, -1, 1);
                    break;
                case "ROLL":
                    roll = Utils.Clamp(floatValue, -1, 1);
                    break;
                case "YAWTRIM":
                    yawTrim = Utils.Clamp(floatValue, -1, 1);
                    break;
                case "PITCHTRIM":
                    pitchTrim = Utils.Clamp(floatValue, -1, 1);
                    break;
                case "ROLLTRIM":
                    rollTrim = Utils.Clamp(floatValue, -1, 1);
                    break;
                case "STARBOARD":
                    starboard = Utils.Clamp(floatValue, -1, 1);
                    break;
                case "TOP":
                    top = Utils.Clamp(floatValue, -1, 1);
                    break;
                case "FORE":
                    fore = Utils.Clamp(floatValue, -1, 1);
                    break;
                case "ROTATION":
                    SetRotation(vectorValue);
                    break;
                case "TRANSLATION":
                    SetTranslation(vectorValue);
                    break;
                case "MAINTHROTTLE":
                    mainThrottle = Utils.Clamp(floatValue, 0, 1);
                    break;
                case "PILOTMAINTHROTTLE":
                    Vessel.ctrlState.mainThrottle = Utils.Clamp(floatValue, 0, 1);
                    if (Vessel == FlightGlobals.ActiveVessel)
                    {
                        FlightInputHandler.state.mainThrottle = 0; 
                    }
                    break;
                case "WHEELTHROTTLE":
                    wheelThrottle = Utils.Clamp(floatValue, 0, 1);
                    break;
                case "WHEELSTEER":
                    wheelSteer = Utils.Clamp(floatValue, -1, 1);
                    break;
                case "WHEELTHROTTLETRIM":
                    wheelThrottleTrim = Utils.Clamp(floatValue, 0, 1);
                    break;
                case "WHEELSTEERTRIM":
                    wheelSteerTrim = Utils.Clamp(floatValue, -1, 1);
                    break;
                default:
                    return false;
            }
            return true;
        }

        private void SetTranslation(Vector vectorValue)
        {
            if (vectorValue == null)
            {
                fore = 0.0f;
                top = 0.0f;
                starboard = 0.0f;
            }
            else
            {
                starboard = (float)vectorValue.X;
                top = (float)vectorValue.Y;
                fore = (float)vectorValue.Z;
            }
        }

        private void SetRotation(Vector vectorValue)
        {
            if (vectorValue == null)
            {
                yaw = 0.0f;
                pitch = 0.0f;
                roll = 0.0f;
            }
            else
            {
                yaw = (float)vectorValue.X;
                pitch = (float)vectorValue.Y;
                roll = (float)vectorValue.Z;
            }
        }

        private bool ValueToVector(object value, ref Vector vectorValue)
        {
            var vector = value as Vector;

            if (vector != null)
            {
                if (!Utils.IsValidVector(vector))
                    return false;
                vectorValue = vector;
            }
            else
            {
                return false;
            }
            return true;
        }

        private static bool ValueToFloat(object value, ref float doubleValue)
        {
            var valueStr = value.ToString();
            float valueParsed;
            if (float.TryParse(valueStr, out valueParsed))
            {
                doubleValue = valueParsed;
            }
            else
            {
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
                ResetControls();
                neutral.Value = bool.Parse(value.ToString());
                Unbind();
                return true;
            }
            neutral.Value = false;
            return false;
        }

        private void ResetControls()
        {
            yaw = default(float);
            pitch = default(float);
            roll = default(float);
            fore = default(float);
            starboard = default(float);
            top = default(float);
            wheelSteer = default(float);
            wheelThrottle = default(float);
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
            if(Math.Abs(yaw) > SETTING_EPILSON) st.yaw = yaw;
            if(Math.Abs(pitch) > SETTING_EPILSON) st.pitch = pitch;
            if(Math.Abs(roll) > SETTING_EPILSON) st.roll = roll;

            if(Math.Abs(yawTrim) > SETTING_EPILSON) st.yawTrim = yawTrim;
            if(Math.Abs(pitchTrim) > SETTING_EPILSON) st.pitchTrim = pitchTrim;
            if(Math.Abs(rollTrim) > SETTING_EPILSON) st.rollTrim = rollTrim;

            // starboard and fore are reversed in KSP so we invert them here
            if(Math.Abs(starboard) > SETTING_EPILSON) st.X = starboard * -1;
            if(Math.Abs(top) > SETTING_EPILSON) st.Y = top;
            if(Math.Abs(fore) > SETTING_EPILSON) st.Z = fore * -1;

            if(Math.Abs(wheelSteer) > SETTING_EPILSON) st.wheelSteer = wheelSteer;
            if(Math.Abs(wheelThrottle) > SETTING_EPILSON) st.wheelThrottle = wheelThrottle;
            if(Math.Abs(mainThrottle) > SETTING_EPILSON) st.mainThrottle = mainThrottle;

            if(Math.Abs(wheelSteerTrim) > SETTING_EPILSON) st.wheelSteerTrim = wheelSteerTrim;
            if(Math.Abs(wheelThrottleTrim) > SETTING_EPILSON) st.wheelThrottleTrim = wheelThrottleTrim;

        }

        public void UpdateVessel(Vessel toUpdate)
        {
            Vessel = toUpdate;
        }

        public void Dispose()
        {
            Unbind();
        }

        public override string ToString()
        {
            return string.Format("{0} FlightControl for {1}", base.ToString(), Vessel.vesselName);
        }
    }
}