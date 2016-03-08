using System;
using System.Linq;
using System.Collections.Generic;
using kOS.Safe.Persistence;
using UnityEngine;
using kOS.Safe.Screen;
using kOS.Module;
using kOS.UserIO;
using kOS.Safe.UserIO;

namespace kOS.Screen
{
    // Blockotronix 550 Computor Monitor
    public class TermWindow : KOSManagedWindow , ITermWindow
    {
        /// <summary>
        /// Pixel size of one square section of the font template image file holding one character:
        /// </summary>
        private const int CHAR_SOURCE_SIZE = 8;
        
        private const string CONTROL_LOCKOUT = "kOSTerminal";
        private const int FONTIMAGE_CHARS_PER_ROW = 16;
        
        private static readonly string root = KSPUtil.ApplicationRootPath.Replace("\\", "/");
        private static readonly Color color = new Color(1, 1, 1, 1); // opaque window color when focused
        private static readonly Color colorAlpha = new Color(1f, 1f, 1f, 0.8f); // slightly less opaque window color when not focused.
        private static readonly Color bgColor = new Color(0.0f, 0.0f, 0.0f, 1.0f); // black background of terminal
        private static readonly Color textColor = new Color(0.4f, 1.0f, 0.2f, 1.0f); // font color on terminal
        private static readonly Color textColorOff = new Color(0.8f, 0.8f, 0.8f, 0.7f); // font color when power starved.
        private static readonly Color textColorOffAlpha = new Color(0.8f, 0.8f, 0.8f, 0.8f); // font color when power starved and not focused.
        private Rect closeButtonRect = new Rect(0, 0, 0, 0); // will be resized later.        
        private Rect resizeButtonCoords = new Rect(0,0,0,0); // will be resized later.
        private GUIStyle tinyToggleStyle;
        private Vector2 resizeOldSize;
        private bool resizeMouseDown;
        private int formerCharPixelHeight;
        private int formerCharPixelWidth;
        
        private bool consumeEvent;
        private bool fontGotResized;
        private bool keyClickEnabled;
        
        private bool collapseFastBeepsToOneBeep = false; // This is a setting we might want to fiddle with depending on opinion.

        private KeyBinding rememberThrottleCutoffKey;
        private KeyBinding rememberThrottleFullKey;

        private bool allTexturesFound = true;
        private CameraManager cameraManager;
        private float cursorBlinkTime;
        private Texture2D fontImage = new Texture2D(0, 0, TextureFormat.DXT1, false);
        private Texture2D [] fontArray;
        private bool isLocked;
        /// <summary>How long blinks should last for, for various blinking needs</summary>
        private readonly TimeSpan blinkDuration = TimeSpan.FromMilliseconds(150);
        /// <summary>How long to pad between consecutive blinks to ensure they are visibly detectable as distinct blinks.</summary>
        private readonly TimeSpan blinkCoolDownDuration = TimeSpan.FromMilliseconds(50);
        /// <summary>At what milliseconds-from-epoch timestamp will the current blink be over.</summary>
        private DateTime blinkEndTime;
        private Color currentTextColor;

        /// <summary>Telnet repaints happen less often than Update()s.  Not every Update() has a telnet repaint happening.
        /// This tells you whether there was one this update.</summary>
        private bool telnetsGotRepainted;
        
        private Texture2D terminalImage = new Texture2D(0, 0, TextureFormat.DXT1, false);
        private Texture2D terminalFrameImage = new Texture2D(0, 0, TextureFormat.DXT1, false);
        private Texture2D terminalFrameActiveImage = new Texture2D(0, 0, TextureFormat.DXT1, false);
        private Texture2D resizeButtonImage = new Texture2D(0, 0, TextureFormat.DXT1, false);
        private Texture2D networkZigZagImage = new Texture2D(0, 0, TextureFormat.DXT1, false);
        private Texture2D brightnessButtonImage = new Texture2D(0, 0, TextureFormat.DXT1, false);
        private Texture2D fontWidthButtonImage = new Texture2D(0, 0, TextureFormat.DXT1, false);
        private Texture2D fontHeightButtonImage = new Texture2D(0, 0, TextureFormat.DXT1, false);
        private WWW beepURL;
        private AudioSource beepSource;
        private int guiTerminalBeepsPending;
        
        private SharedObjects shared;
        private KOSTextEditPopup popupEditor;

        // data stored per telnet client attached:
        private readonly List<TelnetSingletonServer> telnets; // support exists for more than one telnet client to be attached to the same terminal, thus this is a list.
        private readonly Dictionary<TelnetSingletonServer, IScreenSnapShot> prevTelnetScreens;
        
        private ExpectNextChar inputExpected = ExpectNextChar.NORMAL;
        private int pendingWidth; // width to come from a resize combo.
        
        public string TitleText {get; private set;}

        private IScreenSnapShot mostRecentScreen;
        
        private DateTime lastBufferGet = DateTime.Now;
        private DateTime lastTelnetIncrementalRepaint = DateTime.Now;
        private GUISkin customSkin;
        
        private bool uiGloballyHidden = false;
        

        public TermWindow()
        {
            IsPowered = true;
            WindowRect = new Rect(50, 60, 0, 0); // will get resized later in AttachTo().
            telnets = new List<TelnetSingletonServer>();
            prevTelnetScreens = new Dictionary<TelnetSingletonServer, IScreenSnapShot>();
        }

        public bool ShowCursor { get; set; }

