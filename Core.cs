using System;
using UnityEngine;
using kOS.Context;
using kOS.Persistance;
using kOS.Suffixed;


namespace kOS
{
    public class Core : MonoBehaviour
    {
        public static VersionInfo VersionInfo = new VersionInfo(0, 10.0);

        public static Core Fetch; 
        public TerminalWindow Window;
        
        public void Awake()
        {
            // This thing gets instantiated 4 times by KSP for some reason
            if (Fetch != null) return;
            Fetch = this;

            var gObj = new GameObject("kOSTermWindow", typeof(TerminalWindow));
            DontDestroyOnLoad(gObj);
            Window = (TerminalWindow)gObj.GetComponent(typeof(TerminalWindow));
            Window.Core = this;
        }

        public void SaveSettings()
        {
            KSP.IO.BinaryReader.CreateForType<File>(HighLogic.fetch.GameSaveFolder + "/");
        }

        public static void Debug(string line)
        {
        }

        public static void OpenWindow(ICPU cpu)
        {
            Fetch.Window.AttachTo(cpu);
            Fetch.Window.Open();
        }

        internal static void ToggleWindow(ICPU cpu)
        {
            Fetch.Window.AttachTo(cpu);
            Fetch.Window.Toggle();
        }

        public static void CloseWindow(ICPU cpu)
        {
            Fetch.Window.AttachTo(cpu);
            Fetch.Window.Close();
        }
    }

    public class CoreInitializer : KSP.Testing.UnitTest
    {
        public CoreInitializer()
        {
            var gameobject = new GameObject("kOSCore", typeof(Core));
            UnityEngine.Object.DontDestroyOnLoad(gameobject);
        }
    }
}