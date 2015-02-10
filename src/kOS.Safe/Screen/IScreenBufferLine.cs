using System;

namespace kOS.Safe.Screen
{
    public interface IScreenBufferLine
    {
        DateTime LastChangeTime {get;}
        char[] ToArray();
        char this[int i] {get; set;}
        int Length {get;}
        void ArrayCopyFrom(IScreenBufferLine source, int sourceStart, int destinationStart, int length = -1);
        void ArrayCopyFrom(char[] source, int sourceStart, int destinationStart, int length = -1);
        string ToString();
        void TouchTime();
    }
}