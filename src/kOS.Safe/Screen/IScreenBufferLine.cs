namespace kOS.Safe.Screen
{
    public interface IScreenBufferLine
    {
        ulong LastChangeTick {get;}
        char[] ToArray();
        char this[int i] {get; set;}
        int Length {get;}
        void ArrayCopyFrom(IScreenBufferLine source, int sourceStart, int destinationStart, int length = -1);
        void ArrayCopyFrom(char[] source, int sourceStart, int destinationStart, int length = -1);
        void TouchTime();

        /// <summary>
        /// Set a single character without changing the timestamp. Should be used carefully in conjunction with SetTimestamp,
        /// e.g. by SubBuffer.MergeTo
        /// </summary>
        /// <param name="i">Target Index</param>
        /// <param name="c">Value to set</param>
        void SetCharIgnoreTime(int i, char c);

        /// <summary>
        /// Manually set the change timestamp. Should be used carefully in conjunction with SetCharIgnoreTime, e.g. by SubBuffer.MergeTo
        /// </summary>
        /// <param name="timestamp">Timestamp to set</param>
        void SetTimestamp(ulong timestamp);
    }
}