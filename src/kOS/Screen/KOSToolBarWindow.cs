﻿using System;
using System.Linq;
using UnityEngine;
using kOS.Utilities;
using kOS.Suffixed;
using kOS.Safe.Module;
using kOS.Module;
using kOS.UserIO;
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
    [KSPAddon(KSPAddon.Startup.EveryScene,false)]
    public class KOSToolBarWindow : MonoBehaviour
    {
        private ApplicationLauncherButton launcherButton;
        
        private const ApplicationLauncher.AppScenes APP_SCENES = 
            ApplicationLauncher.AppScenes.FLIGHT | 
            ApplicationLauncher.AppScenes.SPH | 
            ApplicationLauncher.AppScenes.VAB | 
            ApplicationLauncher.AppScenes.MAPVIEW;

        private readonly Texture2D launcherButtonTexture;
        private readonly Texture2D terminalClosedIconTexture;
        private readonly Texture2D terminalOpenIconTexture;
        private readonly Texture2D terminalClosedTelnetIconTexture;
        private readonly Texture2D terminalOpenTelnetIconTexture;
        
        // ReSharper disable once RedundantDefaultFieldInitializer
        private bool clickedOn = false;
        private float height = 1f; // will be automatically resized by GUILayout.
        private float width = 1f; // will be automatically resized by GUILayout.
        
        // ReSharper disable RedundantDefaultFieldInitializer
        private int verticalSectionCount  = 0;
        private int horizontalSectionCount = 0;
        // ReSharper restore RedundantDefaultFieldInitializer
        private Vector2 scrollPos = new Vector2(200,350);

        private Rect windowRect;
        private const int UNIQUE_ID = 8675309; // Jenny, I've got your number.
        private GUISkin panelSkin;
        private GUIStyle headingLabelStyle;
        private GUIStyle vesselNameStyle;
        private GUIStyle partNameStyle;
        private GUIStyle tooltipLabelStyle;
        private GUIStyle boxDisabledStyle;
        private GUIStyle boxOffStyle;
        private GUIStyle boxOnStyle;
        private string versionString;
        
        ///<summary>Which CPU part description in the gui panel was the mouse hovering over during the current OnGUI call?</summary>
        private Part newHoverPart;

        ///<summary>Which CPU part description was it hovering over prior to the current OnGUI call?</summary>
        private Part prevHoverPart;

        /// <summary>Use this to remember the part's previous highlight color before we messed with it.</summary>
        private Color originalPartHighlightColor = new Color(1.0f, 1.0f, 1.0f); // Should get overwritten with the real color later.
                                                                                // This first value is a safety in case we don't do that
                                                                                // properly.  We don't want to "restore" the color to null.
        /// <summary>Our highlight color for kOS panel's part highlighting.</summary>
        private readonly Color ourPartHighlightColor = new Color(1.0f, 0.5f, 1.0f); // Bright purple.
        
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
            // This really needs fixing - the name ambiguity between UnityEngine's Debug and ours forces this long fully qualified name:
            Safe.Utilities.Debug.Logger.SuperVerbose("KOSToolbarWindow: PROOF that constructor was called.");
            launcherButtonTexture = new Texture2D(0, 0, TextureFormat.DXT1, false);
            terminalClosedIconTexture = new Texture2D(0, 0, TextureFormat.DXT1, false);
            terminalOpenIconTexture = new Texture2D(0, 0, TextureFormat.DXT1, false);
            terminalClosedTelnetIconTexture = new Texture2D(0, 0, TextureFormat.DXT1, false);
            terminalOpenTelnetIconTexture = new Texture2D(0, 0, TextureFormat.DXT1, false);
        }

        /// <summary>
        /// Unity hates it when a MonoBehaviour has a constructor,
        /// so all the construction work is here instead:
        /// </summary>
        public void FirstTimeSetup()
        {
            ++countInstances;
            myInstanceNum = countInstances;
            Safe.Utilities.Debug.Logger.SuperVerbose("KOSToolBarWindow: Now making instance number "+myInstanceNum+" of KOSToolBarWindow");

            const string LAUNCHER_BUTTON_PNG = "GameData/kOS/GFX/launcher-button.png";
            const string TERMINAL_OPEN_ICON_PNG = "GameData/kOS/GFX/terminal-icon-open.png";
            const string TERMINAL_CLOSED_ICON_PNG = "GameData/kOS/GFX/terminal-icon-closed.png";
            const string TERMINAL_OPEN_TELNET_ICON_PNG = "GameData/kOS/GFX/terminal-icon-open-telnet.png";
            const string TERMINAL_CLOSED_TELNET_ICON_PNG = "GameData/kOS/GFX/terminal-icon-closed-telnet.png";

            // ReSharper disable SuggestUseVarKeywordEvident
            WWW launcherButtonImage = new WWW("file://" + KSPUtil.ApplicationRootPath.Replace("\\", "/") + LAUNCHER_BUTTON_PNG);
            WWW terminalOpenIconImage = new WWW("file://" + KSPUtil.ApplicationRootPath.Replace("\\", "/") + TERMINAL_OPEN_ICON_PNG);
            WWW terminalClosedIconImage = new WWW("file://" + KSPUtil.ApplicationRootPath.Replace("\\", "/") + TERMINAL_CLOSED_ICON_PNG);
            WWW terminalOpenTelnetIconImage = new WWW("file://" + KSPUtil.ApplicationRootPath.Replace("\\", "/") + TERMINAL_OPEN_TELNET_ICON_PNG);
            WWW terminalClosedTelnetIconImage = new WWW("file://" + KSPUtil.ApplicationRootPath.Replace("\\", "/") + TERMINAL_CLOSED_TELNET_ICON_PNG);
            // ReSharper enable SuggestUseVarKeywordEvident
            launcherButtonImage.LoadImageIntoTexture(launcherButtonTexture);
            terminalOpenIconImage.LoadImageIntoTexture(terminalOpenIconTexture);
            terminalClosedIconImage.LoadImageIntoTexture(terminalClosedIconTexture);
            terminalOpenTelnetIconImage.LoadImageIntoTexture(terminalOpenTelnetIconTexture);
            terminalClosedTelnetIconImage.LoadImageIntoTexture(terminalClosedTelnetIconTexture);

            windowRect = new Rect(0,0,width,height); // this origin point will move when opened/closed.
            panelSkin = BuildPanelSkin();
            versionString = Utils.GetAssemblyFileVersion();
            
            GameEvents.onGUIApplicationLauncherReady.Add(RunWhenReady);
            GameEvents.onGUIApplicationLauncherDestroyed.Add(GoAway);
        }

        public void Start()
        {
            Safe.Utilities.Debug.Logger.SuperVerbose("KOSToolbarWindow: PROOF that Start() was called.");
            // Prevent multiple calls of this:
            if (alreadyAwake) return;
            alreadyAwake = true;

            FirstTimeSetup();
        }
        
        public void RunWhenReady()
        {
            Safe.Utilities.Debug.Logger.SuperVerbose("KOSToolBarWindow: Instance number " + myInstanceNum + " is trying to ready the hooks");
            // KSP claims the hook ApplicationLauncherReady.Add will not run until
            // the application is ready, even though this is emphatically false.  It actually
            // fires the event a few times before the one that "sticks" and works:
            if (!ApplicationLauncher.Ready) return; 
            if (someInstanceHasHooks) return;
            thisInstanceHasHooks = true;
            someInstanceHasHooks = true;
            
            Safe.Utilities.Debug.Logger.SuperVerbose("KOSToolBarWindow: Instance number " + myInstanceNum + " will now actually make its hooks");
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
            Safe.Utilities.Debug.Logger.SuperVerbose("KOSToolBarWindow: PROOF: Instance " + myInstanceNum + " is in GoAway().");
            if (thisInstanceHasHooks)
            {
                Safe.Utilities.Debug.Logger.SuperVerbose("KOSToolBarWindow: PROOF: Instance " + myInstanceNum + " has hooks and is entering the guts of GoAway().");
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
            Safe.Utilities.Debug.Logger.SuperVerbose("KOSToolBarWindow: PROOF: CallbackOnTrue()");
            clickedOn = true;
            Open();
        }

        /// <summary>Callback for when the button is toggled off</summary>
        public void CallbackOnFalse()
        {            
            Safe.Utilities.Debug.Logger.SuperVerbose("KOSToolBarWindow: PROOF: CallbackOnFalse()");
            clickedOn = false;
            Close();
        }

        /// <summary>Callback for when the mouse is hovering over the button</summary>
        public void CallbackOnHover()
        {            
            Safe.Utilities.Debug.Logger.SuperVerbose("KOSToolBarWindow: PROOF: CallbackOnHover()");
            if (!clickedOn)
                Open();
        }

        /// <summary>Callback for when the mouse is hover is off the button</summary>
        public void CallbackOnHoverOut()
        {            
            Safe.Utilities.Debug.Logger.SuperVerbose("KOSToolBarWindow: PROOF: CallbackOnHoverOut()");
            if (!clickedOn)
                Close();
        }

        /// <summary>Callback for when the mouse is hovering over the button</summary>
        public void CallbackOnShow()
        {            
            Safe.Utilities.Debug.Logger.SuperVerbose("KOSToolBarWindow: PROOF: CallbackOnShow()");
            if (!clickedOn && !isOpen)
                Open();
        }

        /// <summary>Callback for when the mouse is hover is off the button</summary>
        public void CallbackOnHide()
        {            
            Safe.Utilities.Debug.Logger.SuperVerbose("KOSToolBarWindow: PROOF: CallbackOnHide()");
            if (!clickedOn && isOpen)
            {
                Close();
                clickedOn = false;
            }
        }
        
        public void Open()
        {
            Safe.Utilities.Debug.Logger.SuperVerbose("KOSToolBarWindow: PROOF: Open()");
            
            bool isTop = ApplicationLauncher.Instance.IsPositionedAtTop;

            // Left edge is offset from the right
            // edge of the screen by enough to hold the width of the window and maybe more offset
            // if in the editor where there's a staging list we don't want to cover up:
            float leftEdge = ( (UnityEngine.Screen.width - width) - (HighLogic.LoadedSceneIsEditor ? 64f : 0) );

            // Top edge is either just under the button itself (which contains 40 pixel icons), or just above
            // the screen bottom by enough room to hold the window height plus the 40 pixel icons):
            float topEdge = isTop ? (40f) : (UnityEngine.Screen.height - (height+40) );
            
            windowRect = new Rect(leftEdge, topEdge, 0, 0); // will resize upon first GUILayout-ing.
            Safe.Utilities.Debug.Logger.SuperVerbose("KOSToolBarWindow: PROOF: Open(), windowRect = " + windowRect);
            
            isOpen = true;
        }

        public void Close()
        {
            Safe.Utilities.Debug.Logger.SuperVerbose("KOSToolBarWindow: PROOF: Close()");
            if (! isOpen)
                return;

            isOpen = false;
        }

        /// <summary>Callback for when the button is shown or enabled by the application launcher</summary>
        public void CallbackOnEnable()
        {
            Safe.Utilities.Debug.Logger.SuperVerbose("KOSToolBarWindow: PROOF: CallbackOnEnable()");
            // do nothing, but leaving the hook here as a way to document "this thing exists and might be used".
        }
        
        /// <summary>Callback for when the button is hidden or disabled by the application launcher</summary>
        public void CallbackOnDisable()
        {            
            Safe.Utilities.Debug.Logger.SuperVerbose("KOSToolBarWindow: PROOF: CallbackOnDisable()");
            // do nothing, but leaving the hook here as a way to document "this thing exists and might be used".
        }
        

        public void OnGUI()
        {
            horizontalSectionCount = 0;
            verticalSectionCount = 0;

            if (!onGUICalledThisInstance) // I want proof it was called, but without spamming the log:
            {
                Safe.Utilities.Debug.Logger.SuperVerbose("KOSToolBarWindow: PROOF: OnGUI() was called at least once on instance number " + myInstanceNum);
                onGUICalledThisInstance = true;
            }
            
            if (!isOpen ) return;

            if (!onGUIWasOpenThisInstance) // I want proof it was called, but without spamming the log:
            {
                Safe.Utilities.Debug.Logger.SuperVerbose("KOSToolBarWindow: PROOF: OnGUI() was called while the window was supposed to be open at least once on instance number " + myInstanceNum);
                onGUIWasOpenThisInstance = true;
            }
            
            GUI.skin = HighLogic.Skin;

            windowRect = GUILayout.Window(UNIQUE_ID, windowRect, DrawWindow, "kOS " + versionString);

            width = windowRect.width;
            height = windowRect.height;

        }
        
        public void DrawWindow(int windowID)
        {
            BeginHoverHousekeeping();

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
                    key.Value = GUILayout.Toggle((bool)key.Value,"", panelSkin.toggle);
                }
                else if (key.Value is int)
                {
                    string fieldValue = key.Value.ToString();
                    fieldValue  = GUILayout.TextField(fieldValue, 6, panelSkin.textField, GUILayout.MinWidth(60));
                    int newInt;
                    if (int.TryParse(fieldValue, out newInt))
                        key.Value = newInt;
                    // else it reverts to what it was and wipes the typing if you don't assign it to anything.
                }
                else
                {
                    GUILayout.Label(key.Alias + " is a new type this dialog doesn't support.  Contact kOS devs.");
                }
                GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
                GUILayout.Label(new GUIContent(labelText,toolTipText), panelSkin.label);
                GUILayout.EndHorizontal();

                CountEndHorizontal();
            }
            CountEndVertical();
 
            CountEndHorizontal();
            
            
            // This is where tooltip hover text will show up, rather than in a hover box wherever the pointer is like normal.
            // Unity doesn't do hovering tooltips and you have to specify a zone for them to appear like this:
            string whichMessage = (GUI.tooltip.Length > 0 ? GUI.tooltip : TelnetStatusMessage()); // when tooltip isn't showing, show telnet status instead.
            GUILayout.Label(whichMessage, tooltipLabelStyle);
            CountEndVertical();

            EndHoverHousekeeping();
        }
        
        private string TelnetStatusMessage()
        {
            if (TelnetMainServer.Instance == null) // We can't control the order in which monobeavhiors are loaded, so TelnetMainServer might not be there yet. 
                return "TelnetMainServer object not found"; // hopefully the user never sees this.  It should stop happening the the time the loading screen is over.
            bool isOn = TelnetMainServer.Instance.IsListening;
            if (!isOn)
                return "Telnet server disabled.";
            
            string addr = TelnetMainServer.Instance.BindAddr.ToString();
            int numClients = TelnetMainServer.Instance.ClientCount;
            
            return String.Format("Telnet server listening on {0}. ({1} client{2} connected).",
                                 addr, (numClients == 0 ? "no" : numClients.ToString()), (numClients == 1 ? "" : "s"));
        }
        
        private void DrawActiveCPUsOnPanel()
        {
            scrollPos = GUILayout.BeginScrollView(scrollPos, panelSkin.scrollView, GUILayout.MinWidth(260), GUILayout.Height(height-60));
            
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
                                "but they are all \"on rails\".", panelSkin.label );
            CountEndVertical();

            GUILayout.EndScrollView();
        }
        
        private void DrawPartRow(Part part)
        {
            CountBeginHorizontal();
            
            DrawPart(part);
            
            kOSProcessor processorModule = part.Modules.OfType<kOSProcessor>().FirstOrDefault();

            if (processorModule == null)
            {
                throw new ArgumentException("Part does not have a kOSProcessor module", "part");
            }

            GUIStyle powerBoxStyle;
            string powerLabelText;
            string powerLabelTooltip;
            if (processorModule.ProcessorMode == ProcessorModes.STARVED)
            {
                 powerBoxStyle = boxDisabledStyle;
                 powerLabelText = "power\n<starved>";
                 powerLabelTooltip = "Highlighted CPU has no ElectricCharge.";
            }
            else if (processorModule.ProcessorMode == ProcessorModes.READY)
            {
                 powerBoxStyle = boxOnStyle;
                 powerLabelText = "power\non";
                 powerLabelTooltip = "Highlighted CPU is turned on and running.\n";
            }
            else
            {
                 powerBoxStyle = boxOffStyle;
                 powerLabelText = "power\noff";
                 powerLabelTooltip = "Highlighted CPU is turned off.";
            }

            GUILayout.Box( new GUIContent(powerLabelText, powerLabelTooltip), powerBoxStyle);

            if (GUILayout.Button((processorModule.WindowIsOpen() ? 
                                  new GUIContent((processorModule.TelnetIsAttached() ? terminalOpenTelnetIconTexture : terminalOpenIconTexture),
                                                 "Click to close terminal window.") :
                                  new GUIContent((processorModule.TelnetIsAttached() ? terminalClosedTelnetIconTexture : terminalClosedIconTexture),
                                                 "Click to open terminal window.")),
                                  panelSkin.button))
                processorModule.ToggleWindow();

            CountEndHorizontal();

            CheckHoverOnPreviousGUIElement(part);
        }
        
        private void DrawPart(Part part)
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
            GUILayout.Box(new GUIContent(labelText, "This is the currently highlighted part on the vessel"), partNameStyle);
        }
        
        public void BeginHoverHousekeeping()
        {
            // OnGUI() gets called many times in different modes for different reasons.
            // This logic only works right when used during the Repaint pass of OnGUI().
            if (Event.current.type != EventType.Repaint)
                return;
            
            // Track whether or not the mouse is over the desired GUI element on *this* OnGUI
            // by clearing out what it was before first:
            newHoverPart = null;
        }
        
        /// <summary>
        /// Control the highlighting of parts in the vessel depending on whether or not
        /// the mouse was hovering in the right spot to cause a highlight.
        /// </summary>
        public void EndHoverHousekeeping()
        {
            // OnGUI() gets called many times in different modes for different reasons.  
            // This logic only works right when used during the Repaint pass of OnGUI().
            if (Event.current.type != EventType.Repaint)
                return;
            
            // If we were already highlighting a part, and are no longer hovering over
            // that part area in the panel, then de-highlight it:
            if (prevHoverPart != null && prevHoverPart != newHoverPart)
            {
                prevHoverPart.SetHighlightColor(originalPartHighlightColor);
                prevHoverPart.SetHighlight(false, false);
            }
            
            // If we are now hovering over a part area in the panel, then start highlighting it,
            // remembering what it was before so it cab be set back again.
            if (newHoverPart != null && prevHoverPart != newHoverPart)
            {
                originalPartHighlightColor = newHoverPart.highlightColor;
                newHoverPart.SetHighlightColor(ourPartHighlightColor);
                newHoverPart.SetHighlight(true, false);
            }

            prevHoverPart = newHoverPart;
        }

        /// <summary>Whatever the most recent GUILayout element was, remember the part it was for
        /// if the mouse was hovering in it.</summary>
        /// <param name="part">The part that just had info drawn for it</param>
        public void CheckHoverOnPreviousGUIElement(Part part)
        {
            // OnGUI() gets called many times in different modes for different reasons.  
            // This logic only works right when used during the Repaint pass of OnGUI().
            if (Event.current.type != EventType.Repaint)
                return;
            
            if (GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                newHoverPart = part;
        }
        
        
        // Tracking the count to help detect when there's a mismatch:
        // To help detect if a begin matches with an end, put the same
        // string in both of them and see if they get the same count here.
        private void CountBeginVertical(string debugHelp="")
        {
            if (! String.IsNullOrEmpty(debugHelp))
                Safe.Utilities.Debug.Logger.SuperVerbose("BeginVertical(\""+debugHelp+"\") Nest "+verticalSectionCount);
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
                Safe.Utilities.Debug.Logger.SuperVerbose("EndVertical(\""+debugHelp+"\") Nest "+verticalSectionCount);
        }
        
        // Tracking the count to help detect when there's a mismatch:
        // To help detect if a begin matches with an end, put the same
        // string in both of them and see if they get the same count here.
        private void CountBeginHorizontal(string debugHelp="")
        {
            if (! String.IsNullOrEmpty(debugHelp))
                Safe.Utilities.Debug.Logger.SuperVerbose("BeginHorizontal(\""+debugHelp+"\"): Nest "+horizontalSectionCount);
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
                Safe.Utilities.Debug.Logger.SuperVerbose("EndHorizontal(\""+debugHelp+"\"): Nest "+horizontalSectionCount);
        }
        
        
        private GUISkin BuildPanelSkin()
        {
            GUISkin theSkin = Utils.GetSkinCopy(HighLogic.Skin);
            // theSkin won't actually be used directly anymore because GetSkinCopy is missing a few key
            // fields.  Instead we'll have to set all the GUIStyle's manually everywhere - ugly.
            
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
            partNameStyle = new GUIStyle(theSkin.box)
            {
                hover = {textColor = new Color(0.6f,1.0f,1.0f)}
            };
            boxOnStyle = new GUIStyle(theSkin.box)
            {
                hover = {textColor = new Color(0.6f,1.0f,1.0f)},
                normal = {textColor = new Color(0.4f,1.0f,0.4f)} // brighter green, higher saturation.
            };
            boxOffStyle = new GUIStyle(theSkin.box)
            {
                hover = {textColor = new Color(0.6f,1.0f,1.0f)},
                normal = {textColor = new Color(0.6f,0.7f,0.6f)} // dimmer green, more washed out and grey.
            };
            boxDisabledStyle = new GUIStyle(theSkin.box)
            {
                hover = {textColor = new Color(0.6f,1.0f,1.0f)},
                normal = {textColor = Color.white}
            };
            return theSkin;
        }
 
    }
}
