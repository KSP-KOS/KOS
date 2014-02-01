using kOS.Persistance;

namespace kOS.Context
{
    public interface IContextRunProgram : IExecutionContext
    {
        string Filename { get; }
        void Run(File fileObj);
        string StripComment(string line);
        object PopParameter();
    }
}