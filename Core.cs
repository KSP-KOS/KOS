using KSP.IO;
using KSP.Testing;
using UnityEngine;
using kOS.Context;
using kOS.Suffixed;
using File = kOS.Persistance.File;

namespace kOS
{
    public class Core : MonoBehaviour
    {
        public static VersionInfo VersionInfo = new VersionInfo(0, 11.0);

        public static Core Fetch;
        public TerminalWindow Window;

        public void Awake()
        {
            // This thing gets instantiated 4 times by KSP for some reason
            if (Fetch != null) return;
            Fetch = this;

            var gObj = new GameObject("kOSTermWindow", typeof (TerminalWindow));
            DontDestroyOnLoad(gObj);
            Window = (TerminalWindow) gObj.GetComponent(typeof (TerminalWindow));
            Window.Core = this;
        }

        public void SaveSettings()
        {
            BinaryReader.CreateForType<File>(HighLogic.fetch.GameSaveFolder + "/");
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

    public class CoreInitializer : UnitTest
    {
        public CoreInitializer()
        {
            var gameobject = new GameObject("kOSCore", typeof (Core));
            Object.DontDestroyOnLoad(gameobject);
        }
    }
}