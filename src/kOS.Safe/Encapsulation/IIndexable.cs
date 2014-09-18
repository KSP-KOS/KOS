namespace kOS.Safe.Encapsulation
{
    public interface IIndexable
    {
        object GetIndex(int index);
        void SetIndex(int index, object value);
    }
}
