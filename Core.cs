using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using kOS.Screen;

namespace kOS
{
    public class Core : MonoBehaviour
    {
        public static kOS.Suffixed.VersionInfo VersionInfo = new kOS.Suffixed.VersionInfo(0, 9.3);

        public static Core Fetch; 
        public TermWindow Window;
        private int CPUIdAccumulator;
        private SharedObjects _shared;
        
        public void Awake()
        {
            if (Fetch == null) // This thing gets instantiated 4 times by KSP for some reason
            {
                Fetch = this;

                var gObj = new GameObject("kOSTermWindow", typeof(TermWindow));
                UnityEngine.Object.DontDestroyOnLoad(gObj);
                Window = (TermWindow)gObj.GetComponent(typeof(TermWindow));
            }
        }

        public void SaveSettings()
        {
            //var writer = KSP.IO.BinaryReader.CreateForType<File>(HighLogic.fetch.GameSaveFolder + "/");
        }

        public static void Debug(String line)
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