using kOS.Module;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Module;
using kOS.Safe.Utilities;
using kOS.UserIO;
using kOS.Utilities;
using KSP.UI.Screens;
using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace kOS.Screen
{
    /// <summary>
    /// Window that holds the popup that the toolbar button is meant to create.
    /// window.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class KOSToolbarWindow : MonoBehaviour
    {
        private ApplicationLauncherButton launcherButton;
        private IButton blizzyButton;

        private const ApplicationLauncher.AppScenes APP_SCENES =
            ApplicationLauncher.AppScenes.FLIGHT |
            ApplicationLauncher.AppScenes.SPH |
            ApplicationLauncher.AppScenes.VAB |
            ApplicationLauncher.AppScenes.MAPVIEW;

        private static Texture2D launcherButtonTexture;
        private static Texture2D terminalClosedIconTexture;
        private static Texture2D terminalOpenIconTexture;
        private static Texture2D terminalClosedTelnetIconTexture;
        private static Texture2D terminalOpenTelnetIconTexture;

        // ReSharper disable once RedundantDefaultFieldInitializer
        private bool clickedOn = false;

        private Rect rectToFit = new Rect(0, 0, 1, 1); // will be changed in Open()

        // ReSharper disable RedundantDefaultFieldInitializer
        private int verticalSectionCount = 0;

        private int horizontalSectionCount = 0;

        // ReSharper restore RedundantDefaultFieldInitializer
        private Vector2 scrollPos = new Vector2(200, 350);

        private static Rect windowRect; // does anybody know why this is static?
        private const int UNIQUE_ID = 8675309; // Jenny, I've got your number.
        private static GUISkin panelSkin;
        private static GUIStyle headingLabelStyle;
        private static GUIStyle vesselNameStyle;
        private static GUIStyle partNameStyle;
        private static GUIStyle tooltipLabelStyle;
        private static GUIStyle smallLabelStyle;
        private static GUIStyle boxDisabledStyle;
        private static GUIStyle boxOffStyle;
        private static GUIStyle boxOnStyle;
        private static string versionString;

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

        private bool alreadyAwake;
        private bool firstTime = true;
        private bool isOpen;
        private bool wasOpenLastPaint;
        private kOS.Screen.ListPickerDialog fontPicker;
        private kOS.Screen.ListPickerDialog ipAddrPicker;

        private DateTime prevConfigTimeStamp = DateTime.MinValue;

        private List<int> backingConfigInts;

        private bool uiGloballyHidden = false;


        /// <summary>
        /// Unity hates it when a MonoBehaviour has a constructor,
        /// so all the construction work is here instead:
        /// </summary>
        public static void FirstTimeSetup()
        {
            launcherButtonTexture = Utilities.Utils.GetTextureWithErrorMsg("kOS/GFX/dds_launcher-button", false);
            terminalOpenIconTexture = Utilities.Utils.GetTextureWithErrorMsg("kOS/GFX/dds_terminal-icon-open", false);
            terminalClosedIconTexture = Utilities.Utils.GetTextureWithErrorMsg("kOS/GFX/dds_terminal-icon-closed", false);
            terminalOpenTelnetIconTexture = Utilities.Utils.GetTextureWithErrorMsg("kOS/GFX/dds_terminal-icon-open-telnet", false);
            terminalClosedTelnetIconTexture = Utilities.Utils.GetTextureWithErrorMsg("kOS/GFX/dds_terminal-icon-closed-telnet", false);

            windowRect = new Rect(0, 0, 1f, 1f); // this origin point will move when opened/closed.
            panelSkin = BuildPanelSkin();
            versionString = Utils.GetAssemblyFileVersion();
            //UnityEngine.Debug.Log("[kOSToolBarWindow] FirstTimeSetup Finished, v=" + versionString);
        }

        public void Awake()
        {
            // TODO - remove commented-out line below after varifying KSP 1.1 works without it:
            // GameEvents.onGameSceneLoadRequested.Add(OnGameSceneLoadRequestedForAppLauncher);

            GameEvents.onGUIApplicationLauncherReady.Add(AddButton);
            GameEvents.onGUIApplicationLauncherUnreadifying.Add(RemoveButton);
            GameEvents.onHideUI.Add(OnHideUI);
            GameEvents.onShowUI.Add(OnShowUI);
            GameEvents.onGameStateSave.Add(OnGameStateSave);
            GameObject.DontDestroyOnLoad(this);

            fontPicker = null;
        }

        // TODO - Remove this next method after verifying KSP 1.1 works without it:
        // private void OnGameSceneLoadRequestedForAppLauncher(GameScenes sceneToLoad)
        // {
        //     GoAway();
        // }

        public void Start()
        {
            // Prevent multiple calls of this:
            if (alreadyAwake) return;
            alreadyAwake = true;
            SafeHouse.Logger.SuperVerbose("[kOSToolBarWindow] Start succesful");
        }

        public void AddButton()
        {
            if (!ApplicationLauncher.Ready) return;

            var useBlizzyOnly = ToolbarManager.ToolbarAvailable &&
                                kOSCustomParameters.Instance != null &&
                                kOSCustomParameters.Instance.useBlizzyToolbarOnly;

            if (firstTime)
            {
                FirstTimeSetup();
                firstTime = false;
            }

            if (!useBlizzyOnly && launcherButton == null)
            {
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
            if (blizzyButton == null)
                AddBlizzyButton();

            SetupBackingConfigInts();
            SafeHouse.Logger.SuperVerbose("[kOSToolBarWindow] Launcher Icon init successful");
        }
        
        public void RemoveButton(GameScenes scene)
        {
            RemoveButton();
        }

        public void RemoveButton()
        {
            if (launcherButton != null)
                GoAway();
        }

        public void AddBlizzyButton()
        {
            if (!ToolbarManager.ToolbarAvailable) return;

            blizzyButton = ToolbarManager.Instance.add("kOS", "kOSButton");
            blizzyButton.TexturePath = "kOS/GFX/dds_launcher-button-blizzy";
            blizzyButton.ToolTip = "kOS";
            blizzyButton.OnClick += e => CallbackOnClickBlizzy();
        }

        /// <summary>
        /// In order to support the changes to solve issue #565 (see github for kOS)
        /// we have to store a temp value per integer field, that is NOT the actual
        /// official integer value of the field, but just stores the value the user
        /// is temporarily typing:
        /// </summary>
        public void SetupBackingConfigInts()
        {
            if (SafeHouse.Config.TimeStamp <= prevConfigTimeStamp)
                return;
            prevConfigTimeStamp = DateTime.Now;

            IList<ConfigKey> keys = SafeHouse.Config.GetConfigKeys();
            backingConfigInts = new List<int>();
            // Fills exactly the expected number of needed ints, in the same
            // order they will be encountered in when iterating over GetConfigKeys later
            // in the gui drawing method:
            foreach (ConfigKey key in keys)
                if (key.Value is int)
                    backingConfigInts.Add((int)(key.Value));
        }

        public void GoAway()
        {
            if (isOpen) Close();
            clickedOn = false;

            try
            {
                if (launcherButton != null && ApplicationLauncher.Instance != null)
                {
                    ApplicationLauncher launcher = ApplicationLauncher.Instance;

                    launcher.DisableMutuallyExclusive(launcherButton);
                    launcher.RemoveOnRepositionCallback(CallbackOnShow);
                    launcher.RemoveOnHideCallback(CallbackOnHide);
                    launcher.RemoveOnShowCallback(CallbackOnShow);
                    launcher.RemoveModApplication(launcherButton);
                    launcherButton = null;
                }
            }
            catch (Exception e)
            {
                SafeHouse.Logger.SuperVerbose("[kOSToolBarWindow] Failed unregistering AppLauncher handlers," + e.Message);
            }

            // force close the font picker window if it was still open:
            if (fontPicker != null)
            {
                fontPicker.Close();
                Destroy(fontPicker);
                fontPicker = null;
            }
        }

        public void OnDestroy()
        {
            // TODO : Remove the following commented line after it's been discovered that KSP 1.1 works without it:
            // GameEvents.onGameSceneLoadRequested.Remove(OnGameSceneLoadRequestedForAppLauncher);

            GameEvents.onHideUI.Remove(OnHideUI);
            GameEvents.onShowUI.Remove(OnShowUI);
            GameEvents.onGameStateSave.Remove(OnGameStateSave);

            GoAway();
            SafeHouse.Logger.SuperVerbose("[kOSToolBarWindow] OnDestroy successful");
        }

        public void CallbackOnClickBlizzy()
        {
            if (!isOpen)
                Open();
            else
                Close();
        }

        /// <summary>Callback for when the button is toggled on</summary>
        public void CallbackOnTrue()
        {
            SafeHouse.Logger.SuperVerbose("KOSToolBarWindow: PROOF: CallbackOnTrue()");
            clickedOn = true;
            Open();
        }

        /// <summary>Callback for when the button is toggled off</summary>
        public void CallbackOnFalse()
        {
            SafeHouse.Logger.SuperVerbose("KOSToolBarWindow: PROOF: CallbackOnFalse()");
            clickedOn = false;
            Close();
        }

        /// <summary>Callback for when the mouse is hovering over the button</summary>
        public void CallbackOnHover()
        {
            SafeHouse.Logger.SuperVerbose("KOSToolBarWindow: PROOF: CallbackOnHover()");
            if (!clickedOn)
                Open();
        }

        /// <summary>Callback for when the mouse is hover is off the button</summary>
        public void CallbackOnHoverOut()
        {
            SafeHouse.Logger.SuperVerbose("KOSToolBarWindow: PROOF: CallbackOnHoverOut()");
            if (!clickedOn)
                Close();
        }

        /// <summary>Callback for when the application launcher shows itself</summary>
        public void CallbackOnShow()
        {
            SafeHouse.Logger.SuperVerbose("KOSToolBarWindow: PROOF: CallbackOnShow()");
            if (clickedOn)
                Open();
        }

        /// <summary>Callback for when the application launcher hides itself</summary>
        public void CallbackOnHide()
        {
            SafeHouse.Logger.SuperVerbose("KOSToolBarWindow: PROOF: CallbackOnHide()");
            Close();
        }

        public void Open()
        {
            SafeHouse.Logger.SuperVerbose("KOSToolBarWindow: PROOF: Open()");

            float assumeStagingListWidth = 64f; // hardcoded for now.  Might try to see how to read it on the fly later.

            bool isTop = ApplicationLauncher.Instance.IsPositionedAtTop;

            if (launcherButton == null)
            {
                if (isTop)
                {
                    rectToFit = new Rect(0, 0, UnityEngine.Screen.width - assumeStagingListWidth, UnityEngine.Screen.height);
                    windowRect = new Rect(UnityEngine.Screen.width, 0, 0, 0);
                }
                else
                {
                    rectToFit = new Rect(0, 0, UnityEngine.Screen.width - assumeStagingListWidth, UnityEngine.Screen.height - assumeStagingListWidth);
                    windowRect = new Rect(UnityEngine.Screen.width, UnityEngine.Screen.height, 0, 0);
                }
                isOpen = true;
                return;
            }
            Vector3 launcherScreenCenteredPos = launcherButton.GetAnchorUL();

            // There has *got* to be a method somewhere in Unity that does this transformation
            // without having to hardcode the formula, but after wasting 5 hours searching
            // Unity docs and google and ILSpy, I give up trying to find it.  This formula is
            // probably sitting on top of fragile assumptions, but I give up on trying to find
            // the "right" way.  (The values returned by the  RectTransform appear to be using
            // screen pixel coords, but with the center of the screen being (0,0) rather than
            // one of the corners.  This does not appear to be any of the named reference
            // frames Unity docs talk about ("World", "Viewport", and "Screen")):
            //
            // If any other kOS devs want to try a hand at fighting the Unity docs to figure this
            // out, be my guest.  In the mean time, this is the hardcoded solution:
            float launcherScreenX = launcherScreenCenteredPos.x + UnityEngine.Screen.width / 2;
            float launcherScreenY = launcherScreenCenteredPos.y + UnityEngine.Screen.height / 2;

            // amount to pad on the right side depending on what's there on the screen:

            float fitWidth = (isTop ? launcherScreenX : UnityEngine.Screen.width - assumeStagingListWidth);
            float fitHeight = (isTop ? UnityEngine.Screen.height : UnityEngine.Screen.height - launcherScreenY);

            // subset of the screen we'll clamp to stay within:
            rectToFit = new Rect(0, 0f, fitWidth, fitHeight);

            // Attempt to place the window at first in a position that would extend
            // outside the rectToFit, but at least establishes it at the correct corner
            // of the screen.  Later the auto-layout elsewhere in this class will shift
            // it as needed to obey rectToFit:
            float leftEdge = UnityEngine.Screen.width;
            float topEdge = isTop ? 0f : UnityEngine.Screen.height;

            windowRect = new Rect(leftEdge, topEdge, 0, 0); // will resize and move upon first GUILayout-ing.

            isOpen = true;
        }

        public void Close()
        {
            SafeHouse.Logger.SuperVerbose("KOSToolBarWindow: PROOF: Close()");
            if (!isOpen)
                return;

            isOpen = false;
        }

        /// <summary>Callback for when the button is shown or enabled by the application launcher</summary>
        public void CallbackOnEnable()
        {
            SafeHouse.Logger.SuperVerbose("KOSToolBarWindow: PROOF: CallbackOnEnable()");
            // do nothing, but leaving the hook here as a way to document "this thing exists and might be used".
        }

        /// <summary>Callback for when the button is hidden or disabled by the application launcher</summary>
        public void CallbackOnDisable()
        {
            SafeHouse.Logger.SuperVerbose("KOSToolBarWindow: PROOF: CallbackOnDisable()");
            // do nothing, but leaving the hook here as a way to document "this thing exists and might be used".
        }

        void OnHideUI()
        {
            uiGloballyHidden = true;
        }

        void OnShowUI()
        {
            uiGloballyHidden = false;
        }

        // As long as the user can get this toolbar menu dialog to show up, that means
        // they can change a setting in it.  Those changes should be persisted even when
        // there's no KOSProcessor modules loaded in the scene.  (Thus why the config
        // save is done here as well as in KOSProcessor.)
        void OnGameStateSave(ConfigNode node) // ConfigNode ignored.
        {
            if (SafeHouse.Config != null)
            {
                SafeHouse.Config.SaveConfig();
            }
        }

        public void OnGUI()
        {
            horizontalSectionCount = 0;
            verticalSectionCount = 0;

            if (!isOpen)
            {
                wasOpenLastPaint = false;
                return;
            }

            // Sorting the list is expensive.  Only do it when the window is first re-opened, not on every single repaint:
            if (!wasOpenLastPaint)
                kOSProcessor.SortAllInstances();
            wasOpenLastPaint = true;

            if (uiGloballyHidden && kOS.Safe.Utilities.SafeHouse.Config.ObeyHideUI) return;

            GUI.skin = HighLogic.Skin;

            windowRect = GUILayout.Window(UNIQUE_ID, windowRect, DrawWindow, "kOS " + versionString);
            windowRect = RectExtensions.ClampToRectAngle(windowRect, rectToFit);
        }

        public void DrawWindow(int windowID)
        {
            BeginHoverHousekeeping();

            CountBeginVertical();
            CountBeginHorizontal();

            DrawActiveCPUsOnPanel();

            CountBeginVertical("", 155);
            GUILayout.Label("CONFIG VALUES", headingLabelStyle);
            GUILayout.Label("To access other settings, see the kOS section in KSP's difficulty settings.", smallLabelStyle);
            GUILayout.Label("Global VALUES", headingLabelStyle);
            GUILayout.Label("Changes to these settings are saved and globally affect all saved games.", smallLabelStyle);

            int whichInt = 0; // increments only when an integer field is encountered in the config keys, else stays put.

            SetupBackingConfigInts();

            foreach (ConfigKey key in SafeHouse.Config.GetConfigKeys())
            {
                bool isFontField = key.StringKey.Equals("TerminalFontName");
                bool isIPAddrField = key.StringKey.Equals("TelnetIPAddrString");
                bool isSuppressAutopilotField = key.StringKey.Equals("SuppressAutopilot");

                if (isFontField || isSuppressAutopilotField)
                {
                    CountBeginVertical();
                    GUILayout.Label("_____", panelSkin.label);
                }
                else
                    CountBeginHorizontal();

                string labelText = key.Alias;
                string toolTipText = key.Name;

                if (isFontField)
                {
                    toolTipText += " is: " + key.Value;
                    labelText = "     ^ " + labelText;
                    DrawFontField(key);
                }
                else if (isIPAddrField)
                {
                    toolTipText += " is: " + key.Value;
                    DrawIPAddrField(key);
                }
                else if (isSuppressAutopilotField)
                {
                    DrawSuppressAutopilotField(key);
                }
                else if (key.Value is bool)
                {
                    key.Value = GUILayout.Toggle((bool)key.Value, new GUIContent("", toolTipText), panelSkin.toggle);
                }
                else if (key.Value is int)
                {
                    key.Value = DrawConfigIntField((int)(key.Value), whichInt++);
                }
                else if (key.Value is float || key.Value is double) // if double, the UI will only handle it to float precisions, by the way.
                {
                    CountBeginVertical();
                    float floatValue = Convert.ToSingle(key.Value);
                    float floatMin = Convert.ToSingle(key.MinValue);
                    float floatMax = Convert.ToSingle(key.MaxValue);
                    //Mathf doesn't have a Round to hundreths place, so this is how I'm faking it:
                    GUILayout.Label(new GUIContent((Mathf.Round(floatValue*100f)/100f).ToString()), panelSkin.label);
                    floatValue = GUILayout.HorizontalSlider(floatValue, floatMin, floatMax,
                        GUILayout.MinWidth(50), GUILayout.MaxHeight(4));
                    if (key.Value is double)
                        key.Value = (double)floatValue;
                    else
                        key.Value = floatValue;
                    CountEndVertical();
                }
                else
                {
                    GUILayout.Label(key.Alias + " is a new type this dialog doesn't support.  Contact kOS devs.");
                }

                GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
                GUILayout.Label(new GUIContent(labelText, toolTipText), panelSkin.label);
                GUILayout.EndHorizontal();

                if (isFontField || isSuppressAutopilotField)
                    CountEndVertical();
                else
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
            GUI.SetNextControlName(""); // because if you don't then there is no such thing as the "non" control to move the focus to.
            // This is an invisible dummy control to "focus on" to, basically, unfocus, because Unity didn't
            // provide an unfocus method.
        }

        private void DrawFontField(ConfigKey key)
        {
            bool clicked = GUILayout.Button(key.Value.ToString(), panelSkin.button);
            if (clicked)
            {
                // Make a new picker if it's closed, or close it if it's already open.
                if (fontPicker == null)
                {
                    fontPicker = this.gameObject.AddComponent<ListPickerDialog>();
                    kOS.Screen.ListPickerDialog.ChangeAction onChange = delegate(String s)
                        {
                            // If the font is monospaced, we'll accept it, else we'll deny the attempt
                            // and not commit the change to the config fields:
                            bool ok = AssetManager.Instance.GetSystemFontByNameAndSize(s, 13, true) != null;
                            if (ok)
                                key.Value = s;
                            return ok;
                        };

                    kOS.Screen.ListPickerDialog.CloseAction onClose = delegate() { fontPicker = null; };

                    fontPicker.Summon(windowRect.x, windowRect.y + windowRect.height, 300,
                        key.Name, "(Only fonts detected as monospaced are shown.)",
                        key.Value.ToString(), AssetManager.Instance.GetSystemFontNames(), onChange, onClose
                        );
                }
                else
                {
                    fontPicker.Close();
                    Destroy(fontPicker);
                    fontPicker = null;
                }
            }
        }

        private void DrawIPAddrField(ConfigKey key)
        {
            bool clicked = GUILayout.Button(key.Value.ToString(), panelSkin.button);
            if (clicked)
            {
                // Make a new picker if it's closed, or close it if it's already open.
                if (ipAddrPicker == null)
                {
                    ipAddrPicker = this.gameObject.AddComponent<ListPickerDialog>();
                    kOS.Screen.ListPickerDialog.ChangeAction onChange = delegate(String s)
                    {
                        bool ok = TelnetMainServer.Instance.SetBindAddrFromString(s);
                        if (ok)
                            key.Value = s;
                        return ok;
                    };

                    kOS.Screen.ListPickerDialog.CloseAction onClose = delegate() { ipAddrPicker = null; };

                    ipAddrPicker.Summon(windowRect.x, windowRect.y + windowRect.height, 300,
                        "Telnet address (restart telnet to take effect)\n", null, 
                        "current: " + key.Value.ToString(), TelnetMainServer.GetAllAddresses(), onChange, onClose
                    );
                }
                else
                {
                    ipAddrPicker.Close();
                    ipAddrPicker = null;
                }
            }
        }


        private int DrawConfigIntField(int keyVal, int whichInt)
        {
            int returnValue = keyVal; // no change, by default - return what was passed.
            string fieldName = String.Format("CONFIG_intfield_{0}", whichInt);

            bool hasFocus = GUI.GetNameOfFocusedControl().Equals(fieldName);
            bool userHitReturnThisPass = hasFocus && (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter);
            int backInt = backingConfigInts[whichInt];
            string fieldValue = (backInt == 0) ? "" : backInt.ToString(); // this lets the user temporarily delete the whole value instead of having it become a zero.

            GUI.SetNextControlName(fieldName);
            fieldValue = GUILayout.TextField(fieldValue, 4, panelSkin.textField, GUILayout.MinWidth(60));

            fieldValue = fieldValue.Trim(' ');
            int newInt = -99; // Nonzero value to act as a flag to detect if the following line got triggered:
            if (fieldValue.Length == 0)
                newInt = 0;// Empty or whitespace input should be a zero, instead of letting int.TryParse() call it an error.
            if (newInt == 0 || int.TryParse(fieldValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out newInt))
            {
                backingConfigInts[whichInt] = newInt;
                // Don't commit the temp value back to the CONFIGs unless RETURN is being pressed right now:
                if (userHitReturnThisPass)
                {
                    returnValue = backingConfigInts[whichInt];
                    GUI.FocusControl(""); // unfocus this textfield - it should give the user a visual clue that the edit has been committed.
                }
                // (Upon committing the value back to config, config will range-check it and clamp it if its out of range).
            }
            // else it reverts to what it was and wipes the typing if you don't assign it to anything.

            // Lastly, check for losing the focus - when focus is lost (i.e. user clicks outside the textfield), then
            // revert the backing value to the config value, throwing away edits.
            if (!hasFocus)
                backingConfigInts[whichInt] = keyVal;

            return returnValue;
        }

        private void DrawSuppressAutopilotField(ConfigKey key)
        {
            bool prevValue = (bool)key.Value;
            key.Value = GUILayout.Toggle(
                (bool)key.Value,
                new GUIContent("Emergency Suppress", key.Name),
                panelSkin.button);

            // When the button just got pressed in this pass (went from false to true):
            if ((bool)key.Value && !prevValue)
            {
                Close();
                clickedOn = false;
            }
        }

        private string TelnetStatusMessage()
        {
            if (TelnetMainServer.Instance == null) // We can't control the order in which monobeavhiors are loaded, so TelnetMainServer might not be there yet.
                return "TelnetMainServer object not found"; // hopefully the user never sees this.  It should stop happening the the time the loading screen is over.
            bool isOn = TelnetMainServer.Instance.IsListening;
            if (!isOn)
                return "Telnet server disabled.";

            string addr = TelnetMainServer.Instance.GetRunningAddress().ToString();
            int port = TelnetMainServer.Instance.GetRunningPort();
            int numClients = TelnetMainServer.Instance.ClientCount;

            return String.Format("Telnet server listening on {0} port {1}. ({2} client{3} connected).",
                                 addr, port, (numClients == 0 ? "no" : numClients.ToString()), (numClients == 1 ? "" : "s"));
        }

        private void DrawActiveCPUsOnPanel()
        {
            scrollPos = GUILayout.BeginScrollView(scrollPos, panelSkin.scrollView, GUILayout.MinWidth(260), GUILayout.Height(windowRect.height - 60));

            CountBeginVertical();
            Vessel prevVessel = null;
            bool atLeastOne = false;

            foreach (kOSProcessor kModule in kOSProcessor.AllInstances())
            {
                atLeastOne = true;
                Part thisPart = kModule.part;
                Vessel thisVessel = (thisPart == null) ? null : thisPart.vessel;

                // For each new vessel in the list, start a new vessel section:
                if (thisVessel != null && thisVessel != prevVessel)
                {
                    GUILayout.Box(thisVessel.GetName(), vesselNameStyle);
                    prevVessel = thisVessel;
                }
                DrawPartRow(thisPart);
            }
            if (!atLeastOne)
                GUILayout.Label("No Loaded CPUs Found.\n" +
                                "-------------------------\n" +
                                "There are either no kOS CPU's\n" +
                                "in this universe, or there are\n " +
                                "but they are all \"on rails\".", panelSkin.label);

            if (HighLogic.LoadedSceneIsEditor)
            {
                DrawRereadBootButton();
            }
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
                throw new ArgumentException(@"Part does not have a kOSProcessor module", "part");
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

            GUILayout.Box(new GUIContent(powerLabelText, powerLabelTooltip), powerBoxStyle);

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
                                             ((partTag == null) ? "" : partTag.nameTag)
                                            );
            GUILayout.Box(new GUIContent(labelText, "This is the currently highlighted part on the vessel"), partNameStyle);
        }

        private void DrawRereadBootButton()
        {
            CountBeginVertical();
            GUILayout.Box(" "); // just putting a bar above the button and text.
            CountBeginHorizontal();
            bool clicked = GUILayout.Button("Reread\nBoot\nFolder", panelSkin.button);
            GUILayout.Label(
                "If you added new files to the archive\n" +
                "boot folder after entering this\n" +
                "Editor scene, they won't show up in the\n" +
                "part window unless you click here.\n", panelSkin.label);

            if (clicked)
            {
                kOSProcessor.SetBootListDirty();
            }
            CountEndHorizontal();
            GUILayout.Box(" "); // just putting a bar below the button and text.
            CountEndVertical();
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
        private void CountBeginVertical(string debugHelp = "", float minWidth = -1)
        {
            if (!String.IsNullOrEmpty(debugHelp))
                SafeHouse.Logger.SuperVerbose("BeginVertical(\"" + debugHelp + "\") Nest " + verticalSectionCount);
            if (minWidth < 0)
                GUILayout.BeginVertical();
            else
                GUILayout.BeginVertical(GUILayout.MinWidth(minWidth));
            ++verticalSectionCount;
        }

        // Tracking the count to help detect when there's a mismatch:
        // To help detect if a begin matches with an end, put the same
        // string in both of them and see if they get the same count here.
        private void CountEndVertical(string debugHelp = "")
        {
            GUILayout.EndVertical();
            --verticalSectionCount;
            if (!String.IsNullOrEmpty(debugHelp))
                SafeHouse.Logger.SuperVerbose("EndVertical(\"" + debugHelp + "\") Nest " + verticalSectionCount);
        }

        // Tracking the count to help detect when there's a mismatch:
        // To help detect if a begin matches with an end, put the same
        // string in both of them and see if they get the same count here.
        private void CountBeginHorizontal(string debugHelp = "")
        {
            if (!String.IsNullOrEmpty(debugHelp))
                SafeHouse.Logger.SuperVerbose("BeginHorizontal(\"" + debugHelp + "\"): Nest " + horizontalSectionCount);
            GUILayout.BeginHorizontal();
            ++horizontalSectionCount;
        }

        // Tracking the count to help detect when there's a mismatch:
        // To help detect if a begin matches with an end, put the same
        // string in both of them and see if they get the same count here.
        private void CountEndHorizontal(string debugHelp = "")
        {
            GUILayout.EndHorizontal();
            --horizontalSectionCount;
            if (!String.IsNullOrEmpty(debugHelp))
                SafeHouse.Logger.SuperVerbose("EndHorizontal(\"" + debugHelp + "\"): Nest " + horizontalSectionCount);
        }

        private static GUISkin BuildPanelSkin()
        {
            GUISkin theSkin = Instantiate(HighLogic.Skin); // Use Instantiate to make a copy of the Skin Object

            // Now alter the parts of theSkin that we want to change:
            //
            theSkin.window = new GUIStyle(HighLogic.Skin.window);
            theSkin.box.fontSize = 11;
            theSkin.box.padding = new RectOffset(5, 3, 3, 5);
            theSkin.box.margin = new RectOffset(1, 1, 1, 1);
            theSkin.label.fontSize = 11;
            theSkin.textField.fontSize = 11;
            theSkin.textField.padding = new RectOffset(0, 0, 0, 0);
            theSkin.textField.margin = new RectOffset(1, 1, 1, 1);
            theSkin.textArea.fontSize = 11;
            theSkin.textArea.padding = new RectOffset(0, 0, 0, 0);
            theSkin.textArea.margin = new RectOffset(1, 1, 1, 1);
            theSkin.toggle.fontSize = 10;
            theSkin.button.fontSize = 11;
            theSkin.button.active.textColor = Color.yellow;
            theSkin.button.normal.textColor = Color.white;

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
                normal = { textColor = Color.white }
            };
            tooltipLabelStyle = new GUIStyle(theSkin.label)
            {
                fontSize = 11,
                padding = new RectOffset(0, 2, 0, 2),
                normal = { textColor = Color.white },
                wordWrap = false
            };
            smallLabelStyle = new GUIStyle(theSkin.label)
            {
                fontSize = 11,
                padding = new RectOffset(0, 2, 0, 2),
                normal = { textColor = Color.white },
                wordWrap = true
            };
            partNameStyle = new GUIStyle(theSkin.box)
            {
                hover = { textColor = new Color(0.6f, 1.0f, 1.0f) }
            };
            boxOnStyle = new GUIStyle(theSkin.box)
            {
                hover = { textColor = new Color(0.6f, 1.0f, 1.0f) },
                normal = { textColor = new Color(0.4f, 1.0f, 0.4f) } // brighter green, higher saturation.
            };
            boxOffStyle = new GUIStyle(theSkin.box)
            {
                hover = { textColor = new Color(0.6f, 1.0f, 1.0f) },
                normal = { textColor = new Color(0.6f, 0.7f, 0.6f) } // dimmer green, more washed out and grey.
            };
            boxDisabledStyle = new GUIStyle(theSkin.box)
            {
                hover = { textColor = new Color(0.6f, 1.0f, 1.0f) },
                normal = { textColor = Color.white }
            };
            return theSkin;
        }
    }
}
