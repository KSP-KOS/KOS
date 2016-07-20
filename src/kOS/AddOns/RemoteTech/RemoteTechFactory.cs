using kOS.Factories;
using kOS.Safe.Persistence;
using kOS.Safe.Screen;
using kOS.Communication;
using kOS.Safe.Utilities;

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
            return new RemoteTechArchive(SafeHouse.ArchiveFolder);
        }

        public IVolumeManager CreateVolumeManager(SharedObjects sharedObjects)
        {
            return new RemoteTechVolumeManager(sharedObjects);
        }

        public ConnectivityManager CreateConnectivityManager()
        {
            return new RemoteTechConnectivityManager();
        }
    }
}
