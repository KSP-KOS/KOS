using System;
using kOS.Screen;
using UnityEngine;

namespace kOS.Module
{
    public class KOSNameTag : PartModule
    {
        
        KOSNameTagWindow typingWindow = null;

        [KSPField(isPersistant = true,
                  guiActive = true,
                  guiActiveEditor = true,
                  guiName = "name tag")]
        public string nameTag = "<unassigned>";

        [KSPEvent(guiActive = true,
                  guiActiveEditor = true,
                  guiName = "Change Name Tag")]
        public void PopupNameTagChanger()
        {
            if (typingWindow != null)
                typingWindow.Close();
            GameObject gObj = new GameObject("nametag", typeof(KOSNameTagWindow) );
            DontDestroyOnLoad(gObj);
            typingWindow = (KOSNameTagWindow)gObj.GetComponent(typeof(KOSNameTagWindow));
            typingWindow.Invoke(this,nameTag);
        }
        
        public void TypingDone(string newValue)
        {
            nameTag = newValue;
            typingWindow.Close();
            typingWindow = null;
        }
        
    }
}
