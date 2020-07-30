using kOS.Screen;
using kOS.Suffixed;
using UnityEngine;

namespace kOS.Module
{
    public class KOSNameTag : PartModule
    {
        private const string PAWGroup = "kOS";
		
        private KOSNameTagWindow typingWindow;

        [KSPField(isPersistant = true,
                  guiActive = true,
                  guiActiveEditor = true,
                  guiName = "name tag",
                  groupName = PAWGroup,
                  groupDisplayName = PAWGroup)]
        public string nameTag = "";

        [KSPEvent(guiActive = true,
                  guiActiveEditor = true,
                  guiName = "Change Name Tag",
                  groupName = PAWGroup,
                  groupDisplayName = PAWGroup)]
        public void PopupNameTagChanger()
        {
            if (typingWindow != null)
                typingWindow.Close();
            if (HighLogic.LoadedSceneIsEditor)
            {
                EditorFacility whichEditor = EditorLogic.fetch.ship.shipFacility;
                if (!(Career.CanTagInEditor(whichEditor)))
                {
                    var formattedString = string.Format("The {0} requires an upgrade to assign name tags", whichEditor);
                    ScreenMessages.PostScreenMessage(formattedString, 6, ScreenMessageStyle.UPPER_CENTER);
                    return;
                }
            }
            typingWindow = gameObject.AddComponent<KOSNameTagWindow>();
            typingWindow.Invoke(this, nameTag);
        }

        public void TypingDone(string newValue)
        {
            nameTag = newValue;
            TypingCancel();
        }

        public void TypingCancel()
        {
            typingWindow.Close();
            Destroy(typingWindow);
            typingWindow = null;
        }
    }
}