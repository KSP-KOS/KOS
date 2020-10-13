using kOS.Screen;
using kOS.Suffixed;
using kOS.Safe.Utilities;
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
            // Make a new instance of typingWindow, replacing the existing one if there was one:
            KOSNameTagWindow oldTypingWindow = gameObject.GetComponent<KOSNameTagWindow>();
            if (oldTypingWindow != null)
            Destroy(oldTypingWindow);
            typingWindow = gameObject.AddComponent<KOSNameTagWindow>();
            typingWindow.Invoke(this, nameTag);
        }

        // For issue #2764, this enforces a rule that says regardless of what ModuleManager
        // rules end up doing, there shall only ever be one KosNameTag per part:
        public override void OnAwake()
        {
            // If other instances of me exist in this part, remove them.  I am replacing them:
            for (int i = part.Modules.Count - 1; i >= 0; --i)
            {
                PartModule pm = part.Modules[i];
                if (pm != this && pm is KOSNameTag)
                {
                    SafeHouse.Logger.Log(string.Format(
                        "Removing duplicate KOSNameTag PartModule from {0}.  KOS cannot deal with more than one tag per part.", part.name));
                    part.RemoveModule(pm);
                }
            }
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