        public void Awake()
        {
            LoadTexture("GameData/kOS/GFX/monitor_minimal.png", ref terminalImage);
            LoadTexture("GameData/kOS/GFX/monitor_minimal_frame.png", ref terminalFrameImage);
            LoadTexture("GameData/kOS/GFX/monitor_minimal_frame_active.png", ref terminalFrameActiveImage);
            LoadTexture("GameData/kOS/GFX/resize-button.png", ref resizeButtonImage);
            LoadTexture("GameData/kOS/GFX/network-zigzag.png", ref networkZigZagImage);
            LoadTexture("GameData/kOS/GFX/brightness-button.png", ref brightnessButtonImage);
            LoadTexture("GameData/kOS/GFX/font-width-button.png", ref fontWidthButtonImage);
            LoadTexture("GameData/kOS/GFX/font-height-button.png", ref fontHeightButtonImage);
            LoadTexture("GameData/kOS/GFX/font_sml.png", ref fontImage);
            
            LoadFontArray();
            
            LoadAudio();
            
            tinyToggleStyle = new GUIStyle(HighLogic.Skin.toggle)
            {
                fontSize = 10
            };

            var gObj = new GameObject( "texteditPopup", typeof(KOSTextEditPopup) );
            DontDestroyOnLoad(gObj);
            popupEditor = (KOSTextEditPopup)gObj.GetComponent(typeof(KOSTextEditPopup));
            popupEditor.SetUniqueId(UniqueId + 5);
            
            customSkin = BuildPanelSkin();

            GameEvents.onHideUI.Add (OnHideUI);
			GameEvents.onShowUI.Add (OnShowUI);
        }

        public void OnDestroy()
        {
            GameEvents.onHideUI.Remove(OnHideUI);
            GameEvents.onShowUI.Remove(OnShowUI);
        }
        
        private void LoadFontArray()
        {
            // Make it hold all possible ASCII values even though many will be blank pictures:
            fontArray = new Texture2D[128];
            
            for (int i = 0 ; i < 128 ; ++i)
            {
                // TextureFormat cannot be DXT1 or DXT5 if you want to ever perform a
                // SetPixel on the texture (which we do).  So we start it off as a ARGB32
                // first, long enough to perform the SetPixel call, then compress it
                // afterward into a DXT5:
                Texture2D charImage = new Texture2D(CHAR_SOURCE_SIZE, CHAR_SOURCE_SIZE, TextureFormat.ARGB32, true);

                int tx = i % FONTIMAGE_CHARS_PER_ROW;
                int ty = i / FONTIMAGE_CHARS_PER_ROW;
                
                // While Unity uses the convention of upside down textures common in
                // 3D (2D images put orgin at upper-left, 3D uses lower-left), it doesn't seem
                // to apply this rule to textures loaded from files like the fontImage.
                // Thus the difference requiring the upside-down Y coord below.
                charImage.SetPixels(fontImage.GetPixels(tx * CHAR_SOURCE_SIZE, fontImage.height - (ty+1) * CHAR_SOURCE_SIZE, CHAR_SOURCE_SIZE, CHAR_SOURCE_SIZE));
                charImage.Compress(false);
                charImage.Apply();

                fontArray[i] = charImage;
            }
        }
        
        private void LoadAudio()
        {
            beepURL = new WWW("file://"+ root + "GameData/kOS/GFX/terminal-beep.wav");
            AudioClip beepClip = beepURL.audioClip;            
            beepSource = gameObject.AddComponent<AudioSource>();
            beepSource.clip = beepClip;
        }

        public void LoadTexture(String relativePath, ref Texture2D targetTexture)
        {
            var imageLoader = new WWW("file://" + root + relativePath);
            imageLoader.LoadImageIntoTexture(targetTexture);

            if (imageLoader.isDone && imageLoader.size == 0) allTexturesFound = false;
        }
        
        public void OpenPopupEditor( Volume v, string fName )
        {
            popupEditor.AttachTo(this, v, fName );
            popupEditor.Open();
        }
        
        void OnHideUI()
        {
            uiGloballyHidden = true;
        }
        
        void OnShowUI()
        {
            uiGloballyHidden = false;            
        }
        
        public override void GetFocus()
        {
            Lock();
        }
        
        public override void LoseFocus()
        {
            Unlock();
        }

        public override void Open()
        {
            base.Open();
            BringToFront();
            guiTerminalBeepsPending = 0; // Closing and opening the window will wipe pending beeps from the beep queue.
        }

        public override void Close()
        {
            // Diable GUI and release all locks
            base.Close();
        }

        public void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        private void Lock()
        {
            if (isLocked) return;

            isLocked = true;
            ShowCursor = true;
            BringToFront();

            cameraManager = CameraManager.Instance;
            cameraManager.enabled = false;

            InputLockManager.SetControlLock(CONTROL_LOCKOUT);

            // Prevent editor keys from being pressed while typing
            EditorLogic editor = EditorLogic.fetch;
                //TODO: POST 0.90 REVIEW
            if (editor != null && InputLockManager.IsUnlocked(ControlTypes.All)) editor.Lock(true, true, true, CONTROL_LOCKOUT);

            // This seems to be the only way to force KSP to let me lock out the "X" throttle
            // key.  It seems to entirely bypass the logic of every other keypress in the game,
            // so the only way to fix it is to use the keybindings system from the Setup screen.
            // When the terminal is focused, the THROTTLE_CUTOFF action gets unbound, and then
            // when its unfocused later, its put back the way it was:
            rememberThrottleCutoffKey = GameSettings.THROTTLE_CUTOFF;
            GameSettings.THROTTLE_CUTOFF = new KeyBinding(KeyCode.None);
            rememberThrottleFullKey = GameSettings.THROTTLE_FULL;
            GameSettings.THROTTLE_FULL = new KeyBinding(KeyCode.None);
        }

        private void Unlock()
        {
            if (!isLocked) return;
            
            isLocked = false;

            InputLockManager.RemoveControlLock(CONTROL_LOCKOUT);

            cameraManager.enabled = true;


            EditorLogic editor = EditorLogic.fetch;
            if (editor != null) editor.Unlock(CONTROL_LOCKOUT);

            // This seems to be the only way to force KSP to let me lock out the "X" throttle
            // key.  It seems to entirely bypass the logic of every other keypress in the game:
            if (rememberThrottleCutoffKey != null)
                GameSettings.THROTTLE_CUTOFF = rememberThrottleCutoffKey;
            if (rememberThrottleFullKey != null)
                GameSettings.THROTTLE_FULL = rememberThrottleFullKey;

        }

