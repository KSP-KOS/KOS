namespace kOS.Safe.Encapsulation
{
    public interface IOperable
    {
        object TryOperation(string op, object other, bool reverseOrder);
    }
}