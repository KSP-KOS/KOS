using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using kOS.Value;


namespace kOS
{
    public class Core : MonoBehaviour
    {
        public static VersionInfo VersionInfo = new VersionInfo(0, 10.0);

        public static Core Fetch; 
        public TermWindow Window;
        private int CPUIdAccumulator;
        
        public void Awake()
        {
            if (Fetch == null) // This thing gets instantiated 4 times by KSP for some reason
            {
                Fetch = this;

                var gObj = new GameObject("kOSTermWindow", typeof(TermWindow));
                UnityEngine.Object.DontDestroyOnLoad(gObj);
                Window = (TermWindow)gObj.GetComponent(typeof(TermWindow));
                Window.Core = this;
            }
        }

        public void SaveSettings()
        {
            var writer = KSP.IO.BinaryReader.CreateForType<File>(HighLogic.fetch.GameSaveFolder + "/");
        }

        public static void Debug(String line)
        {
        }

        public static void OpenWindow(CPU cpu)
        {
            Fetch.Window.AttachTo(cpu);
            Fetch.Window.Open();
        }

        internal static void ToggleWindow(CPU cpu)
        {
            Fetch.Window.AttachTo(cpu);
            Fetch.Window.Toggle();
        }

        void OnGUI()
        {
        }

        public static void CloseWindow(CPU cpu)
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