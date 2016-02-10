using kOS.Safe.Persistence;
using kOS.Safe.Screen;

namespace kOS.Factories
{
    public interface IFactory
    {
        IInterpreter CreateInterpreter(SharedObjects shared);
        Archive CreateArchive();
        IVolumeManager CreateVolumeManager(SharedObjects sharedObjects);
    }
}
