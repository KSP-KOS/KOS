using kOS.Screen;
using kOS.Suffixed;
using UnityEngine;

namespace kOS.Module
{
    public class KOSNameTag : PartModule
    {
        private KOSNameTagWindow typingWindow;

        [KSPField(isPersistant = true,
                  guiActive = true,
                  guiActiveEditor = true,
                  guiName = "name tag")]
        public string nameTag = "";

        [KSPEvent(guiActive = true,
                  guiActiveEditor = true,
                  guiName = "Change Name Tag")]
        public void PopupNameTagChanger()
        {
            if (typingWindow != null)
                typingWindow.Close();
            if (HighLogic.LoadedSceneIsEditor)
            {
                EditorFacility whichEditor = EditorLogic.fetch.ship.shipFacility;
                if (!(Career.CanTagInEditor(whichEditor)))
                {
                    ScreenMessages.PostScreenMessage("The "+whichEditor.ToString()+" requires an upgrade to assign name tags",
                                                     6,
                                                     ScreenMessageStyle.UPPER_CENTER);
                    return;
                }
            }
            GameObject gObj = new GameObject("nametag", typeof(KOSNameTagWindow) );
            DontDestroyOnLoad(gObj);
            typingWindow = (KOSNameTagWindow)gObj.GetComponent(typeof(KOSNameTagWindow));
            typingWindow.Invoke(this,nameTag);
        }
        
        public void TypingDone(string newValue)
        {
            nameTag = newValue;
            TypingCancel();
        }
        
        public void TypingCancel()
        {
            typingWindow.Close();
            typingWindow = null;
        }
        
    }
}
