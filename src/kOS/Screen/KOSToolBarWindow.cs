using System;
using System.Linq;

using UnityEngine;
using kOS.Utilities;
using kOS.Module;
using kOS.Suffixed;

namespace kOS.Screen
{
    /// <summary>
    /// Window that holds the popup that the toolbar button is meant to create.
    /// Note that there should only be one of these at a time, unlike some of the
    /// other KOSManagedWindows.
    /// <br></br>
    /// Frustratingly, The only two choices that KSP gives for the boolean 
    /// value "once" in the KSPAddon attribute are these:<br/>
    /// <br/>
    /// Set it to True to have your class instanced only exactly once in the entire game.<br/>
    /// Set it to False to have your class instanced about 5-6 times per scene change.<br/>
    /// <br/>
    /// The sane behavior, of "instance it exactly once each time the scene changes, and no more"
    /// does not seem to be an option.  Therefore this class has a lot of silly counters to
    /// track how many times its been instanced.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.EditorAny | KSPAddon.Startup.Flight, false)]
    public class KOSToolBarWindow : MonoBehaviour
    {
        private ApplicationLauncherButton launcherButton;
        
        private ApplicationLauncher.AppScenes appScenes = 
            ApplicationLauncher.AppScenes.FLIGHT |
            ApplicationLauncher.AppScenes.SPH |
            ApplicationLauncher.AppScenes.VAB |
            ApplicationLauncher.AppScenes.MAPVIEW;
        
        private Texture2D launcherButtonTexture = new Texture2D(0, 0, TextureFormat.DXT1, false);
        
        private bool clickedOn = false;
        private float height = 1f; // will be automatically resized by GUILayout.
        private float width = 1f; // will be automatically resized by GUILayout.
        private float minWidth = 220f; // keeps GUILayout from being stupid.

/* -------------------------------------------
 * OLD WAY OF ADDING KOSNameTags, now abandoned
 * -------------------------------------------
 * Save these commented-out bits of code in a github commit so if you ever want to do
 * anything where you have to
 * click on a part and need to discover which part it is, you can look to this example
 * to see how that is done.
 * Once this commented section gets merged into at least one commit to develop, then
 * it can be removed in the future.  I just don't want this hard work forgotten.  This
 * is no longer necessary because we had to change the design of how KOSNameTag is used
 * so it now is ModuleManager'ed onto every part in the game.
 * -----------------------------------------------------------------------------------------
 * 
 * 
        
        private ScreenMessage partClickMsg = null;
        private KOSNameTag nameTagModule = null;
        private global::Part currentHoverPart = null;

        private bool isAssigningPart = false;

 * END Commented-out section
 * --------------------------
 */         
        private Rect windowRect;
        private int uniqueId = 8675309; // Jenny, I've got your number.
        private GUISkin panelSkin;
        private GUIStyle headingLabelStyle;
        private GUIStyle tooltipLabelStyle;
        private GUIStyle smallToggleStyle;
        private string versionString;

        
        // Some of these are for just debug messages, and others are
        // necessary for tracking things to make it not spawn too many
        // buttons or spawn them at the wrong times.  For now I want to
        // keep the debug logging in the code so users have something they
        // can show in bug reports until I'm more confident this is working
        // perfectly:
        
        private bool alreadyAwake = false;
        private static int  countInstances = 0;
        private int myInstanceNum = 0;
        private bool thisInstanceHasHooks = false;
        private static bool someInstanceHasHooks = false;
        private bool isOpen = false;
        private bool onGUICalledThisInstance = false;
        private bool onGUIWasOpenThisInstance = false;
        
        /// <summary>
        /// Unity hates it when a MonoBehaviour has a constructor,
        /// so all the construction work is here instead:
        /// </summary>
        public void FirstTimeSetup()
        {
            ++countInstances;
            myInstanceNum = countInstances;
            Debug.Log("KOSToolBarWindow: Now making instance number "+myInstanceNum+" of KOSToolBarWindow");

            string relPath = "GameData/kOS/GFX/launcher-button.png";

            WWW imageFromURL = new WWW("file://" + KSPUtil.ApplicationRootPath.Replace("\\", "/") + relPath);
            imageFromURL.LoadImageIntoTexture(launcherButtonTexture);

            windowRect = new Rect(0,0,width,height); // this origin point will move when opened/closed.
            panelSkin = BuildPanelSkin();
            versionString = Utils.GetAssemblyFileVersion();
            
            GameEvents.onGUIApplicationLauncherReady.Add(RunWhenReady);
            GameEvents.onGUIApplicationLauncherDestroyed.Add(GoAway);
        }
        
