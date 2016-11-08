using kOS.Module;
using kOS.Safe.Utilities;
using System;
using System.Collections.Generic;

namespace kOS.Communication
{
    [AssemblyWalk(InterfaceType = typeof(IConnectivityManager), StaticRegisterMethod = "RegisterMethod")]
    public class ConnectivityManager
    {
        private static bool needEventInit = true;
        private static IConnectivityManager myinstance;

        public static IConnectivityManager Instance
        {
            get
            {
                if (myinstance == null)
                {
                    RefreshInstance();
                }
                return myinstance;
            }
        }

        public static void RefreshInstance()
        {
            if (needEventInit)
            {
                // KSP's events don't support pointing to a static method, so we need to wrap the call
                GameEvents.OnGameSettingsApplied.Add(() => { RefreshInstance(); });
                GameEvents.onGameStatePostLoad.Add(value => { RefreshInstance(); });
                needEventInit = false;
            }
            SafeHouse.Logger.SuperVerbose("ConnectivityManager.RefreshInstance()");
            if (myinstance == null || myinstance.GetType() != GetSelectedManagerType())
            {
                SafeHouse.Logger.SuperVerbose("RefreshInstance - change manager");
                myinstance = CreateManagerObject();
                if (myinstance == null || !myinstance.IsEnabled)
                {
                    SafeHouse.Logger.LogError("ConnectivityManager.RefreshInstance - Failed to instantiate " + GetSelectedManagerType().Name);
                    kOSConnectivityParameters.Instance.CheckNewManagers();
                }
            }
        }

        private static HashSet<Type> typeHash = new HashSet<Type>();

        public static void RegisterMethod(Type t)
        {
            typeHash.Add(t);
        }

        public static List<string> GetStringList()
        {
            var ret = new List<string>();
            foreach (Type t in typeHash)
            {
                if (t == typeof(PermitAllConnectivityManager))
                {
                    ret.Insert(0, t.Name);
                }
                else
                {
                    IConnectivityManager test = (IConnectivityManager)Activator.CreateInstance(t);
                    if (test.IsEnabled)
                    {
                        ret.Add(t.Name);
                    }
                }
            }
            return ret;
        }

        public static HashSet<string> GetStringHash()
        {
            var ret = new HashSet<string>();
            foreach (Type t in typeHash)
            {
                IConnectivityManager test = (IConnectivityManager)Activator.CreateInstance(t);
                if (test.IsEnabled)
                {
                    ret.Add(t.Name);
                }
            }
            return ret;
        }

        public static Type GetSelectedManagerType()
        {
            string name = kOSConnectivityParameters.Instance.connectivityHandler;
            return GetManagerType(name);
        }

        public static Type GetManagerType(string name)
        {
            foreach (Type t in typeHash)
            {
                if (t.Name.Equals(name))
                {
                    return t;
                }
            }
            return null;
        }

        public static IConnectivityManager CreateManagerObject()
        {
            Type t = GetSelectedManagerType();
            if (t == null)
                return new PermitAllConnectivityManager();
            return (IConnectivityManager)Activator.CreateInstance(GetSelectedManagerType());
        }

        public static bool NeedAutopilotResubscribe
        {
            get
            {
                return Instance.NeedAutopilotResubscribe;
            }
        }

        public static double GetDelay(Vessel vessel1, Vessel vessel2)
        {
            return Instance.GetDelay(vessel1, vessel2);
        }

        public static double GetDelayToHome(Vessel vessel)
        {
            return Instance.GetDelayToHome(vessel);
        }

        public static double GetDelayToControl(Vessel vessel)
        {
            return Instance.GetDelayToControl(vessel);
        }

        public static bool HasConnectionToHome(Vessel vessel)
        {
            return Instance.HasConnectionToHome(vessel);
        }

        public static bool HasConnectionToControl(Vessel vessel)
        {
            return Instance.HasConnectionToControl(vessel);
        }

        public static bool HasConnection(Vessel vessel1, Vessel vessel2)
        {
            return Instance.HasConnection(vessel1, vessel2);
        }

        public static void AddAutopilotHook(Vessel vessel, FlightInputCallback hook)
        {
            Instance.AddAutopilotHook(vessel, hook);
        }

        public static void RemoveAutopilotHook(Vessel vessel, FlightInputCallback hook)
        {
            Instance.RemoveAutopilotHook(vessel, hook);
        }
    }
}