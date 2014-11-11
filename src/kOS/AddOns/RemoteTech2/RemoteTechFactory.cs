using kOS.Factories;
using kOS.Safe.Screen;
using kOS.Screen;
using kOS.Persistence;

namespace kOS.AddOns.RemoteTech2
{
    public class RemoteTechFactory : IFactory
    {
        public IInterpreter CreateInterpreter(SharedObjects shared)
        {
            return new RemoteTechInterpreter(shared);
        }

        public Archive CreateArchive()
        {
            return new RemoteTechArchive();
        }

        public VolumeManager CreateVolumeManager(SharedObjects sharedObjects)
        {
            return new RemoteTechVolumeManager(sharedObjects);
        }
    }
}
