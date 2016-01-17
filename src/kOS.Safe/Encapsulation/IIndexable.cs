namespace kOS.Safe.Encapsulation
{
    public interface IIndexable
    {
        Structure GetIndex(Structure index);
        Structure GetIndex(int index);
        void SetIndex(Structure index, Structure value);
        void Structure SetIndex(int index, Structure value);
    }
}