        void OnGUI()
        {
            if (!IsOpen) return;

            if (isLocked) ProcessKeyEvents();
            
            try
            {
                if (FlightResultsDialog.isDisplaying) return;
                if (uiGloballyHidden && kOS.Safe.Utilities.SafeHouse.Config.ObeyHideUI) return;
            }
            catch(NullReferenceException)
            {
            }
            
            GUI.skin = HighLogic.Skin;
            
            GUI.color = isLocked ? color : colorAlpha;

            // Should probably make "gui screen name for my CPU part" into some sort of utility method:
            ChangeTitle(CalcualteTitle());

            WindowRect = GUI.Window(UniqueId, WindowRect, TerminalGui, TitleText);
            
            if (consumeEvent)
            {
                consumeEvent = false;
                Event.current.Use();
            }

        }
        
        void Update()
        {
            if (shared == null || shared.Vessel == null || shared.Vessel.parts.Count == 0)
            {
                // Holding onto a vessel instance that no longer exists?
                Close();
            }
            GetNewestBuffer();
            TelnetOutputUpdate();
            ProcessTelnetInput(); // want to do this even when the terminal isn't actually displaying.
            if (telnetsGotRepainted)
            {
                // Move the beeps from the screenbuffer "queue" to my own local terminal "queue".
                // This is only done in the Update() when telentRepaint got triggered
                // because otherwise the GUI terminal could wipe shared.Screen.BeepsPending to zero during an update
                // where the telnet terminal never saw the beeps pending and never knew to print the beeps out.
                guiTerminalBeepsPending += shared.Screen.BeepsPending;
                shared.Screen.BeepsPending = 0; // Presume all the beeping has been dealt with for both the telnets and the GUI terminal.
            }

            if (!IsOpen ) return;
         
            UpdateLogic();
            UpdateGUIBeeps();

            if (!isLocked) return;

            cursorBlinkTime += Time.deltaTime;
            if (cursorBlinkTime > 1) cursorBlinkTime -= 1;
        }
        
        void UpdateGUIBeeps()
        {
            // Eat just one beep off the pending queue.  Wait for a future
            // Update() to consume more, if there are any more:
            if (guiTerminalBeepsPending > 0)
            {
                // Behavior depending on whether we'd like BEEP, BEEP, BEEP to
                // emit 3 back to back beeps, or just one beep ingoring overlapping beeps.
                // Many hardware terminals only emit one beep when given a string
                // of lots of beeps.
                if (collapseFastBeepsToOneBeep)
                {
                    Beep();
                    guiTerminalBeepsPending = 0;
                }
                else
                {
                    if (Beep())
                        --guiTerminalBeepsPending;
                }
            }
        }
        
        /// <summary>
        /// Attempts to make Unity start a playthrough of the beep audio clip.  The playthrough will continue
        /// on its own in the background while the rest of the code continues on.  It will do nothing if the previous
        /// beep is still being played.  (We only gave ourselves one audio beep source per GUI terminal so we can't play beeps
        /// simultaneously.)<br/>
        /// Addendum: It will redirect into a visual beep if that is called for instead.
        /// </summary>
        /// <returns>true if it beeped.  false if it coudn't beep yet (because the audio source is still busy emitting the previous beep).</returns>
        bool Beep()
        {
            if (shared.Screen.VisualBeep)
            {
                DateTime nowTime = DateTime.Now;
                if (nowTime < (blinkEndTime + (blinkCoolDownDuration))) // prev blink not done yet.
                    return false;
                
                // Turning this timer on tells GUI repainter elsewhere in this class to paint in reverse until it expires:
                blinkEndTime = nowTime + blinkDuration;
            }
            else
            {
                if (!beepSource.clip.isReadyToPlay || beepSource.isPlaying)
                    return false; // prev beep sound still is happening.
                
                // This is nonblocking.  Begins playing sound in background.  Code will not wait for it to finish:
                beepSource.Play();
            }
            return true;
        }

        void TelnetOutputUpdate()
        {
            DateTime newTime = DateTime.Now;
            telnetsGotRepainted = false;
            
            // Throttle it back so the faster Update() rates don't cause pointlessly repeated work:
            // Needs to be no faster than the fastest theoretical typist or script might change the view.
            if (newTime > lastTelnetIncrementalRepaint + TimeSpan.FromMilliseconds(50)) // = 1/20th second.
            {
                lastTelnetIncrementalRepaint = newTime;
                foreach (TelnetSingletonServer telnet in telnets)
                {
                    RepaintTelnet(telnet, false); // try the incremental differ update.
                }
                telnetsGotRepainted = true;
            }
        }

