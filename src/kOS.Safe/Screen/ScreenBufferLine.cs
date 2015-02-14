using kOS.Safe.Utilities;
using System;

namespace kOS.Safe.Screen
{
    /// <summary>
    /// This is basically just an array of chars, but one in which it keeps track of the timestamp
    /// of the most recent alteration of the contents.  It's not particularly vigorous about catching
    /// all the possible ways one could change the contents.
    /// <br/>
    /// To keep it simple, not all the possible array operations have been implemented here,
    /// just enough to work for the accesses that we use elsewhere in the code.
    /// </summary>
    public class ScreenBufferLine : IScreenBufferLine
    {
        private readonly char[] charArray;

        /// <summary>
        /// A number representing a count that can be used to check which changed
        /// line is newer.  It doesn't store the real time, just an ever-increasing
        /// counter taken from the TickGen class.  The purpose is to guarantee
        /// that later numbers are marked with a later timestamp no matter what.
        /// The .Net built-in DateTime.Now is insufficient for this purpose because it
        /// returns the same fixed value for a few milliseconds before updating itself,
        /// leading to a lot of 'ties' if its used as timestamp on code that runs fast.
        /// </summary>
        public ulong LastChangeTick { get; private set; }

        public int Length { get { return charArray.Length; } }

        /// <summary>
        /// Return a copy of the contents array.  It's a copy, not the internal array,
        /// so altering it won't change the actual values or update the timestamp.
        /// </summary>
        /// <returns></returns>
        public char[] ToArray()
        {
            char[] safeOutputOnlyCopy = new Char[charArray.Length];
            Array.Copy(charArray, safeOutputOnlyCopy, charArray.Length);
            return safeOutputOnlyCopy;
        }

        /// <summary>
        /// Perform the array index operator, updating the change time if a value is changed.
        /// </summary>
        public char this[int i] { get { return charArray[i]; } set { charArray[i] = value; TouchTime(); } }

        /// <summary>
        /// Constructor given the array size.  This fills the same role as doing:<br/>
        ///    new char[size];<br/>
        /// would in a normal vanilla char aray.<br/>
        /// </summary>
        /// <param name="size">make the char array be this length</param>
        public ScreenBufferLine(int size)
        {
            charArray = new char[size];
        }

        /// <summary>
        /// Perform the Array.Copy() function.
        /// Copies a subrange of values from a source array into this array.
        /// <param name="source">copy from here</param>
        /// <param name="sourceStart">starting at this index</param>
        /// <param name="destinationStart">putting it into this index of me</param>
        /// <param name="length">this many characters.  Optional.  If left off the whole source array is copied</param>
        /// </summary>
        public void ArrayCopyFrom(char[] source, int sourceStart, int destinationStart, int length = -1)
        {
            Array.Copy(source, sourceStart, charArray, destinationStart, (length >= 0 ? length : source.Length));
            TouchTime();
        }

        /// <summary>
        /// Perform the Array.Copy() function.
        /// Copies a subrange of values from a source array into this array.
        /// </summary>
        /// <param name="source">copy from here</param>
        /// <param name="sourceStart">starting at this index</param>
        /// <param name="destinationStart">putting it into this index of me</param>
        /// <param name="length">this many characters.  Optional.  If left off the whole source array is copied</param>
        public void ArrayCopyFrom(IScreenBufferLine source, int sourceStart, int destinationStart, int length = -1)
        {
            ArrayCopyFrom(source.ToArray(), sourceStart, destinationStart, (length >= 0 ? length : source.Length));
            TouchTime();
        }

        public void TouchTime()
        {
            LastChangeTick = TickGen.Next;
        }

        public override string ToString()
        {
            return new String(charArray);
        }
    }
}