        public void Awake()
        {
            // Awake gets called a stupid number of times.  This
            // ensures only one of them per instance actually happens:
            if (alreadyAwake) return;
            alreadyAwake = true;

            FirstTimeSetup();
        }
        
        public void RunWhenReady()
        {
            Debug.Log("KOSToolBarWindow: Instance number " + myInstanceNum + " is trying to ready the hooks");
            // KSP claims the hook ApplicationLauncherReady.Add will not run until
            // the application is ready, even though this is emphatically false.  It actually
            // fires the event a few times before the one that "sticks" and works:
            if (!ApplicationLauncher.Ready) return; 
            if (someInstanceHasHooks) return;
            thisInstanceHasHooks = true;
            someInstanceHasHooks = true;
            
            Debug.Log("KOSToolBarWindow: Instance number " + myInstanceNum + " will now actually make its hooks");
            ApplicationLauncher launcher = ApplicationLauncher.Instance;
            
            launcherButton = launcher.AddModApplication(
                CallbackOnTrue,
                CallbackOnFalse,
                CallbackOnHover,
                CallbackOnHoverOut,
                CallbackOnEnable,
                CallbackOnDisable,
                appScenes,
                launcherButtonTexture);
                
            launcher.AddOnShowCallback(CallbackOnShow);
            launcher.AddOnHideCallback(CallbackOnHide);
            launcher.EnableMutuallyExclusive(launcherButton);
        }
        
        public void GoAway()
        {
            Debug.Log("KOSToolBarWindow: PROOF: Instance " + myInstanceNum + " is in GoAway().");
            if (thisInstanceHasHooks)
            {
                Debug.Log("KOSToolBarWindow: PROOF: Instance " + myInstanceNum + " has hooks and is entering the guts of GoAway().");
                if (isOpen) Close();
                clickedOn = false;
                thisInstanceHasHooks = false;
                someInstanceHasHooks = false; // if this is the instance that had hooks and it's going away, let another instance have a go.
            
                ApplicationLauncher launcher = ApplicationLauncher.Instance;
                
                launcher.DisableMutuallyExclusive(launcherButton);
                launcher.RemoveOnRepositionCallback(CallbackOnShow);
                launcher.RemoveOnHideCallback(CallbackOnHide);
                launcher.RemoveOnShowCallback(CallbackOnShow);
            
                launcher.RemoveModApplication(launcherButton);
            }
        }
        
        public void OnDestroy()
        {
            GoAway();
        }
                        
        /// <summary>Callback for when the button is toggled on</summary>
        public void CallbackOnTrue()
        {
            Debug.Log("KOSToolBarWindow: PROOF: CallbackOnTrue()");
            clickedOn = true;
            Open();
        }

        /// <summary>Callback for when the button is toggled off</summary>
        public void CallbackOnFalse()
        {            
            Debug.Log("KOSToolBarWindow: PROOF: CallbackOnFalse()");
            clickedOn = false;
            Close();
        }

        /// <summary>Callback for when the mouse is hovering over the button</summary>
        public void CallbackOnHover()
        {            
            Debug.Log("KOSToolBarWindow: PROOF: CallbackOnHover()");
            if (!clickedOn)
                Open();
        }

        /// <summary>Callback for when the mouse is hover is off the button</summary>
        public void CallbackOnHoverOut()
        {            
            Debug.Log("KOSToolBarWindow: PROOF: CallbackOnHoverOut()");
            if (!clickedOn)
                Close();
        }

        /// <summary>Callback for when the mouse is hovering over the button</summary>
        public void CallbackOnShow()
        {            
            Debug.Log("KOSToolBarWindow: PROOF: CallbackOnShow()");
            if (!clickedOn && !isOpen)
                Open();
        }

        /// <summary>Callback for when the mouse is hover is off the button</summary>
        public void CallbackOnHide()
        {            
            Debug.Log("KOSToolBarWindow: PROOF: CallbackOnHide()");
            if (!clickedOn && isOpen)
            {
                Close();
                clickedOn = false;
            }
        }
        