        /// <summary>
        /// Process the GUI event handler key events that are being seen the by GUI terminal,
        /// by translating them into values from the UnicodeCommand enum first if need be,
        /// and then passing them through to ProcessOneInputChar().
        /// </summary>
        void ProcessKeyEvents()
        {
            Event e = Event.current;
            if (e.type == EventType.KeyDown)
            {
                // Unity handles some keys in a particular way
                // e.g. Keypad7 is mapped to 0xffb7 instead of 0x37
                var c = (char)(e.character & 0x007f);

                // command sequences
                if (e.keyCode == KeyCode.C && e.control) // Ctrl+C
                {
                    ProcessOneInputChar((char)UnicodeCommand.BREAK, null);
                    consumeEvent = true;
                    return;
                }
                // Command used to be Control-shift-X, now we don't care if shift is down aymore, to match the telnet expereince
                // where there is no such thing as "uppercasing" a control char.
                if ((e.keyCode == KeyCode.X && e.control) ||
                    (e.keyCode == KeyCode.D && e.control) // control-D to match the telnet experience
                   )
                {
                    ProcessOneInputChar((char)0x000d, null);
                    consumeEvent = true;
                    return;
                }
                
                if (e.keyCode == KeyCode.A && e.control)
                {
                    ProcessOneInputChar((char)0x0001, null);
                    consumeEvent = true;
                    return;
                }
                
                if (e.keyCode == KeyCode.E && e.control)
                {
                    ProcessOneInputChar((char)0x0005, null);
                    consumeEvent = true;
                    return;
                }
                
                if (0x20 <= c && c < 0x7f) // printable characters
                {
                    ProcessOneInputChar(c, null);
                    consumeEvent = true;
                    cursorBlinkTime = 0.0f; // Don't blink while the user is still actively typing.
                }
                else if (e.keyCode != KeyCode.None)
                {
                    consumeEvent = true;
                    switch (e.keyCode)
                    {
                        case KeyCode.Tab:          ProcessOneInputChar('\t', null);                                  break;
                        case KeyCode.LeftArrow:    ProcessOneInputChar((char)UnicodeCommand.LEFTCURSORONE, null);    break;
                        case KeyCode.RightArrow:   ProcessOneInputChar((char)UnicodeCommand.RIGHTCURSORONE, null);   break;
                        case KeyCode.UpArrow:      ProcessOneInputChar((char)UnicodeCommand.UPCURSORONE, null);      break;
                        case KeyCode.DownArrow:    ProcessOneInputChar((char)UnicodeCommand.DOWNCURSORONE, null);    break;
                        case KeyCode.Home:         ProcessOneInputChar((char)UnicodeCommand.HOMECURSOR, null);       break;
                        case KeyCode.End:          ProcessOneInputChar((char)UnicodeCommand.ENDCURSOR, null);        break;
                        case KeyCode.PageUp:       ProcessOneInputChar((char)UnicodeCommand.PAGEUPCURSOR, null);     break;
                        case KeyCode.PageDown:     ProcessOneInputChar((char)UnicodeCommand.PAGEDOWNCURSOR, null);   break;
                        case KeyCode.Delete:       ProcessOneInputChar((char)UnicodeCommand.DELETERIGHT, null);      break;
                        case KeyCode.Backspace:    ProcessOneInputChar((char)UnicodeCommand.DELETELEFT, null);       break;
                        case KeyCode.KeypadEnter:  // (deliberate fall through to next case)
                        case KeyCode.Return:       ProcessOneInputChar((char)UnicodeCommand.STARTNEXTLINE, null);    break;
                        
                        // More can be added to the list here to support things like F1, F2, etc.  But at the moment we don't use them yet.
                        
                        // default: ignore and allow the event to pass through to whatever else wants to read it:
                        default:                   consumeEvent = false;                                             break;
                    }
                    cursorBlinkTime = 0.0f;// Don't blink while the user is still actively typing.
                }
            }
        }
        
        /// <summary>
        /// Read all pending input from all telnet clients attached and process it all.
        /// Hopefully this won't bog down anything, as we don't expect to get lots
        /// of chars at once from keyboard input in a single update.  The amount of
        /// characters pending in the queues should be very small since this is flushing it out
        /// every update.  It could potentially be large if someone does a big cut-n-paste
        /// into their terminal window and their telnet client therefore sends a wall of
        /// text within the span of one Update().  Premature optimization is bad, so we'll
        /// wait to see if that is a problem later.
        /// </summary>
        private void ProcessTelnetInput()
        {
            foreach (var telnet in telnets)
            {
                while (telnet.InputWaiting())
                {
                    ProcessOneInputChar(telnet.ReadChar(), telnet);
                }
            }
        }

        /// <summary>
        /// Respond to one single input character in Unicode, using the pretend
        /// virtual Unicode terminal keycodes described in the UnicodeCommand enum.
        /// To keep things simple, all key input is coerced into single Unicode chars
        /// even if the actual keypress takes multiple characters to express in its
        /// native form (i.e. ESC [ A means left-arrow on a VT100 terminal.  If
        /// a telnet is talking via VT100 codes, that ESC [ A will get converted into
        /// a single UnicdeCommand.LEFTCURSORONE character before being sent here.)
        /// <br/>
        /// This method is public because it is also how other mods should send input
        /// to the terminal if they want some other source to send simulated keystrokes.
        /// </summary>
        /// <param name="ch">The character, which might be a UnicodeCommand char</param>
        /// <param name="whichTelnet">If this came from a telnet session, which one did it come from?
        /// Set to null in order to say it wasn't from a telnet but was from the interactive GUI</param>
        public void ProcessOneInputChar(char ch, TelnetSingletonServer whichTelnet)
        {
            // Weird exceptions for multi-char data combos that would have been begun on previous calls to this method:
            switch (inputExpected)
            {
                case ExpectNextChar.RESIZEWIDTH:
                    pendingWidth = ch;
                    inputExpected = ExpectNextChar.RESIZEHEIGHT;
                    return;
                case ExpectNextChar.RESIZEHEIGHT:
                    int height = ch;
                    shared.Screen.SetSize(height, pendingWidth);
                    inputExpected = ExpectNextChar.NORMAL;
                    return;
                default:
                    break;
            }

            // Printable ASCII section of Unicode - the common vanilla situation
            // (Idea: Since this is all Unicode anyway, should we allow a wider range to
            // include multi-language accent characters and so on?  Answer: to do so we'd
            // first need to expand the font pictures in the font image file, so it's a
            // bigger task than it may first seem.)
            if (0x0020 <= ch && ch <= 0x007f)
            {
                 Type(ch);
            }
            else
            {
                switch(ch)
                {
                    // A few conversions from UnicodeCommand into those parts of ASCII that it 
                    // maps directly into nicely, otherwise just pass it through to SpecialKey():

                    case (char)UnicodeCommand.DELETELEFT:
                        Type((char)8);
                        break;
                    case (char)UnicodeCommand.STARTNEXTLINE:
                        Type('\r');
                        break;
                    case '\t':
                        Type('\t');
                        break;
                    case (char)UnicodeCommand.RESIZESCREEN:
                        inputExpected = ExpectNextChar.RESIZEWIDTH;
                        break; // next expected char is the width.
                        
                    // Finish session:  If GUI, then close window.  If Telnet, then detatch from the session:
                    
                    case (char)0x0004/*control-D*/: // How users of unix shells are used to doing this.
                    case (char)0x0018/*control-X*/: // How kOS did it in the past in the GUI window.
                        if (shared.Interpreter.IsAtStartOfCommand())
                        {
                            if (whichTelnet == null)
                                Close();
                            else
                                whichTelnet.DisconnectFromProcessor();
                        }
                        break;
                        
                    // User asking for redraw (Unity already requires that we continually redraw the GUI Terminal, so this is only meaningful for telnet):
                    
                    case (char)UnicodeCommand.REQUESTREPAINT:
                        if (whichTelnet != null)
                        {
                            ResizeAndRepaintTelnet(whichTelnet, shared.Screen.ColumnCount, shared.Screen.RowCount, true);
                        }
                        break;
                        
                    // Typical case is to just let SpecialKey do the work:
                    
                    default:
                        SpecialKey(ch);
                        break;
                }
            }

            // else ignore it - unimplemented char.
        }

