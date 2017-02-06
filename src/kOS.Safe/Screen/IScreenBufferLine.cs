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
        string ToString(); // Am I allowed to put this in an interface?
    }
}