        public void Open()
        {
            Debug.Log("KOSToolBarWindow: PROOF: Open()");
            
            bool isTop = ApplicationLauncher.Instance.IsPositionedAtTop;

            // Left edge is offset from the right
            // edge of the screen by enough to hold the width of the window and maybe more offset
            // if in the editor where there's a staging list we don't want to cover up:
            float leftEdge = ( (UnityEngine.Screen.width - width) - (HighLogic.LoadedSceneIsEditor ? 64f : 0) );

            // Top edge is either just under the button itself (which contains 40 pixel icons), or just above
            // the screen bottom by enough room to hold the window height plus the 40 pixel icons):
            float topEdge = isTop ? (40f) : (UnityEngine.Screen.height - (height+40) );
            
            windowRect = new Rect(leftEdge, topEdge, 0, 0); // will resize upon first GUILayout-ing.
            Debug.Log("KOSToolBarWindow: PROOF: Open(), windowRect = " + windowRect);
            
            isOpen = true;
        }

        public void Close()
        {
            Debug.Log("KOSToolBarWindow: PROOF: Close()");
            if (! isOpen)
                return;

            isOpen = false;
            /* isAssigningPart = false; */ // See comments further down that say: "OLD WAY OF ADDING KOSNameTags"
        }

        /// <summary>Callback for when the button is shown or enabled by the application launcher</summary>
        public void CallbackOnEnable()
        {
            Debug.Log("KOSToolBarWindow: PROOF: CallbackOnEnable()");
            // do nothing, but leaving the hook here as a way to document "this thing exists and might be used".
        }
        
        /// <summary>Callback for when the button is hidden or dinabled by the application launcher</summary>
        public void CallbackOnDisable()
        {            
            Debug.Log("KOSToolBarWindow: PROOF: CallbackOnDisable()");
            // do nothing, but leaving the hook here as a way to document "this thing exists and might be used".
        }
        
        private global::Part GetPartFromRayCast(Ray ray)
        {
            RaycastHit hit;
            global::Part returnMe = null;
            if (Physics.Raycast(ray, out hit, 999999f))
            {
                if (hit.collider != null && hit.collider.gameObject != null)
                {
                    GameObject gObject = hit.collider.gameObject;
                    // That found the innermost hit - need to walk up chain of parents until
                    // hitting a Unity GameObject that is also a KSP part.
                    while (true) // (there is an explicit break in the loop).
                    {
                        returnMe = Part.FromGO(gObject);
                        if (returnMe == null && gObject != null &&
                            gObject.transform.parent != null &&
                            gObject.transform.parent.gameObject != null)
                        {
                            gObject = gObject.transform.parent.gameObject;
                        }
                        else
                        {
                            break; // quits when either returnMe is found, or a null was hit walking up the parent chain.
                        }
                    }
                }
            }
            return returnMe;
        }

        public void OnGUI()
        {
            if (!onGUICalledThisInstance) // I want proof it was called, but without spamming the log:
            {
                Debug.Log("KOSToolBarWindow: PROOF: OnGUI() was called at least once on instance number " + myInstanceNum);
                onGUICalledThisInstance = true;
            }
            
            if (!isOpen ) return;

            if (!onGUIWasOpenThisInstance) // I want proof it was called, but without spamming the log:
            {
                Debug.Log("KOSToolBarWindow: PROOF: OnGUI() was called while the window was supposed to be open at least once on instance number " + myInstanceNum);
                onGUIWasOpenThisInstance = true;
            }
            
            GUI.skin = panelSkin;
            
            windowRect = GUILayout.Window(uniqueId, windowRect, DrawWindow,"kOS " + versionString, GUILayout.MinWidth(minWidth));

            width = windowRect.width;
            height = windowRect.height;

/* -------------------------------------------
 * OLD WAY OF ADDING KOSNameTags, now abandoned
 * -------------------------------------------
            
            if (isAssigningPart)
            {
                Event e = Event.current;
                
                if (e.type == EventType.mouseDown)
                {
                    if (currentHoverPart != null)
                    {
                        // If it doesn't already have the module, add it on, else get the one
                        // that's already on it.
                        KOSNameTag nameTag = currentHoverPart.Modules.OfType<KOSNameTag>().FirstOrDefault();
                        if (nameTag==null)
                        {
                            currentHoverPart.AddModule("KOSNameTag");
                            nameTag = currentHoverPart.Modules.OfType<KOSNameTag>().FirstOrDefault();
                        }
                        
                        // Invoke the name tag changer now that the part has the module on it:
                        nameTag.PopupNameTagChanger();
                    }
                    EndAssignPartMode();

                    // Tell the flags to stop looking for a click on a part:
                    // NOTE this executes whether a part was hit or not,
                    // because clicking the screen somwhere not on a part should cancel the mode.
                    if (HighLogic.LoadedSceneIsEditor)
                        EditorLogic.fetch.Unlock("KOSNameTagAddingLock");

                    e.Use();
                }
            }

 * END Commented-out section
 * --------------------------
 */

        }
        