        void Type(char ch)
        {
            if (shared != null && shared.Interpreter != null)
            {
                shared.Interpreter.Type(ch);
                if (IsOpen && keyClickEnabled)
                    shared.SoundMaker.BeginSound("click");
            }
        }

        void SpecialKey(char key)
        {
            if (shared != null && shared.Interpreter != null)
            {
                bool wasUsed = shared.Interpreter.SpecialKey(key);
                if (IsOpen && keyClickEnabled && wasUsed)
                    shared.SoundMaker.BeginSound("click");
            }
        }
        
        /// <summary>
        /// Get the newest copy of the screen buffer once, for use in all calculations for a while.
        /// This was added when telnet clients were added.  The execution cost of obtaining a buffer snapshot
        /// from the ScreenBuffer class is non-trivial, and therefore shouldn't be done over and over
        /// for each telnet client that needs it within the span of a short time.  This gets it
        /// once for all of them to borrow.  All calls will re-use this copy for a while,
        /// until the next terminal refresh (1/20th of a second, at the moment).
        /// </summary>
        void GetNewestBuffer()
        {
            DateTime newTime = DateTime.Now;
            
            // Throttle it back so the faster Update() rates don't cause pointlessly repeated work:
            // Needs to be no faster than the fastest theoretical typist or script might change the view.
            if (newTime > lastBufferGet + TimeSpan.FromMilliseconds(50)) // = 1/20th second.
            {
                mostRecentScreen = new ScreenSnapShot(shared.Screen);
                lastBufferGet = newTime;
            }
        }

