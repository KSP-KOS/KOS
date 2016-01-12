namespace kOS.Safe.Encapsulation
{
    public interface IIndexable
    {
        Structure GetIndex(Structure index);
        void SetIndex(Structure index, Structure value);
    }
}
