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
    }
}