        void TerminalGui(int windowId)
        {

            if (!allTexturesFound)
            {
                GUI.Label(new Rect(15, 15, 450, 300), "Error: Some or all kOS textures were not found. Please " +
                           "go to the following folder: \n\n<Your KSP Folder>\\GameData\\kOS\\GFX\\ \n\nand ensure that the png texture files are there.");

                GUI.Label(closeButtonRect, "Close");
                return;
            }

            if (shared == null || shared.Screen == null)
            {
                return;
            }
            IScreenBuffer screen = shared.Screen;
            
            GUI.color = isLocked ? color : colorAlpha;
            GUI.DrawTexture(new Rect(15, 20, WindowRect.width-30, WindowRect.height-55), terminalImage);

            if (telnets.Count > 0)
                DrawTelnetStatus();

            closeButtonRect = new Rect(WindowRect.width-75, WindowRect.height-30, 50, 25);
            Rect reverseButtonRect = new Rect(WindowRect.width-180, WindowRect.height-42, 100, 18);
            Rect visualBeepButtonRect = new Rect(WindowRect.width-180, WindowRect.height-22, 100, 18);
            Rect keyClickButtonRect = new Rect(10, WindowRect.height - 22, 85, 18);
            Rect rasterBarsButtonRect = new Rect(10, WindowRect.height - 42, 85, 18);
            Rect brightnessRect = new Rect(3, WindowRect.height - 100, 8, 50);
            Rect brightnessButtonRect = new Rect(1, WindowRect.height - 48, brightnessButtonImage.width, brightnessButtonImage.height);
            Rect fontWidthButtonRect = new Rect(15, WindowRect.height-32, fontWidthButtonImage.width, fontWidthButtonImage.height);
            Rect fontWidthLabelRect = new Rect(35, WindowRect.height-28, 20, 10);
            Rect fontWidthLessButtonRect = new Rect(65, WindowRect.height-28, 10, 10);
            Rect fontWidthMoreButtonRect = new Rect(90, WindowRect.height-28, 10, 10);
            Rect fontHeightButtonRect = new Rect(140, WindowRect.height-32, fontHeightButtonImage.width, fontHeightButtonImage.height);
            Rect fontHeightLabelRect = new Rect(160, WindowRect.height-28, 20, 10);
            Rect fontHeightLessButtonRect = new Rect(185, WindowRect.height-28, 10, 10);
            Rect fontHeightMoreButtonRect = new Rect(210, WindowRect.height-28, 10, 10);

            resizeButtonCoords = new Rect(WindowRect.width-resizeButtonImage.width,
                                          WindowRect.height-resizeButtonImage.height,
                                          resizeButtonImage.width,
                                          resizeButtonImage.height);
            if (GUI.RepeatButton(resizeButtonCoords, resizeButtonImage ))
            {
                if (! resizeMouseDown)
                {
                    // Remember the fact that this mouseDown started on the resize button:
                    resizeMouseDown = true;
                    resizeOldSize = new Vector2(WindowRect.width, WindowRect.height);
                }
            }

            if (GUI.Button(closeButtonRect, "Close"))
            {
                Close();
                Event.current.Use();
            }
            
            screen.ReverseScreen = GUI.Toggle(reverseButtonRect, screen.ReverseScreen, "Reverse Screen", tinyToggleStyle);
            screen.VisualBeep = GUI.Toggle(visualBeepButtonRect, screen.VisualBeep, "Visual Beep", tinyToggleStyle);
            keyClickEnabled = GUI.Toggle(keyClickButtonRect, keyClickEnabled, "Keyclicker", tinyToggleStyle);
            screen.Brightness = GUI.VerticalSlider(brightnessRect, screen.Brightness, 1f, 0f);
            GUI.DrawTexture(brightnessButtonRect, brightnessButtonImage);

            int charWidth = screen.CharacterPixelWidth;
            int charHeight = screen.CharacterPixelHeight;
            
            GUI.DrawTexture(fontWidthButtonRect, fontWidthButtonImage);
            GUI.Label(fontWidthLabelRect,charWidth+"px", customSkin.label);
            if (GUI.Button(fontWidthLessButtonRect, "-", customSkin.button))
                charWidth = Math.Max(4, charWidth - 2);
            if (GUI.Button(fontWidthMoreButtonRect, "+", customSkin.button))
                charWidth = Math.Min(24, charWidth + 2);

            GUI.DrawTexture(fontHeightButtonRect, fontHeightButtonImage);
            GUI.Label(fontHeightLabelRect,charHeight+"px", customSkin.label);
            if (GUI.Button(fontHeightLessButtonRect, "-", customSkin.button))
                charHeight = Math.Max(4, charHeight - 2);
            if (GUI.Button(fontHeightMoreButtonRect, "+", customSkin.button))
                charHeight = Math.Min(24, charHeight + 2);

            screen.CharacterPixelWidth = charWidth;
            screen.CharacterPixelHeight = charHeight;

            fontGotResized = false;
            if (formerCharPixelWidth != screen.CharacterPixelWidth || formerCharPixelHeight != screen.CharacterPixelHeight)
            {
                formerCharPixelWidth = screen.CharacterPixelWidth;
                formerCharPixelHeight = screen.CharacterPixelHeight;
                screen.SetSize(HowManyRowsFit(), HowManyColumnsFit());
                fontGotResized = true;
            }

            currentTextColor = IsPowered ? textColor : textColorOff;
                        
            // Paint the background color.
            DateTime nowTime = DateTime.Now;
            bool reversingScreen = (nowTime > blinkEndTime) ? screen.ReverseScreen : (!screen.ReverseScreen);
            if (reversingScreen)
            {   // In reverse screen mode, draw a big rectangle in foreground color across the whole active screen area:
                GUI.color = AdjustColor(textColor, screen.Brightness);
                GUI.DrawTexture(new Rect(15, 20, WindowRect.width-30, WindowRect.height-55), Texture2D.whiteTexture, ScaleMode.ScaleAndCrop );
            }
            GUI.BeginGroup(new Rect(28, 38, screen.ColumnCount * charWidth, screen.RowCount * charHeight));

            List<IScreenBufferLine> buffer = mostRecentScreen.Buffer; // just to keep the name shorter below:

            // Sometimes the buffer is shorter than the terminal height if the resize JUST happened in the last Update():
            int rowsToPaint = Math.Min(screen.RowCount, buffer.Count);

            for (int row = 0; row < rowsToPaint; row++)
            {
                IScreenBufferLine lineBuffer = buffer[row];
                for (int column = 0; column < lineBuffer.Length; column++)
                {
                    char c = lineBuffer[column];
                    if (c != 0 && c != 9 && c != 32)
                        ShowCharacterByAscii(c, column, row, currentTextColor, reversingScreen,
                                             charWidth, charHeight, screen.Brightness);
                }
            }

            bool blinkOn = cursorBlinkTime < 0.5f &&
                           screen.CursorRowShow < screen.RowCount &&
                           IsPowered &&
                           ShowCursor;
            
            if (blinkOn)
            {
                ShowCharacterByAscii((char)1, screen.CursorColumnShow, screen.CursorRowShow, currentTextColor, reversingScreen,
                                     charWidth, charHeight, screen.Brightness);
            }
            
            GUI.EndGroup();
            
            GUI.color = color; // screen size label was never supposed to be in green like the terminal is:            

            // Draw the rounded corner frame atop the chars field, so it covers the sqaure corners of the character zone
            // if they bleed over a bit.  Also, change which variant is used depending on focus:
            if (isLocked)
                GUI.DrawTexture(new Rect(15, 20, WindowRect.width-30, WindowRect.height-55), terminalFrameActiveImage);            
            else
                GUI.DrawTexture(new Rect(15, 20, WindowRect.width-30, WindowRect.height-55), terminalFrameImage);            

            GUI.Label(new Rect(WindowRect.width/2-40, WindowRect.height-12,100,10), screen.ColumnCount+"x"+screen.RowCount, customSkin.label);

            if (!fontGotResized)
                CheckResizeDrag(); // Has to occur before DragWindow or else DragWindow will consume the event and prevent drags from being seen by the resize icon.
            GUI.DragWindow();
        }
        
        protected Color AdjustColor(Color baseColor, float brightness)
        {
            Color newColor = baseColor;
            newColor.a = brightness; // represent dimness by making it fade into the backround.
            return newColor;
        }

