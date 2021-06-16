using System;
using System.Globalization;
using System.Collections.Generic;
using kOS.AddOns.RemoteTech;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using kOS.Safe.Utilities;
using kOS.Utilities;
using Math = System.Math;
using kOS.Communication;
using kOS.Control;

namespace kOS.Suffixed
{
    [KOSNomenclature("Control")]
    public class FlightControl : Structure , IDisposable, IFlightControlParameter
    {
        private const float SETTING_EPILSON = 0.00001f;
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
        private bool bound;
        private readonly List<string> floatSuffixes;
        private readonly List<string> vectorSuffixes;

        public FlightControl(Vessel vessel)
        {
            bound = false;
            Vessel = vessel;

            floatSuffixes = new List<string> { "YAW", "PITCH", "ROLL", "STARBOARD", "TOP", "FORE", "MAINTHROTTLE", "PILOTMAINTHROTTLE", "WHEELTHROTTLE", "WHEELSTEER" };
            vectorSuffixes = new List<string> { "ROTATION", "TRANSLATION" };
            InitializeSuffixes();
            InitializePilotSuffixes();
        }

        public Vessel Vessel { get; private set; }

        public bool FightsWithSas { get { return false; } }

        public override bool SetSuffix(string suffixName, object value, bool failOkay = false)
        {
            float floatValue = 0;
            Vector vectorValue = null;

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

        public void Dispose()
        {
        }

        public override string ToString()
        {
            return string.Format("{0} FlightControl for {1}", base.ToString(), Vessel.vesselName);
        }

        private void InitializePilotSuffixes()
        {
            AddSuffix(new[] { "PILOTYAW" }, new Suffix<ScalarValue>(() => ReadPilot(ref FlightInputHandler.state.yaw)));
            AddSuffix(new[] { "PILOTYAWTRIM" }, new SetSuffix<ScalarValue>(() => ReadPilot(ref FlightInputHandler.state.yawTrim),
                v => WritePilot(ref FlightInputHandler.state.yawTrim, v)));
            AddSuffix(new[] { "PILOTROLL" }, new Suffix<ScalarValue>(() => ReadPilot(ref FlightInputHandler.state.roll)));
            AddSuffix(new[] { "PILOTROLLTRIM" }, new SetSuffix<ScalarValue>(() => ReadPilot(ref FlightInputHandler.state.rollTrim),
                                v => WritePilot(ref FlightInputHandler.state.rollTrim, v)));
            AddSuffix(new[] { "PILOTPITCH" }, new Suffix<ScalarValue>(() => ReadPilot(ref FlightInputHandler.state.pitch)));
            AddSuffix(new[] { "PILOTPITCHTRIM" }, new SetSuffix<ScalarValue>(() => ReadPilot(ref FlightInputHandler.state.pitchTrim),
                                v => WritePilot(ref FlightInputHandler.state.pitchTrim, v)));

            AddSuffix(new[] { "PILOTFORE" }, new Suffix<ScalarValue>(() => Invert(ReadPilot(ref FlightInputHandler.state.Z))));
            AddSuffix(new[] { "PILOTSTARBOARD" }, new Suffix<ScalarValue>(() => Invert(ReadPilot(ref FlightInputHandler.state.X))));
            AddSuffix(new[] { "PILOTTOP" }, new Suffix<ScalarValue>(() => ReadPilot(ref FlightInputHandler.state.Y)));

            AddSuffix(new[] { "PILOTWHEELTHROTTLE" }, new SetSuffix<ScalarValue>(() => ReadPilot(ref FlightInputHandler.state.wheelThrottle),
                                                                v => WritePilot(ref FlightInputHandler.state.wheelThrottle, v)));
            AddSuffix(new[] { "PILOTWHEELTHROTTLETRIM" }, new SetSuffix<ScalarValue>(() => ReadPilot(ref FlightInputHandler.state.wheelThrottleTrim),
                                v => WritePilot(ref FlightInputHandler.state.wheelThrottleTrim, v)));
            AddSuffix(new[] { "PILOTWHEELSTEER" }, new Suffix<ScalarValue>(() => ReadPilot(ref FlightInputHandler.state.wheelSteer)));
            AddSuffix(new[] { "PILOTWHEELSTEERTRIM" }, new SetSuffix<ScalarValue>(() => ReadPilot(ref FlightInputHandler.state.wheelSteerTrim),
                                v => WritePilot(ref FlightInputHandler.state.wheelSteerTrim, v)));
            AddSuffix(new[] { "PILOTNEUTRAL" }, new Suffix<BooleanValue>(() => Vessel == FlightGlobals.ActiveVessel && FlightInputHandler.state.isNeutral));

            AddSuffix(new[] { "PILOTROTATION" }, new Suffix<Vector>(GetPilotRotation));
            AddSuffix(new[] { "PILOTTRANSLATION" }, new Suffix<Vector>(GetPilotTranslation));

            AddSuffix(new[] { "PILOTMAINTHROTTLE" }, new ClampSetSuffix<ScalarValue>(() => ReadPilot(ref FlightInputHandler.state.mainThrottle), value =>
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

        private void WritePilot(ref float flightInputValue, float newVal)
        {
            if (FlightGlobals.ActiveVessel)
                flightInputValue = newVal;
        }

        private void InitializeSuffixes()
        {
            //ROTATION
            AddSuffix(new[] { "YAW" }, new ClampSetSuffix<ScalarValue>(() => yaw, value => yaw = value, -1, 1));
            AddSuffix(new[] { "YAWTRIM" }, new ClampSetSuffix<ScalarValue>(() => yawTrim, value => yawTrim = value, -1, 1));
            AddSuffix(new[] { "ROLL" }, new ClampSetSuffix<ScalarValue>(() => roll, value => roll = value, -1, 1));
            AddSuffix(new[] { "ROLLTRIM" }, new ClampSetSuffix<ScalarValue>(() => rollTrim, value => rollTrim = value, -1, 1));
            AddSuffix(new[] { "PITCH" }, new ClampSetSuffix<ScalarValue>(() => pitch, value => pitch = value, -1, 1));
            AddSuffix(new[] { "PITCHTRIM" }, new ClampSetSuffix<ScalarValue>(() => pitchTrim, value => pitchTrim = value, -1, 1));
            AddSuffix(new[] { "ROTATION" }, new SetSuffix<Vector>(() => new Vector(yaw, pitch, roll), SetRotation));

            //TRANSLATION
            AddSuffix(new[] { "FORE" }, new ClampSetSuffix<ScalarValue>(() => fore, value => fore = value, -1, 1));
            AddSuffix(new[] { "STARBOARD" }, new ClampSetSuffix<ScalarValue>(() => starboard, value => starboard = value, -1, 1));
            AddSuffix(new[] { "TOP" }, new ClampSetSuffix<ScalarValue>(() => top, value => top = value, -1, 1));
            AddSuffix(new[] { "TRANSLATION" }, new SetSuffix<Vector>(() => new Vector(starboard, top, fore) , SetTranslation));

            //ROVER
            AddSuffix(new[] { "WHEELSTEER" }, new ClampSetSuffix<ScalarValue>(() => wheelSteer, value => wheelSteer = value, -1, 1));
            AddSuffix(new[] { "WHEELSTEERTRIM" }, new ClampSetSuffix<ScalarValue>(() => wheelSteerTrim, value => wheelSteerTrim = value, -1, 1));
            

            //THROTTLE
            AddSuffix(new[] { "MAINTHROTTLE" }, new ClampSetSuffix<ScalarValue>(() => mainThrottle, value => mainThrottle = value, 0, 1));
            AddSuffix(new[] { "WHEELTHROTTLE" }, new ClampSetSuffix<ScalarValue>(() => wheelThrottle, value => wheelThrottle = value, -1, 1));
            AddSuffix(new[] { "WHEELTHROTTLETRIM" }, new ClampSetSuffix<ScalarValue>(() => wheelThrottleTrim, value => wheelThrottleTrim = value, -1, 1));

            //OTHER
            AddSuffix(new[] { "BOUND" }, new SetSuffix<BooleanValue>(() => bound, value => bound = value));
            AddSuffix(new[] { "NEUTRAL", "NEUTRALIZE" }, new SetSuffix<BooleanValue>(IsNeutral, v => {if (v) Neutralize();}));
        }

        private Vector GetPilotTranslation()
        {
            if (Vessel == FlightGlobals.ActiveVessel)
            {
                return new Vector(
                    Invert(FlightInputHandler.state.X),
                    FlightInputHandler.state.Y, 
                    Invert(FlightInputHandler.state.Z)
                    );
            }
            return Vector.Zero;
        }

        private float Invert(float f)
        {
            // starboard and fore are reversed in KSP so we invert them here
            return f * -1;
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
                starboard = (float)KOSMath.Clamp(vectorValue.X, -1, 1);
                top = (float)KOSMath.Clamp(vectorValue.Y, -1, 1);
                fore = (float)KOSMath.Clamp(vectorValue.Z, -1, 1);
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
                yaw = (float)KOSMath.Clamp(vectorValue.X, -1, 1);
                pitch = (float)KOSMath.Clamp(vectorValue.Y, -1, 1);
                roll = (float)KOSMath.Clamp(vectorValue.Z, -1, 1);
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
            if (float.TryParse(valueStr, NumberStyles.Float, CultureInfo.InvariantCulture, out valueParsed))
            {
                doubleValue = valueParsed;
            }
            else
            {
                return false;
            }
            return true;
        }

        private void Neutralize()
        {
            ResetControls();
        }

        private void ResetControls()
        {
            yaw = default(float);
            yawTrim = default(float);
            pitch = default(float);
            pitchTrim = default(float);
            roll = default(float);
            rollTrim = default(float);
            fore = default(float);
            starboard = default(float);
            top = default(float);
            wheelSteer = default(float);
            wheelSteerTrim = default(float);
            wheelThrottle = default(float);
            wheelThrottleTrim = default(float);
        }

        private BooleanValue IsNeutral()
        {
            return (yaw == yawTrim && pitch == pitchTrim && roll == rollTrim &&
                fore == 0 && starboard == 0 && top == 0 &&
                wheelSteer == wheelSteerTrim && wheelThrottle == wheelSteerTrim);
        }

        private void OnFlyByWire(FlightCtrlState st)
        {
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
            if(Math.Abs(starboard) > SETTING_EPILSON) st.X = Invert(starboard);
            if(Math.Abs(top) > SETTING_EPILSON) st.Y = top;
            if(Math.Abs(fore) > SETTING_EPILSON) st.Z = Invert(fore);

            if(Math.Abs(wheelSteer) > SETTING_EPILSON) st.wheelSteer = wheelSteer;
            if(Math.Abs(wheelThrottle) > SETTING_EPILSON) st.wheelThrottle = wheelThrottle;
            if(Math.Abs(mainThrottle) > SETTING_EPILSON) st.mainThrottle = mainThrottle;

            if(Math.Abs(wheelSteerTrim) > SETTING_EPILSON) st.wheelSteerTrim = wheelSteerTrim;
            if(Math.Abs(wheelThrottleTrim) > SETTING_EPILSON) st.wheelThrottleTrim = wheelThrottleTrim;

        }

        bool IFlightControlParameter.Enabled
        {
            get
            {
                return !IsNeutral();
            }
        }

        bool IFlightControlParameter.IsAutopilot
        {
            get
            {
                return true;
            }
        }

        uint IFlightControlParameter.ControlPartId
        {
            get
            {
                return 0;
            }
        }

        void IFlightControlParameter.UpdateValue(object value, SharedObjects shared)
        {
            throw new NotImplementedException();
            // FlightControl value does not get set like other values
        }

        object IFlightControlParameter.GetValue()
        {
            throw new NotImplementedException();
            // FlightControl value does not get like other values
        }

        SharedObjects IFlightControlParameter.GetShared()
        {
            return null;
        }

        Vessel IFlightControlParameter.GetResponsibleVessel()
        {
            return Vessel;
        }

        void IFlightControlParameter.UpdateAutopilot(FlightCtrlState c, ControlTypes ctrlLock)
        {
            OnFlyByWire(c);
        }

        bool IFlightControlParameter.SuppressAutopilot(FlightCtrlState c)
        {
            return !(IsNeutral());
        }

        void IFlightControlParameter.EnableControl(SharedObjects shared)
        {
            // No need to enable control, it will automatically enable based on setting values
        }

        void IFlightControlParameter.DisableControl(SharedObjects shared)
        {
            Neutralize();
        }

        void IFlightControlParameter.DisableControl()
        {
            Neutralize();
        }

        void IFlightControlParameter.CopyFrom(IFlightControlParameter origin)
        {
            return;
            throw new NotImplementedException(); // TODO: implement copy
        }
    }
}