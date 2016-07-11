using kOS.Safe.Persistence;
using kOS.Safe.Screen;
using kOS.Screen;
using kOS.Communication;
using kOS.Safe.Utilities;

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
            return new Archive(SafeHouse.ArchiveFolder);
        }

        public IVolumeManager CreateVolumeManager(SharedObjects sharedObjects)
        {
            return new VolumeManager();
        }

        public ConnectivityManager CreateConnectivityManager()
        {
            return new StockConnectivityManager();
        }
    }
}