        /// <summary>
        ///  Draw a little status line at the bottom as a reminder that you are sharing the terminal with a telnet session:
        /// </summary>
        protected void DrawTelnetStatus()
        {
            int num = telnets.Count;
            string message = String.Format( "{0} telnet client{1} attached", num, (num == 1 ? "" : "s"));
            GUI.DrawTexture(new Rect(10, WindowRect.height - 25, 25, 25), networkZigZagImage);
            GUI.Label(new Rect(40, WindowRect.height - 25, 160, 20), message);
        }
        
        protected void CheckResizeDrag()
        {
            if (Input.GetMouseButton(0) && !fontGotResized ) // mouse button is maybe dragging the frame
            {
                if (resizeMouseDown) // and it's in the midst of a drag.
                {
                    Vector2 dragDelta = MousePosAbsolute - MouseButtonDownPosAbsolute;
                    WindowRect = new Rect(WindowRect.xMin,
                                          WindowRect.yMin,
                                          Math.Max(resizeOldSize.x + dragDelta.x, 200),
                                          Math.Max(resizeOldSize.y + dragDelta.y, 200));
                }
            }
            else // mouse button is up
            {
                if (resizeMouseDown) // and it had been dragging a resize before.
                {
                    resizeMouseDown = false;
                    // Resize by integer character cells, not by actual x/y pixels:
                    // Note I wanted to call this dynamically as it's resizing, but there's some weird issue
                    // with the timing of it, where the windowRect is temporarily set to a bogus value during the
                    // time it was trying to run this, and thus it created exception-throwing code unless it waits
                    // until the drag is done before trying to calculate this.
                    // The effect the user sees is that the text can float in space past the edge of the window (when
                    // shrinking the size) until the resize drag is done, and then it redraws itself.
                    shared.Screen.SetSize(HowManyRowsFit(), HowManyColumnsFit());
                }
            }
        }
        
        void ShowCharacterByAscii(char ch, int x, int y, Color charTextColor, bool reversingScreen, int charWidth, int charHeight, float brightness)
        {
            GUI.BeginGroup(new Rect((x * charWidth), (y * charHeight), charWidth, charHeight));
            GUI.color = AdjustColor(reversingScreen ? bgColor : textColor, brightness);            
            GUI.DrawTexture(new Rect(0, 0, charWidth, charHeight), fontArray[ch], ScaleMode.StretchToFill, true);
            GUI.EndGroup();
        }

        public Rect GetRect()
        {
            return WindowRect;
        }
        
        public void Print( string str )
        {
            shared.Screen.Print( str );
        }

        internal void AttachTo(SharedObjects sharedObj)
        {
            shared = sharedObj;
            shared.Window = this;
            
            shared.Screen.CharacterPixelWidth = 8;
            shared.Screen.CharacterPixelHeight = 8;
            shared.Screen.Brightness = 0.5f;
            formerCharPixelWidth = shared.Screen.CharacterPixelWidth;
            formerCharPixelHeight = shared.Screen.CharacterPixelHeight;

            NotifyOfScreenResize(shared.Screen);
            shared.Screen.AddResizeNotifier(NotifyOfScreenResize);
            ChangeTitle(CalcualteTitle());
        }
        
        internal string CalcualteTitle()
        {
           KOSNameTag partTag = shared.KSPPart.Modules.OfType<KOSNameTag>().FirstOrDefault();
           return String.Format("{0} CPU: {1} ({2})",
                                shared.Vessel.vesselName,
                                shared.KSPPart.partInfo.title.Split(' ')[0], // just the first word of the name, i.e "CX-4181"
                                ((partTag==null) ? "" : partTag.nameTag)
                                );
        }
        
        internal void AttachTelnet(TelnetSingletonServer server)
        {
            telnets.AddUnique(server);
            prevTelnetScreens[server] = null;
        }

        internal void DetachTelnet(TelnetSingletonServer server)
        {
            server.DisconnectFromProcessor();
            telnets.Remove(server);
        }
        
        public void DetachAllTelnets()
        {
            // Have to use a (shallow) copy of the list, because the act of detaching telnets
            // will delete from the list while we're trying to iterate through it:
            TelnetSingletonServer[] listSnapshot = telnets.ToArray();
            
            foreach (TelnetSingletonServer telnet in listSnapshot)
            {
                DetachTelnet(telnet);
            }
        }

        internal int NotifyOfScreenResize(IScreenBuffer sb)
        {
            WindowRect = new Rect(WindowRect.xMin, WindowRect.yMin, sb.ColumnCount*sb.CharacterPixelWidth + 65, sb.RowCount*sb.CharacterPixelHeight + 100);

            foreach (TelnetSingletonServer telnet in telnets)
            {
                ResizeAndRepaintTelnet(telnet, sb.ColumnCount, sb.RowCount, false);
            }
            return 0;
        }
        
        /// <summary>
        /// Tell the telnet session to resize itself
        /// </summary>
        /// <param name="telnet">which telnet session to send to</param>
        /// <param name="width">new width</param>
        /// <param name="height">new height</param>
        /// <param name="unconditional">if true, then send the resize message no matter what.  If false,
        /// then only send it if we calculate that the size changed.</param>
        private void ResizeAndRepaintTelnet(TelnetSingletonServer telnet, int width, int height, bool unconditional)
        {
            // Don't bother telling it to resize if its already the same size - this should stop resize spew looping:
            if (unconditional || telnet.ClientWidth != width || telnet.ClientHeight != height)
            {
                string resizeCmd = new string( new [] {(char)UnicodeCommand.RESIZESCREEN, (char)width, (char)height} );
                telnet.Write(resizeCmd);
                RepaintTelnet(telnet, true);
            }            
        }
        
