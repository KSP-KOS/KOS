using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Utilities;
using System;
using UnityEngine;

namespace kOS.Suffixed
{
    /// <summary>
    /// A Structure to hold the information about the time warp and/or
    /// physics warp about the game.
    /// </summary>
    [kOS.Safe.Utilities.KOSNomenclature("Timewarp")]
    public class TimeWarpValue : Structure
    {
        private static TimeWarpValue selfSingleInstance = null;

        private TimeWarpValue()
        {
            InitializeSuffixes();
        }

        public static TimeWarpValue Instance
        {
            get
            {
                if (selfSingleInstance == null)
                    selfSingleInstance = new TimeWarpValue();
                return selfSingleInstance;
            }
        }

        private void InitializeSuffixes()
        {
            AddSuffix("RATE", new SetSuffix<ScalarValue>(GetRate, SetRate));
            AddSuffix("RATELIST", new Suffix<ListValue>(() => ListValue.CreateList(GetRateArrayForMode(TimeWarp.WarpMode))));
            AddSuffix("RAILSRATELIST", new Suffix<ListValue>(() => ListValue.CreateList(GetRateArrayForMode(TimeWarp.Modes.HIGH))));
            AddSuffix("PHYSICSRATELIST", new Suffix<ListValue>(() => ListValue.CreateList(GetRateArrayForMode(TimeWarp.Modes.LOW))));
            AddSuffix("MODE", new SetSuffix<StringValue>(GetModeAsString, SetModeAsString));
            AddSuffix("WARP", new SetSuffix<ScalarIntValue>(GetWarp, SetWarp));
            AddSuffix("WARPTO", new OneArgsSuffix<ScalarValue>(WarpTo));
            AddSuffix("CANCELWARP", new NoArgsVoidSuffix(CancelWarp));
            AddSuffix("PHYSICSDELTAT", new Suffix<ScalarValue>(GetDeltaT));
            AddSuffix("ISSETTLED", new Suffix<BooleanValue>(IsWarpSettled));
        }

        public ScalarValue GetRate()
        {
            return TimeWarp.CurrentRate;
        }

        // Set the rate to the nearest rate to the value given that the user interface
        // normally allows.  (i.e. if you tell it to set the rate to 90x, it will round
        // to 100x)  Note that the underlying API allows any in-between warp value, but
        // in keeping with the philosophy that kOS should only allow the user to do things
        // they could have done manually, we'll clamp it to the user interface's allowed
        // rates:
        public void SetRate(ScalarValue desiredRate)
        {
            float wantRate = desiredRate;
            float[] rateArray = GetRateArrayForMode(TimeWarp.WarpMode);

            // Walk the list of possible rates, settling on the one
            // that is closest to the desired rate (has smallest error
            // difference):
            float error = float.MaxValue;
            int index;
            for (index = 0; index < rateArray.Length; ++index)
            {
                float nextError = Mathf.Abs(wantRate - rateArray[index]);
                if (nextError > error)
                    break;
                error = nextError;
            }
            SetWarpRate(index - 1, rateArray.Length - 1);
        }

        public BooleanValue IsPhysicsWarping()
        {
            return TimeWarp.WarpMode == TimeWarp.Modes.LOW;
        }

        public StringValue GetModeAsString()
        {
            switch (TimeWarp.WarpMode)
            {
                case TimeWarp.Modes.LOW:
                    return "PHYSICS";
                case TimeWarp.Modes.HIGH:
                    return "RAILS";
                default:
                    return "UNDEFINED"; // This should never happen unless SQUAD adds more to the Modes enum.
            }
        }

        public void SetModeAsString(StringValue modeString)
        {
            switch (modeString.ToUpper())
            {
                case "PHYSICS":
                    TimeWarp.fetch.Mode = TimeWarp.Modes.LOW;
                    break;
                case "RAILS":
                    TimeWarp.fetch.Mode = TimeWarp.Modes.HIGH;
                    break;
                default:
                    throw new Exception(string.Format("WARP MODE '{0}' is not valid", modeString.ToString()));
            }
        }

        public ScalarIntValue GetWarp()
        {
            return TimeWarp.CurrentRateIndex;
        }

        public void SetWarp(ScalarIntValue newIndex)
        {
            int newRate = newIndex;
            switch (TimeWarp.WarpMode)
            {
                case TimeWarp.Modes.HIGH:
                    SetWarpRate(newRate, TimeWarp.fetch.warpRates.Length - 1);
                    break;
                case TimeWarp.Modes.LOW:
                    SetWarpRate(newRate, TimeWarp.fetch.physicsWarpRates.Length - 1);
                    break;
                default:
                    throw new Exception(string.Format("WARP MODE {0} unknown to kOS - did KSP's API change?", TimeWarp.WarpMode.ToString()));
            }
        }

        public ScalarValue GetDeltaT()
        {
            return TimeWarp.fixedDeltaTime;
        }

        public BooleanValue IsWarpSettled()
        {
            float expectedRate = GetRateArrayForMode(TimeWarp.WarpMode)[TimeWarp.CurrentRateIndex];

            // The comparison has to have a large-ish epsilon because sometimes the
            // current rate will eventually settle steady on something like 99.9813 or so
            // for 100x. (far bigger than one would expect for a normal floating point
            // equality epsilon):
            if (Mathf.Abs(expectedRate - TimeWarp.CurrentRate) < 0.05)
                return true;
            else
                return false;
        }

        public void WarpTo(ScalarValue timeStamp)
        {
            TimeWarp.fetch.WarpTo(timeStamp.GetDoubleValue());
        }

        /// <summary>
        /// Cancel any current auto-warping and reset the warp to default.
        /// </summary>
        public void CancelWarp()
        {
            TimeWarp.fetch.CancelAutoWarp();
            SetWarp(0);
        }

        // Return which of the two rates arrays is to be used for the given mode:
        private float[] GetRateArrayForMode(TimeWarp.Modes whichMode)
        {
            float[] rateArray;
            switch (whichMode)
            {
                case TimeWarp.Modes.HIGH:
                    rateArray = TimeWarp.fetch.warpRates;
                    break;
                case TimeWarp.Modes.LOW:
                    rateArray = TimeWarp.fetch.physicsWarpRates;
                    break;
                default:
                    throw new Exception(string.Format("WARP MODE {0} unknown to kOS - did KSP's API change?", TimeWarp.WarpMode.ToString()));
            }
            return rateArray;
        }

        private static void SetWarpRate(int newRate, int maxRate)
        {
            var clampedValue = Mathf.Clamp(newRate, 0, maxRate);
            if (clampedValue != newRate)
            {
                SafeHouse.Logger.Log(string.Format("Clamped Timewarp rate. Was: {0} Is: {1}", newRate, clampedValue));
            }
            TimeWarp.SetRate(clampedValue, false);
        }
    }
}