using System;
using System.Collections.Generic;

namespace kOS.Safe.Utilities
{
    public static class KOSMath
    {
        public static float Clamp(float input, float low, float high)
        {
            return (input > high ? high : (input < low ? low : input));
        }

        public static double Clamp(double input, double low, double high)
        {
            return (input > high ? high : (input < low ? low : input));
        }

        public static double? Clamp(double? input, double low, double high)
        {
            if (!input.HasValue)
            {
                return null;
            }
            return Clamp(input.Value, low, high);
        }

        public static float? Clamp(float? input, float low, float high)
        {
            if (!input.HasValue)
            {
                return null;
            }
            return Clamp(input.Value, low, high);
        }

        /// <summary>
        /// Round the value to the nearest allowed value.
        /// if the slider starts at min, ends at max, and has detents every inc.
        /// </summary>
        /// <param name="val">value to round</param>
        /// <param name="min">minimum allowed value</param>
        /// <param name="max">maximum allowed value</param>
        /// <param name="increment">increment of the detents</param>
        /// <returns></returns>
        public static double ClampToIndent(double val, double min, double max, double increment)
        {
            // First clamp the value to within min/max:
            double outVal = System.Math.Max(min, System.Math.Min(val, max));
            
            // How many discrete increments up the slider gets us to the nearest detent less than or equal to the value:
            var numIncs = (int)System.Math.Floor((outVal-min)/increment);

            // get detent values just below and above the value:
            double nearUnder = min + (numIncs*increment);
            double nearOver = min + ((numIncs+1)*increment);
            
            // pick which one to round to:
            double remainder = outVal - nearUnder;
            if (remainder >= (increment/2f) && nearOver <= max)
                outVal = nearOver;
            else
                outVal = nearUnder;

            return outVal;
        }

        /// <summary>
        /// Round the value to the nearest allowed value.
        /// if the slider starts at min, ends at max, and has detents every inc.
        /// </summary>
        /// <param name="val">value to round</param>
        /// <param name="min">minimum allowed value</param>
        /// <param name="max">maximum allowed value</param>
        /// <param name="increment">increment of the detents</param>
        /// <returns></returns>
        public static float ClampToIndent(float val, float min, float max, float increment)
        {
            // First clamp the value to within min/max:
            float outVal = System.Math.Max(min, System.Math.Min(val, max));
            
            // How many discrete increments up the slider gets us to the nearest detent less than or equal to the value:
            var numIncs = (long)System.Math.Floor((outVal-min)/increment);

            // get detent values just below and above the value:
            float nearUnder = min + (numIncs*increment);
            float nearOver = min + ((numIncs+1)*increment);
            
            // pick which one to round to:
            float remainder = outVal - nearUnder;
            if (remainder >= (increment/2f) && nearOver <= max)
                outVal = nearOver;
            else
                outVal = nearUnder;

            return outVal;
        }

        /// <summary>
        ///   Fix the strange too-large or too-small angle degrees that are sometimes
        ///   returned by KSP, normalizing them into a constrained 360 degree range.
        /// </summary>
        /// <param name="inAngle">input angle in degrees</param>
        /// <param name="rangeStart">
        ///   Bottom of 360 degree range to normalize to.
        ///   ( 0 means the range [0..360]), while -180 means [-180,180] )
        /// </param>
        /// <returns>the same angle, normalized to the range given.</returns>
        public static double DegreeFix(double inAngle, double rangeStart)
        {
            double rangeEnd = rangeStart + 360.0;
            double outAngle = inAngle;
            while (outAngle > rangeEnd)
                outAngle -= 360.0;
            while (outAngle < rangeStart)
                outAngle += 360.0;
            return outAngle;
        }

        private static Dictionary<string, System.Random> randomizers = null;

        /// <summary>
        /// Begin a new random number sequence from an integer seed, giving it
        /// a string key to refer to later in GetRandom.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="seed">If null, no seed is given and it does the default seed</param>
        public static void StartRandomFromSeed(string key, int? seed)
        {
            if (randomizers == null)
                randomizers = new Dictionary<string, Random>(StringComparer.OrdinalIgnoreCase);
            if (!randomizers.ContainsKey(key))
                randomizers.Add(key, null);
            randomizers[key] = (seed == null ? new Random() : new System.Random(seed.Value));
        }

        /// <summary>
        /// Get the next number from a random number sequence given its string key
        /// you gave it in StartRandomFromSeed()
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static double GetRandom(string key)
        {
            if (randomizers == null)
                randomizers = new Dictionary<string, Random>(StringComparer.OrdinalIgnoreCase);
            if (!randomizers.ContainsKey(key))
                randomizers.Add(key, new Random());
            return randomizers[key].NextDouble();
        }
    }
}
