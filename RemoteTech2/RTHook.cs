using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace kOS
{
    public static class RTHook
    {
        public const String RemoteTechAssembly = "RemoteTech2";
        public const String RemoteTechApi = "RemoteTech.API";

        private static bool mHookFail;
        private static IRemoteTechAPIv1 mInstance;
        public static IRemoteTechAPIv1 Instance
        {
            get
            {
                if (mHookFail) return null;
                mInstance = mInstance ?? InitializeAPI();
                if (mInstance == null) mHookFail = true;
                return mInstance;
            }
        } 

        private static IRemoteTechAPIv1 InitializeAPI()
        {
            var loadedAssembly = AssemblyLoader.loadedAssemblies.FirstOrDefault(a => a.assembly.GetName().Name.Equals(RemoteTechAssembly));
            if (loadedAssembly == null) return null;

            var type = loadedAssembly.assembly.GetTypes().FirstOrDefault(t => t.FullName.Equals(RemoteTechApi));
            if (type == null) return null;

            var methods = type.GetMethods();
            var api = new RTInterfaceImplementation();

            try
            {
                foreach (var property in api.GetType().GetProperties())
                {
                    var method = methods.FirstOrDefault(m => { Debug.Log(m.Name); return m.Name.Equals(property.Name); });
                    if (method == null) throw new ArgumentNullException(property.Name);
                    var del = Delegate.CreateDelegate(property.PropertyType, type, method.Name);
                    property.SetValue(api, del, null);
                }
            }
            catch (Exception e)
            {
                Debug.Log("kOS: Error creating RemoteTech2 interface: " + e);
                return null;
            }

            Debug.Log("kOS: RemoteTech2 interface successfully created.");
            return api;
        }
    }

    internal class RTInterfaceImplementation : IRemoteTechAPIv1
    {
        public Func<Guid, bool> HasFlightComputer { get; internal set; }
        public Action<Guid, Action<FlightCtrlState>> AddSanctionedPilot { get; internal set; }
        public Action<Guid, Action<FlightCtrlState>> RemoveSanctionedPilot { get; internal set; }
        public Func<Guid, bool> HasAnyConnection { get; internal set; }
        public Func<Guid, bool> HasConnectionToKSC { get; internal set; }
        public Func<Guid, double> GetShortestSignalDelay { get; internal set; }
        public Func<Guid, double> GetSignalDelayToKSC { get; internal set; }
        public Func<Guid, Guid, double> GetSignalDelayToSatellite { get; internal set; }
    }
}
