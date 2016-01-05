namespace kOS.Safe.Encapsulation
{
    public interface IIndexable
    {
        object GetIndex(object index);
        void SetIndex(object index, object value);
    }
}
