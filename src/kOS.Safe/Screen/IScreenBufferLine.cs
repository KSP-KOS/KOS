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
        string ToString(); // Does this do anything?  Or will the generic object.ToString() always satisfy it when the class never explicitly makes one?
    }
}