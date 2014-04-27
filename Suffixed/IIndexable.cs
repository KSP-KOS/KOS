namespace kOS.Suffixed
{
    public interface IIndexable
    {
        object GetIndex(int index);
        void SetIndex(int index, object value);
    }
}
