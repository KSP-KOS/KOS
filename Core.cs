using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using kOS.Screen;
using kOS.Suffixed;

namespace kOS
{
    public class Core : MonoBehaviour
    {
        public static VersionInfo VersionInfo = new VersionInfo(0, 11.0);

        public static Core Fetch; 
        public TermWindow Window;
        
        public void Awake()
        {
            // This thing gets instantiated 4 times by KSP for some reason
            if (Fetch != null) return;
            Fetch = this;

            var gObj = new GameObject("kOSTermWindow", typeof(TermWindow));
            UnityEngine.Object.DontDestroyOnLoad(gObj);
            Window = (TermWindow)gObj.GetComponent(typeof(TermWindow));
        }

        public void SaveSettings()
        {
            //var writer = KSP.IO.BinaryReader.CreateForType<File>(HighLogic.fetch.GameSaveFolder + "/");
        }

        public static void Debug(string line)
        {
        }

        public static void OpenWindow(SharedObjects shared)
        {
            Fetch.Window.AttachTo(shared);
            Fetch.Window.Open();
        }

        internal static void ToggleWindow(SharedObjects shared)
        {
            Fetch.Window.AttachTo(shared);
            Fetch.Window.Toggle();
        }

        void OnGUI()
        {
        }

        public static void CloseWindow(SharedObjects shared)
        {
            Fetch.Window.AttachTo(shared);
            Fetch.Window.Close();
        }
    }

    public class CoreInitializer : KSP.Testing.UnitTest
    {
        public CoreInitializer()
        {
            var gameobject = new GameObject("kOSCore", typeof (Core));
            UnityEngine.Object.DontDestroyOnLoad(gameobject);
        }
    }
}
