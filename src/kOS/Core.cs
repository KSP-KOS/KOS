using UnityEngine;
using kOS.Safe.Encapsulation;
using kOS.Screen;
using kOS.Suffixed;

namespace kOS
{
    public class Core : MonoBehaviour
    {
        public static VersionInfo VersionInfo = new VersionInfo(0, 14.1);

        public static Core Fetch; 
        
        public void Awake()
        {
            // This thing gets instantiated 4 times by KSP for some reason
            if (Fetch != null) return;
            Fetch = this;

        }

        public void SaveSettings()
        {
            //var writer = KSP.IO.BinaryReader.CreateForType<File>(HighLogic.fetch.GameSaveFolder + "/");
        }

        public static void Debug(string line)
        {
        }

        void OnGUI()
        {
        }

    }

    public class CoreInitializer : KSP.Testing.UnitTest
    {
        public CoreInitializer()
        {
            var gameobject = new GameObject("kOSCore", typeof (Core));
            Object.DontDestroyOnLoad(gameobject);
        }
    }
}
