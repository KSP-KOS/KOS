using kOS.Screen;
using kOS.Persistence;

namespace kOS.Factories
{
    public interface IFactory
    {
        Interpreter CreateInterpreter(SharedObjects shared);
        Archive CreateArchive();
    }
}