        internal void ChangeTitle(string newTitle)
        {
            if (TitleText != newTitle) // For once, a direct simple reference-equals is really what we want.  Immutable strings should make this work quickly.
            {
                TitleText = newTitle;
                foreach (TelnetSingletonServer telnet in telnets)
                    SendTitleToTelnet(telnet);
            }
        }

        internal void SendTitleToTelnet(TelnetSingletonServer telnet)
        {
            // Make the telnet client learn about the new title:
            string changeTitleCmd = String.Format("{0}{1}{2}",
                                                  (char)UnicodeCommand.TITLEBEGIN,
                                                  TitleText,
                                                  (char)UnicodeCommand.TITLEEND);
            telnet.Write(changeTitleCmd);
        }

        /// <summary>
        /// Do the repaint of the telnet session.
        /// </summary>
        /// <param name="telnet">which telnet session to repaint</param>
        /// <param name="fullSync">if true, then ignore the diffing algorithm and just redraw everything.</param>
        internal void RepaintTelnet(TelnetSingletonServer telnet, bool fullSync)
        {
            if (fullSync || prevTelnetScreens[telnet] == null)
            {
                RepaintTelnetFull(telnet);
                return;
            }
            
            // If the state of the screen reverse, or the visual bell flags, has changed since last time,
            // spit out the characters to change the state:
            if (telnet.ReverseScreen != shared.Screen.ReverseScreen)
            {
                telnet.Write(shared.Screen.ReverseScreen ? ((char)UnicodeCommand.REVERSESCREENMODE) : ((char)UnicodeCommand.NORMALSCREENMODE));
                telnet.ReverseScreen = shared.Screen.ReverseScreen;
            }
            if (telnet.VisualBeep != shared.Screen.VisualBeep)
            {
                telnet.Write(shared.Screen.VisualBeep ? ((char)UnicodeCommand.VISUALBEEPMODE) : ((char)UnicodeCommand.AUDIOBEEPMODE));
                telnet.VisualBeep = shared.Screen.VisualBeep;
            }            
            string updateText = mostRecentScreen.DiffFrom(prevTelnetScreens[telnet]);
            telnet.Write(updateText);
            
            prevTelnetScreens[telnet] = mostRecentScreen.DeepCopy();
            for (int i = 0 ; i < shared.Screen.BeepsPending ; ++i)
                telnet.Write((char)UnicodeCommand.BEEP); // The terminal's UnicodeMapper will convert this to ascii 0x07 if the right terminal type.
        }
        
        /// <summary>
        /// Cover the case where the whole screen needs to be repainted from scratch.
        /// </summary>
        /// <param name="telnet">which telnet to paint to.</param>
        private void RepaintTelnetFull(TelnetSingletonServer telnet)
        {   
            List<IScreenBufferLine> buffer = mostRecentScreen.Buffer; // just to keep the name shorter below:

            // Sometimes the buffer is shorter than the terminal height if the resize JUST happened in the last Update():
            int rowsToPaint = Math.Min(shared.Screen.RowCount, buffer.Count);
            
            telnet.Write((char)UnicodeCommand.CLEARSCREEN);
            for (int row = 0 ; row < rowsToPaint ; ++row)
            {
                IScreenBufferLine lineBuffer = buffer[row];
                int columnOfLastContent = -1;
                for (int col = 0 ; col < lineBuffer.Length ; ++col)
                {
                    char ch = lineBuffer[col];
                    switch (ch)
                    {
                        case (char)0x0000: // The buffer pads null chars into the 'dead' space of the screen past the last printed char.
                            break;
                        case (char)0x0009: // tab chars - really shouldn't be in the buffer.
                            break;
                        default:
                            columnOfLastContent = col;
                            break;
                    }
                }
                if (columnOfLastContent >= 0) // skip for empty lines
                {
                    string line = lineBuffer.ToString().Substring(0, columnOfLastContent+1);
                    telnet.Write(line);
                }
                if (row < rowsToPaint-1) //don't write the eoln for the lastmost line.
                    telnet.Write((char)UnicodeCommand.STARTNEXTLINE);
            }

            // ensure cursor locatiom (in case it's not at the bottom):
            telnet.Write(String.Format("{0}{1}{2}",
                                       (char)UnicodeCommand.TELEPORTCURSOR,
                                       // The next two are cast to char because, for example, a value of 76 should be
                                       // encoded as (char)76 (which is 'L'), rather than as '7' followed by '6':
                                       (char)mostRecentScreen.CursorColumn,
                                       (char)mostRecentScreen.CursorRow));

            prevTelnetScreens[telnet] = mostRecentScreen.DeepCopy();
        }
        
        public void ClearScreen()
        {
            shared.Screen.ClearScreen();
            foreach (TelnetSingletonServer telnet in telnets)
            {
                telnet.Write((char)UnicodeCommand.CLEARSCREEN);
                prevTelnetScreens[telnet] = ScreenSnapShot.EmptyScreen(shared.Screen);
            }
        }

        public int NumTelnets()
        {
            return telnets.Count;
        }
        
        private int HowManyRowsFit()
        {
            return (int)(WindowRect.height - 100) / shared.Screen.CharacterPixelHeight;
        }

        private int HowManyColumnsFit()
        {
            return (int)(WindowRect.width - 65) / shared.Screen.CharacterPixelWidth;
        }
        
        private static GUISkin BuildPanelSkin()
        {
            GUISkin theSkin = kOS.Utilities.Utils.GetSkinCopy(HighLogic.Skin);

            theSkin.label.fontSize = 10;
            theSkin.label.normal.textColor = Color.white;
            theSkin.label.padding = new RectOffset(0, 0, 0, 0);
            theSkin.label.margin = new RectOffset(1, 1, 1, 1);

            theSkin.button.fontSize = 10;
            theSkin.button.padding = new RectOffset(0, 0, 0, 0);
            theSkin.button.margin = new RectOffset(0, 0, 0, 0);

            return theSkin;
        }
    }
}