        public void DrawWindow(int windowID)
        {
/* -------------------------------------------
 * OLD WAY OF ADDING KOSNameTags, now abandoned
 * -------------------------------------------
            if (GUILayout.Button("Add KOS Nametag to a part"))
            {
                BeginAssignPartMode();
                if (HighLogic.LoadedSceneIsEditor)
                    EditorLogic.fetch.Lock(false,false,false,"KOSNameTagAddingLock");
            }
            GUILayout.Label("eraseme: I am instance number " + myInstanceNum);
 * END Commented-out section
 * --------------------------
 */

            GUILayout.Box("",GUILayout.ExpandWidth(true)); // Just draw an empty horizontal bar under the title.

            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical();
            GUILayout.Label("\n\nComing soon... \n  watch this space\n  for more content\n  in future versions.");
            GUILayout.EndVertical();

            GUILayout.Box("",GUILayout.ExpandHeight(true)); // just draw an empty vertical bar as a separator.
            
            GUILayout.BeginVertical();
            GUILayout.Label("CONFIG VALUES", headingLabelStyle);
            GUILayout.Label("Changes to these settings are saved and globally affect all saved games.", tooltipLabelStyle);

            foreach (ConfigKey key in Config.Instance.GetConfigKeys())
            {
                GUILayout.BeginHorizontal();

                string labelText = key.Alias;
                string toolTipText = key.Name;

                if (key.Value is bool)
                {
                    key.Value = GUILayout.Toggle((bool)key.Value,"");
                }
                else if (key.Value is int)
                {
                    string newStringVal = GUILayout.TextField(key.Value.ToString(), 6, GUILayout.Width(50));
                    int newInt;
                    if (int.TryParse(newStringVal, out newInt))
                        key.Value = newInt;
                    // else it reverts to what it was and wipes the typing if you don't assign it to anything.
                }
                else
                {
                    GUILayout.Label(key.Alias + " is a new type this dialog doesn't support.  Contact kOS devs.");
                }
                GUILayout.Label(new GUIContent(labelText,toolTipText));

                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            // This is where tooltip hover text will show up, rather than in a hover box wherever the poiner is like normal.
            // Unity doesn't do hovering tooltips and you have to specify a zone for them to appear like this:
            GUILayout.Label(GUI.tooltip, tooltipLabelStyle);
        }
        
        private GUISkin BuildPanelSkin()
        {
            GUISkin theSkin = new GUISkin();

            // If we just did something like this:
            //     theSkin = HighLogic.Skin;
            //     theSkin.label.fontSize=10;
            // Then theSkin would have just been a reference to the same skin
            // everybody else uses in KSP and therefore it would have altered
            // the look of all the KSP windows from stock and all the other mods.
            // (Yes, this is what happened the first time I tried this).
            //
            // Therefore we want to make theSkin a deep copy of HighLogic.Skin, then
            // alter it after making the copy, but GUISkin has no copy constructor.
            // The individual GUIStyle's inside it do, however, and that is what
            // causes this next block of code.  It's starting off theSkin as a deep
            // copy of HighLogic.Skin, at least as much as it can:
            //
            theSkin.window      = new GUIStyle(HighLogic.Skin.window);
            theSkin.button      = new GUIStyle(HighLogic.Skin.button);
            theSkin.box         = new GUIStyle(HighLogic.Skin.box);
            theSkin.label       = new GUIStyle(HighLogic.Skin.label);
            theSkin.toggle      = new GUIStyle(HighLogic.Skin.toggle);
            theSkin.textArea    = new GUIStyle(HighLogic.Skin.textArea);
            theSkin.textField   = new GUIStyle(HighLogic.Skin.textField);
            theSkin.scrollView  = new GUIStyle(HighLogic.Skin.scrollView);

            // Now alter the parts of theSkin that we want to change:
            //
            theSkin.window = new GUIStyle(HighLogic.Skin.window);
            theSkin.box.fontSize = 11;
            theSkin.box.padding = new RectOffset(5,3,3,5);
            theSkin.box.margin = new RectOffset(1,1,1,1);
            theSkin.label.fontSize = 11;
            theSkin.label.padding = new RectOffset(2,2,2,2);
            theSkin.label.margin = new RectOffset(1,1,1,1);
            theSkin.textField.fontSize = 11;
            theSkin.textField.padding = new RectOffset(0,0,0,0);
            theSkin.textField.margin = new RectOffset(1,1,1,1);
            theSkin.textArea.fontSize = 11;
            theSkin.textArea.padding = new RectOffset(0,0,0,0);
            theSkin.textArea.margin = new RectOffset(1,1,1,1);
            theSkin.toggle.fontSize = 11;
            theSkin.toggle.padding = new RectOffset(0,0,0,0);
            theSkin.toggle.margin = new RectOffset(1,1,1,1);
            theSkin.button.fontSize =11;
            theSkin.button.padding = new RectOffset(0,0,0,0);
            theSkin.button.margin = new RectOffset(1,1,1,1);

            // And these are new styles for our own use in special cases:
            //
            headingLabelStyle = new GUIStyle(theSkin.label);
            headingLabelStyle.fontSize = 13;
            headingLabelStyle.padding = new RectOffset(2,2,2,2);
            
            tooltipLabelStyle = new GUIStyle(theSkin.label);
            tooltipLabelStyle.fontSize = 11;
            tooltipLabelStyle.padding = new RectOffset(0,2,0,2);
            tooltipLabelStyle.normal.textColor = Color.white;
                        
            return theSkin;
        }

/* -------------------------------------------
 * OLD WAY OF ADDING KOSNameTags, now abandoned
 * -------------------------------------------
 * 
        private void BeginAssignPartMode()
        {
            isAssigningPart = true;

            List<global::Part> partsOnShip;
            if (HighLogic.LoadedSceneIsEditor)
                partsOnShip = EditorLogic.SortedShipList;
            else if (HighLogic.LoadedSceneIsFlight)
                partsOnShip = FlightGlobals.ActiveVessel.Parts;
            else
                return;

            foreach (global::Part part in partsOnShip)
            {
                part.AddOnMouseEnter(MouseOverPartEnter);
                part.AddOnMouseExit(MouseOverPartLeave);
            }
            partClickMsg = ScreenMessages.PostScreenMessage("Click on a part To apply a nametag",120,ScreenMessageStyle.UPPER_CENTER);            
        }
        
        private void EndAssignPartMode()
        {
            isAssigningPart = false;

            List<global::Part> partsOnShip;
            if (HighLogic.LoadedSceneIsEditor)
                partsOnShip = EditorLogic.SortedShipList;
            else if (HighLogic.LoadedSceneIsFlight)
                partsOnShip = FlightGlobals.ActiveVessel.Parts;
            else
                return;
                
            foreach (global::Part part in partSOnShip)
            {
                part.RemoveOnMouseEnter(MouseOverPartEnter);
                part.RemoveOnMouseExit(MouseOverPartLeave);
            }
            ScreenMessages.RemoveMessage(partClickMsg);
        }
        
        public void MouseOverPartEnter(global::Part p)
        {
            currentHoverPart = p;
            
            nameTagModule = p.Modules.OfType<KOSNameTag>().FirstOrDefault();
            if (nameTagModule != null)
            {
                nameTagModule.PopupNameTagChanger();
            }
        }

        public void MouseOverPartLeave(global::Part p)
        {
            currentHoverPart = null;
            
            if (nameTagModule != null)
            {
                nameTagModule.TypingCancel();
                nameTagModule = null;
            }
        }
 *
 *  END Commented out section
 *  --------------------------
 */
 
    }
}
