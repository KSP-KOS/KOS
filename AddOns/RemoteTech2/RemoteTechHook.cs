using System;
using System.Linq;
using kOS.Suffixed;

namespace kOS.AddOns.RemoteTech2
{
    public static class RemoteTechHook
    {
        private const String REMOTE_TECH_ASSEMBLY = "RemoteTech2";
        private const String REMOTE_TECH_API = "RemoteTech.API";

        private static bool hookFail;
        private static IRemoteTechAPIv1 instance;
        public static IRemoteTechAPIv1 Instance
        {
            get
            {
                if (hookFail) return null;
                instance = instance ?? InitializeAPI();
                if (instance == null) hookFail = true;
                return instance;
            }
        }

        public static bool IsAvailable(Guid vesselId)
        {
            try
            {
                var isAvailableBase = IsAvailable();
                if (!isAvailableBase)
                {
                    return false;
                }
                var hasFlightComputer = Instance.HasFlightComputer(vesselId);
                return isAvailableBase && hasFlightComputer;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool IsAvailable()
        {
            var integrationEnabled = Config.GetInstance().EnableRT2Integration;
            if (!integrationEnabled)
            {
                return false;
            }
            var instanceAvailable = Instance != null;
            return integrationEnabled && instanceAvailable;
        }

        private static IRemoteTechAPIv1 InitializeAPI()
        {
            var loadedAssembly = AssemblyLoader.loadedAssemblies.FirstOrDefault(a => a.assembly.GetName().Name.Equals(REMOTE_TECH_ASSEMBLY));
            if (loadedAssembly == null) return null;

            var type = loadedAssembly.assembly.GetTypes().FirstOrDefault(t => t.FullName.Equals(REMOTE_TECH_API));
            if (type == null) return null;

            var methods = type.GetMethods();
            var api = new RemoteTechAPI();

            try
            {
                foreach (var property in api.GetType().GetProperties())
                {
                    var method = methods.FirstOrDefault(m => { UnityEngine.Debug.Log(m.Name); return m.Name.Equals(property.Name); });
                    if (method == null) throw new ArgumentNullException(property.Name);
                    var del = Delegate.CreateDelegate(property.PropertyType, type, method.Name);
                    property.SetValue(api, del, null);
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.Log("kOS: Error creating RemoteTech2 interface: " + e);
                return null;
            }

            UnityEngine.Debug.Log("kOS: RemoteTech2 interface successfully created.");
            return api;
        }
    }
}
