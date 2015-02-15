﻿using System;
using System.Collections.Generic;
using kOS.AddOns.RemoteTech2;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Utilities;
using kOS.Utilities;
using Math = System.Math;

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
        private readonly Flushable<bool> resetTrim;
        private bool bound;
        private readonly List<string> floatSuffixes;
        private readonly List<string> vectorSuffixes;

        public FlightControl(Vessel vessel)
        {
            neutral = new Flushable<bool>(); 
            killRotation = new Flushable<bool>(); 
            resetTrim = new Flushable<bool>(); 
            bound = false;
            Vessel = vessel;

            floatSuffixes = new List<string> { "YAW", "PITCH", "ROLL", "STARBOARD", "TOP", "FORE", "MAINTHROTTLE", "PILOTMAINTHROTTLE", "WHEELTHROTTLE", "WHEELSTEER" };
            vectorSuffixes = new List<string> { "ROTATION", "TRANSLATION" };
            InitializeSuffixes();
            InitializePilotSuffixes();
        }

        public Vessel Vessel { get; private set; }

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

            if (CheckResetTrim(suffixName, value))
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

            return base.SetSuffix(suffixName, value);
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
                Vessel.OnPreAutopilotUpdate -= OnFlyByWire;
            }
            bound = false;
            UnityEngine.Debug.Log("kOS: FlightControl Unbound");
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

        private void InitializePilotSuffixes()
        {
            AddSuffix(new[] { "PILOTYAW" }, new Suffix<float>(() => ReadPilot(ref FlightInputHandler.state.yaw)));
            AddSuffix(new[] { "PILOTYAWTRIM" }, new Suffix<float>(() => ReadPilot(ref FlightInputHandler.state.yawTrim)));
            AddSuffix(new[] { "PILOTROLL" }, new Suffix<float>(() => ReadPilot(ref FlightInputHandler.state.roll)));
            AddSuffix(new[] { "PILOTROLLTRIM" }, new Suffix<float>(() => ReadPilot(ref FlightInputHandler.state.rollTrim)));
            AddSuffix(new[] { "PILOTPITCH" }, new Suffix<float>(() => ReadPilot(ref FlightInputHandler.state.pitch)));
            AddSuffix(new[] { "PILOTPITCHTRIM" }, new Suffix<float>(() => ReadPilot(ref FlightInputHandler.state.pitchTrim)));
            AddSuffix(new[] { "PILOTFORE" }, new Suffix<float>(() => ReadPilot(ref FlightInputHandler.state.Z)));
            AddSuffix(new[] { "PILOTSTARBOARD" }, new Suffix<float>(() => ReadPilot(ref FlightInputHandler.state.X)));
            AddSuffix(new[] { "PILOTTOP" }, new Suffix<float>(() => ReadPilot(ref FlightInputHandler.state.Y)));
            AddSuffix(new[] { "PILOTWHEELTHROTTLE" }, new Suffix<float>(() => ReadPilot(ref FlightInputHandler.state.wheelThrottle)));
            AddSuffix(new[] { "PILOTWHEELTHROTTLETRIM" }, new Suffix<float>(() => ReadPilot(ref FlightInputHandler.state.wheelThrottleTrim)));
            AddSuffix(new[] { "PILOTWHEELSTEER" }, new Suffix<float>(() => ReadPilot(ref FlightInputHandler.state.wheelSteer)));
            AddSuffix(new[] { "PILOTWHEELSTEERTRIM" }, new Suffix<float>(() => ReadPilot(ref FlightInputHandler.state.wheelSteerTrim)));
            AddSuffix(new[] { "PILOTNEUTRAL" }, new Suffix<bool>(() => Vessel == FlightGlobals.ActiveVessel && FlightInputHandler.state.isNeutral));

            AddSuffix(new[] { "PILOTROTATION" }, new Suffix<Vector>(GetPilotRotation));
            AddSuffix(new[] { "PILOTTRANSLATION" }, new Suffix<Vector>(GetPilotTranslation));

            AddSuffix(new[] { "PILOTMAINTHROTTLE" }, new ClampSetSuffix<float>(() => ReadPilot(ref FlightInputHandler.state.mainThrottle), value =>
            {
                Vessel.ctrlState.mainThrottle = value;
                if (Vessel == FlightGlobals.ActiveVessel)
                {
                    FlightInputHandler.state.mainThrottle = value;
                }
            }, 0, 1));
        }

        private float ReadPilot(ref float flightInputValue)
        {
            return Vessel == FlightGlobals.ActiveVessel ? flightInputValue : 0f;
        }

        private void InitializeSuffixes()
        {
            //ROTATION
            AddSuffix(new[] { "YAW" }, new ClampSetSuffix<float>(() => yaw, value => yaw = value, -1, 1));
            AddSuffix(new[] { "YAWTRIM" }, new ClampSetSuffix<float>(() => yawTrim, value => yawTrim = value, -1, 1));
            AddSuffix(new[] { "ROLL" }, new ClampSetSuffix<float>(() => roll, value => roll = value, -1, 1));
            AddSuffix(new[] { "ROLLTRIM" }, new ClampSetSuffix<float>(() => rollTrim, value => rollTrim = value, -1, 1));
            AddSuffix(new[] { "PITCH" }, new ClampSetSuffix<float>(() => pitch, value => pitch = value, -1, 1));
            AddSuffix(new[] { "PITCHTRIM" }, new ClampSetSuffix<float>(() => pitchTrim, value => pitchTrim = value, -1, 1));
            AddSuffix(new[] { "ROTATION" }, new SetSuffix<Vector>(() => new Vector(yaw, pitch, roll), SetRotation));

            //TRANSLATION
            AddSuffix(new[] { "FORE" }, new ClampSetSuffix<float>(() => fore, value => fore = value, -1, 1));
            AddSuffix(new[] { "STARBOARD" }, new ClampSetSuffix<float>(() => starboard, value => starboard = value, -1, 1));
            AddSuffix(new[] { "TOP" }, new ClampSetSuffix<float>(() => top, value => top = value, -1, 1));
            AddSuffix(new[] { "TRANSLATION" }, new SetSuffix<Vector>(() => new Vector(starboard, top, fore) , SetTranslation));

            //ROVER
            AddSuffix(new[] { "WHEELSTEER" }, new ClampSetSuffix<float>(() => wheelSteer, value => wheelSteer = value, -1, 1));
            AddSuffix(new[] { "WHEELSTEERTRIM" }, new ClampSetSuffix<float>(() => wheelSteerTrim, value => wheelSteerTrim = value, -1, 1));
            

            //THROTTLE
            AddSuffix(new[] { "MAINTHROTTLE" }, new ClampSetSuffix<float>(() => mainThrottle, value => mainThrottle = value, 0, 1));
            AddSuffix(new[] { "WHEELTHROTTLE" }, new ClampSetSuffix<float>(() => wheelThrottle, value => wheelThrottle = value, -1, 1));
            AddSuffix(new[] { "WHEELTHROTTLETRIM" }, new ClampSetSuffix<float>(() => wheelThrottleTrim, value => wheelThrottleTrim = value, -1, 1));

            //OTHER
            AddSuffix(new[] { "BOUND" }, new SetSuffix<bool>(() => bound, value => bound = value));
            AddSuffix(new[] { "NEUTRAL" }, new Suffix<Flushable<bool>>(() => neutral));

        }

        private Vector GetPilotTranslation()
        {
            if (Vessel == FlightGlobals.ActiveVessel)
            {
                return new Vector(FlightInputHandler.state.X, FlightInputHandler.state.Y, FlightInputHandler.state.Z);
            }
            return Vector.Zero;
        }

        private Vector GetPilotRotation()
        {
            if (Vessel == FlightGlobals.ActiveVessel)
            {
                return new Vector(FlightInputHandler.state.yaw, FlightInputHandler.state.pitch, FlightInputHandler.state.roll);
            }
            return Vector.Zero;
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
                starboard = (float)Safe.Utilities.Math.Clamp(vectorValue.X, -1, 1);
                top = (float)Safe.Utilities.Math.Clamp(vectorValue.Y, -1, 1);
                fore = (float)Safe.Utilities.Math.Clamp(vectorValue.Z, -1, 1);
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
                yaw = (float)Safe.Utilities.Math.Clamp(vectorValue.X, -1, 1);
                pitch = (float)Safe.Utilities.Math.Clamp(vectorValue.Y, -1, 1);
                roll = (float)Safe.Utilities.Math.Clamp(vectorValue.Z, -1, 1);
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
                Vessel.OnPreAutopilotUpdate += OnFlyByWire;
            }
            bound = true;
            UnityEngine.Debug.Log("kOS: FlightControl Bound");
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
        private bool CheckResetTrim(string suffixName, object value)
        {
            if (suffixName == "RESETTRIM")
            {
                resetTrim.Value = bool.Parse(value.ToString());
                return true;
            }
            resetTrim.Value = false;
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

            if (resetTrim.IsStale)
            {
                if (resetTrim.FlushValue)
                {
                    st.ResetTrim();
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
    }
}