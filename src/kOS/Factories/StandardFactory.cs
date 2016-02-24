using kOS.Safe.Persistence;
using kOS.Safe.Screen;
using kOS.Screen;

namespace kOS.Factories
{
    public class StandardFactory : IFactory
    {
        public IInterpreter CreateInterpreter(SharedObjects shared)
        {
            return new Interpreter(shared);
        }

        public Archive CreateArchive()
        {
            return new Archive();
        }

        public IVolumeManager CreateVolumeManager(SharedObjects sharedObjects)
        {
            return new VolumeManager();
        }
    }
}
