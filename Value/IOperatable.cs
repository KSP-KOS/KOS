namespace kOS.Value
{
    public interface IOperatable
    {
        object TryOperation(string op, object other, bool reverseOrder);
    }
}