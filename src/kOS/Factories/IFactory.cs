using kOS.Safe.Persistence;
using kOS.Safe.Screen;
using kOS.Persistence;

namespace kOS.Factories
{
    public interface IFactory
    {
        IInterpreter CreateInterpreter(SharedObjects shared);
        Archive CreateArchive();
        IVolumeManager CreateVolumeManager(SharedObjects sharedObjects);
    }
}
