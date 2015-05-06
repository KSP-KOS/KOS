using kOS.Factories;
using kOS.Safe.Persistence;
using kOS.Safe.Screen;

namespace kOS.AddOns.RemoteTech
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
