namespace kOS.Safe.Encapsulation
{
    public interface ILexicon
    {
        object GetKey(object key);

        void SetKey(object index, object value);
    }
}