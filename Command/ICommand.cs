using kOS.Context;

namespace kOS.Command
{
    public interface ICommand : IExecutionContext
    {
        void Evaluate();
        void Refresh();
    }
}