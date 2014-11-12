using System;
using System.Linq;
using UnityEngine;
using kOS.Utilities;
using kOS.Suffixed;
using kOS.Safe.Module;
using kOS.Module;

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

        private const ApplicationLauncher.AppScenes APP_SCENES = 
            ApplicationLauncher.AppScenes.FLIGHT | 
            ApplicationLauncher.AppScenes.SPH | 
            ApplicationLauncher.AppScenes.VAB | 
            ApplicationLauncher.AppScenes.MAPVIEW;

        private readonly Texture2D launcherButtonTexture;
        
        // ReSharper disable once RedundantDefaultFieldInitializer
        private bool clickedOn = false;
        private float height = 1f; // will be automatically resized by GUILayout.
        private float width = 1f; // will be automatically resized by GUILayout.
        
        // ReSharper disable RedundantDefaultFieldInitializer
        private int verticalSectionCount  = 0;
        private int horizontalSectionCount = 0;
        // ReSharper restore RedundantDefaultFieldInitializer
        private Vector2 scrollPos = new Vector2(200,350);

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
        private const int UNIQUE_ID = 8675309; // Jenny, I've got your number.
        private GUISkin panelSkin;
        private GUIStyle headingLabelStyle;
        private GUIStyle vesselNameStyle;
        private GUIStyle tooltipLabelStyle;
        private GUIStyle buttonDisabledStyle;
        private GUIStyle buttonOffStyle;
        private GUIStyle buttonOnStyle;
        private string versionString;

        
        // Some of these are for just debug messages, and others are
        // necessary for tracking things to make it not spawn too many
        // buttons or spawn them at the wrong times.  For now I want to
        // keep the debug logging in the code so users have something they
        // can show in bug reports until I'm more confident this is working
        // perfectly:
        
        // ReSharper disable RedundantDefaultFieldInitializer
        private bool alreadyAwake = false;
        private static int  countInstances = 0;
        private int myInstanceNum = 0;
        private bool thisInstanceHasHooks = false;
        private static bool someInstanceHasHooks = false;
        private bool isOpen = false;
        private bool onGUICalledThisInstance = false;
        private bool onGUIWasOpenThisInstance = false;
        // ReSharper enable RedundantDefaultFieldInitializer

        public KOSToolBarWindow()
        {
            launcherButtonTexture = new Texture2D(0, 0, TextureFormat.DXT1, false);
        }

        /// <summary>
        /// Unity hates it when a MonoBehaviour has a constructor,
        /// so all the construction work is here instead:
        /// </summary>
        public void FirstTimeSetup()
        {
            ++countInstances;
            myInstanceNum = countInstances;
            Debug.Log("KOSToolBarWindow: Now making instance number "+myInstanceNum+" of KOSToolBarWindow");

            const string LAUNCHER_BUTTON_PNG = "GameData/kOS/GFX/launcher-button.png";

            // ReSharper disable once SuggestUseVarKeywordEvident
            WWW imageFromURL = new WWW("file://" + KSPUtil.ApplicationRootPath.Replace("\\", "/") + LAUNCHER_BUTTON_PNG);

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
                APP_SCENES,
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
        
        /// <summary>Callback for when the button is hidden or disabled by the application launcher</summary>
        public void CallbackOnDisable()
        {            
            Debug.Log("KOSToolBarWindow: PROOF: CallbackOnDisable()");
            // do nothing, but leaving the hook here as a way to document "this thing exists and might be used".
        }
        

        public void OnGUI()
        {
            horizontalSectionCount = 0;
            verticalSectionCount = 0;
            
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

            windowRect = GUILayout.Window(UNIQUE_ID, windowRect, DrawWindow, "kOS " + versionString);

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

            CountBeginVertical();
            CountBeginHorizontal();

            DrawActiveCPUsOnPanel();

            CountBeginVertical();
            GUILayout.Label("CONFIG VALUES", headingLabelStyle);
            GUILayout.Label("Changes to these settings are saved and globally affect all saved games.", tooltipLabelStyle);

            foreach (ConfigKey key in Config.Instance.GetConfigKeys())
            {
                CountBeginHorizontal();

                string labelText = key.Alias;
                string toolTipText = key.Name;

                if (key.Value is bool)
                {
                    key.Value = GUILayout.Toggle((bool)key.Value,"");
                }
                else if (key.Value is int)
                {
                    string newStringVal = GUILayout.TextField(key.Value.ToString(), 6, GUILayout.MinWidth(60));
                    int newInt;
                    if (int.TryParse(newStringVal, out newInt))
                        key.Value = newInt;
                    // else it reverts to what it was and wipes the typing if you don't assign it to anything.
                }
                else
                {
                    GUILayout.Label(key.Alias + " is a new type this dialog doesn't support.  Contact kOS devs.");
                }
                GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
                GUILayout.Label(new GUIContent(labelText,toolTipText));
                GUILayout.EndHorizontal();

                CountEndHorizontal();
            }
            CountEndVertical();
 
            CountEndHorizontal();

            // This is where tooltip hover text will show up, rather than in a hover box wherever the pointer is like normal.
            // Unity doesn't do hovering tooltips and you have to specify a zone for them to appear like this:
            GUILayout.Label(GUI.tooltip, tooltipLabelStyle);
            CountEndVertical();
        }
        
        private void DrawActiveCPUsOnPanel()
        {
            scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.MinWidth(210), GUILayout.Height(height-60));
            
            CountBeginVertical();
            Vessel prevVessel = null;
            bool atLeastOne = false;
            
            foreach (kOSProcessor kModule in kOSProcessor.AllInstances())
            {
                atLeastOne = true;   
                Part thisPart = kModule.part;
                Vessel thisVessel = (thisPart==null) ? null : thisPart.vessel;

                // For each new vessel in the list, start a new vessel section:
                if (thisVessel != null && thisVessel != prevVessel)
                {
                    GUILayout.Box(thisVessel.GetName(),vesselNameStyle);
                    prevVessel = thisVessel;
                }
                DrawPartRow(thisPart);
            }
            if (! atLeastOne)
                GUILayout.Label("No Loaded CPUs Found.\n" +
                                "-------------------------\n" +
                                "There are either no kOS CPU's\n" +
                                "in this universe, or there are\n " +
                                "but they are all \"on rails\"." );
            CountEndVertical();

            GUILayout.EndScrollView();
        }
        
        private void DrawPartRow(Part part)
        {
            CountBeginHorizontal();
            
            GUILayout.Label("  "); // indent each part row over slightly.
            DrawPart(part);
            
            kOSProcessor processorModule = part.Modules.OfType<kOSProcessor>().FirstOrDefault();

            if (processorModule == null)
            {
                throw new ArgumentException("Part does not have a kOSProcessor module", "part");
            }

            GUIStyle windowButtonStyle = processorModule.WindowIsOpen() ? buttonOnStyle : buttonOffStyle;
            GUIStyle powerButtonStyle = 
                (processorModule.ProcessorMode == ProcessorModes.STARVED) ?
                buttonDisabledStyle : ( (processorModule.ProcessorMode == ProcessorModes.READY) ?
                                         buttonOnStyle : buttonOffStyle);
            string powerButtonText = 
                (processorModule.ProcessorMode == ProcessorModes.STARVED) ?
                "<Starved>" : "Power";

            CountBeginVertical();
            if (GUILayout.Button("Window", windowButtonStyle))
            {
                processorModule.ToggleWindow();
            }
            if (GUILayout.Button(powerButtonText, powerButtonStyle))
            {
                processorModule.TogglePower();
            }
            CountEndVertical();
            CountEndHorizontal();
        }
        
        private static void DrawPart(Part part)
        {
            // Someday we may work on making this into something that
            // actually draws out the part image like in the editor icons, however
            // there appears to be no KSP API to do this for us, and it's a bit messy
            // and there's more important other stuff to do first.
            //
            // In the meantime, this is as far as my research has taken me:
            // Step 1: get a Unity GameObject from the part prototype, like so:
            //    GameObject prototypeGO = part.partInfo.iconPrefab;

            // Also, it seems to be using some proprietary extension to Unity's GUI
            // called EZGui to render some sort of camera view of the gameobject inside the
            // button, and EZGui is even worse for online documentation than Unity itself,
            // so whatever the technique is, it's hidden behind a wall of impenetrable
            // documentation with zero examples.

            // So for the meantime let's use our own text label and leave it at that.
            
            KOSNameTag partTag = part.Modules.OfType<KOSNameTag>().FirstOrDefault();

            string labelText = String.Format("{0}\n({1})",
                                             part.partInfo.title.Split(' ')[0], // just the first word of the name, i.e "CX-4181"
                                             ((partTag==null) ? "" : partTag.nameTag)
                                            );
            GUILayout.Box(labelText);
        }
        
        // Tracking the count to help detect when there's a mismatch:
        // To help detect if a begin matches with an end, put the same
        // string in both of them and see if they get the same count here.
        private void CountBeginVertical(string debugHelp="")
        {
            if (! String.IsNullOrEmpty(debugHelp))
                Debug.Log("BeginVertical(\""+debugHelp+"\") Nest "+verticalSectionCount);
            GUILayout.BeginVertical();
            ++verticalSectionCount;
        }
        
        // Tracking the count to help detect when there's a mismatch:
        // To help detect if a begin matches with an end, put the same
        // string in both of them and see if they get the same count here.
        private void CountEndVertical(string debugHelp="")
        {
            GUILayout.EndVertical();
            --verticalSectionCount;            
            if (! String.IsNullOrEmpty(debugHelp))
                Debug.Log("EndVertical(\""+debugHelp+"\") Nest "+verticalSectionCount);
        }
        
        // Tracking the count to help detect when there's a mismatch:
        // To help detect if a begin matches with an end, put the same
        // string in both of them and see if they get the same count here.
        private void CountBeginHorizontal(string debugHelp="")
        {
            if (! String.IsNullOrEmpty(debugHelp))
                Debug.Log("BeginHorizontal(\""+debugHelp+"\"): Nest "+horizontalSectionCount);
            GUILayout.BeginHorizontal();
            ++horizontalSectionCount;
        }
        
        // Tracking the count to help detect when there's a mismatch:
        // To help detect if a begin matches with an end, put the same
        // string in both of them and see if they get the same count here.
        private void CountEndHorizontal(string debugHelp="")
        {
            GUILayout.EndHorizontal();
            --horizontalSectionCount;            
            if (! String.IsNullOrEmpty(debugHelp))
                Debug.Log("EndHorizontal(\""+debugHelp+"\"): Nest "+horizontalSectionCount);
        }
        
        
        private GUISkin BuildPanelSkin()
        {
            GUISkin theSkin = Utils.GetSkinCopy(HighLogic.Skin);
            
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
            headingLabelStyle = new GUIStyle(theSkin.label)
            {
                fontSize = 13, 
                padding = new RectOffset(2, 2, 2, 2)
            };
            vesselNameStyle = new GUIStyle(theSkin.box)
            {
                fontSize = 12, 
                normal = {textColor = Color.white}
            };
            tooltipLabelStyle = new GUIStyle(theSkin.label)
            {
                fontSize = 11,
                padding = new RectOffset(0, 2, 0, 2),
                normal = {textColor = Color.white}
            };
            buttonOnStyle = new GUIStyle(theSkin.button)
            {
                normal = {textColor = new Color(0.4f,1.0f,0.4f)} // brighter green, higher saturation.
            };
            buttonOffStyle = new GUIStyle(theSkin.button)
            {
                normal = {textColor = new Color(0.6f,0.7f,0.6f)} // dimmer green, more washed out and grey.
            };
            buttonDisabledStyle = new GUIStyle(theSkin.button)
            {
                normal = {textColor = Color.white}
            };
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
