using kOS.Safe.Screen;
using kOS.Persistence;

namespace kOS.Factories
{
    public interface IFactory
    {
        IInterpreter CreateInterpreter(SharedObjects shared);
        Archive CreateArchive();
    }
}
