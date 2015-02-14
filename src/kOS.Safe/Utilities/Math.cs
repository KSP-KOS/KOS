namespace kOS.Safe.Utilities
{
    public static class Math
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
    }
}
