using kOS.Persistance;

namespace kOS.Context
{
    public interface IContextRunProgram: IExecutionContext
    {
        void Run(File fileObj);
        string StripComment(string line);
        object PopParameter();
        string Filename { get; }
    }
}