using System;
using System.Linq;
using System.Collections.Generic;
using kOS.Safe.Persistence;
using UnityEngine;
using kOS.Safe.Screen;
using kOS.Safe.Utilities;
using kOS.Module;
using kOS.UserIO;
using kOS.Safe.UserIO;

namespace kOS.Screen
{
    // Blockotronix 550 Computor Monitor
    public class TermWindow : KOSManagedWindow , ITermWindow
    {
        private const string TERMINAL_MODEL = "kOS-GREEN-ONE";
        private const int CHARSIZE = 8;
        private const string CONTROL_LOCKOUT = "kOSTerminal";
        private const int FONTIMAGE_CHARS_PER_ROW = 16;
        
        private static readonly string root = KSPUtil.ApplicationRootPath.Replace("\\", "/");
        private static readonly Color color = new Color(1, 1, 1, 1);
        private static readonly Color colorAlpha = new Color(0.9f, 0.9f, 0.9f, 0.6f);
        private static readonly Color textColor = new Color(0.45f, 0.92f, 0.23f, 0.9f);
        private static readonly Color textColorAlpha = new Color(0.45f, 0.92f, 0.23f, 0.5f);
        private static readonly Color textColorOff = new Color(0.8f, 0.8f, 0.8f, 0.7f);
        private static readonly Color textColorOffAlpha = new Color(0.8f, 0.8f, 0.8f, 0.3f);
        private Rect closeButtonRect = new Rect(0, 0, 0, 0); // will be resized later.        
        private Rect resizeButtonCoords = new Rect(0,0,0,0); // will be resized later.
        private Vector2 resizeOldSize;
        private bool resizeMouseDown;
        
        private bool consumeEvent;

        private KeyBinding rememberThrottleCutoffKey;
        private KeyBinding rememberThrottleFullKey;

        private bool allTexturesFound = true;
        private CameraManager cameraManager;
        private float cursorBlinkTime;
        private Texture2D fontImage = new Texture2D(0, 0, TextureFormat.DXT1, false);
        private bool isLocked;
        private Texture2D terminalImage = new Texture2D(0, 0, TextureFormat.DXT1, false);
        private Texture2D resizeButtonImage = new Texture2D(0, 0, TextureFormat.DXT1, false);
        private Texture2D networkZigZagImage = new Texture2D(0, 0, TextureFormat.DXT1, false);

        private SharedObjects shared;
        private KOSTextEditPopup popupEditor;
        private Color currentTextColor = new Color(1,1,1,1); // a dummy color at first just so it won't crash before TerminalGUI() where it's *really* set.

        // data stored per telnet client attached:
        private List<TelnetSingletonServer> telnets; // support exists for more than one telnet client to be attached to the same terminal, thus this is a list.
        private Dictionary<TelnetSingletonServer, IScreenSnapShot> prevTelnetScreens;
        
        private ExpectNextChar inputExpected = ExpectNextChar.NORMAL;
        private int pendingWidth; // width to come from a resize combo.
        
        public string TitleText {get; private set;}

        private IScreenSnapShot mostRecentScreen;
        
        private DateTime lastBufferGet = DateTime.Now;
        private DateTime lastTelnetIncrementalRepaint = DateTime.Now;

        public TermWindow()
        {
            IsPowered = true;
            windowRect = new Rect(50, 60, 0, 0); // will get resized later in AttachTo().
            telnets = new List<TelnetSingletonServer>();
            prevTelnetScreens = new Dictionary<TelnetSingletonServer, IScreenSnapShot>();
        }

        public bool ShowCursor { get; set; }

        public void Awake()
        {
            LoadTexture("GameData/kOS/GFX/font_sml.png", ref fontImage);
            LoadTexture("GameData/kOS/GFX/monitor_minimal.png", ref terminalImage);
            LoadTexture("GameData/kOS/GFX/resize-button.png", ref resizeButtonImage);
            LoadTexture("GameData/kOS/GFX/network-zigzag.png", ref networkZigZagImage);
            
            var gObj = new GameObject( "texteditPopup", typeof(KOSTextEditPopup) );
            DontDestroyOnLoad(gObj);
            popupEditor = (KOSTextEditPopup)gObj.GetComponent(typeof(KOSTextEditPopup));
            popupEditor.SetUniqueId(uniqueId + 5);
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
        }

        public override void Close()
        {
            // Diable GUI and release all locks
            base.Close();
        }

        public void Toggle()
        {
            if (IsOpen()) Close();
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
            if (!IsOpen()) return;

            if (isLocked) ProcessKeyEvents();
            
            try
            {
                if (PauseMenu.isOpen || FlightResultsDialog.isDisplaying) return;
            }
            catch(NullReferenceException)
            {
            }
            
            GUI.skin = HighLogic.Skin;
            
            GUI.color = isLocked ? color : colorAlpha;

            // Should probably make "gui screen name for my CPU part" into some sort of utility method:
            KOSNameTag partTag = shared.KSPPart.Modules.OfType<KOSNameTag>().FirstOrDefault();
            ChangeTitle(CalcualteTitle());

            windowRect = GUI.Window(uniqueId, windowRect, TerminalGui, TitleText);
            
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

            if (!IsOpen() ) return;
         
            UpdateLogic();

            if (!isLocked) return;

            cursorBlinkTime += Time.deltaTime;
            if (cursorBlinkTime > 1) cursorBlinkTime -= 1;
        }
        
        void TelnetOutputUpdate()
        {
            DateTime newTime = DateTime.Now;
            
            // Throttle it back so the faster Update() rates don't cause pointlessly repeated work:
            // Needs to be no faster than the fastest theoretical typist or script might change the view.
            if (newTime > lastTelnetIncrementalRepaint + System.TimeSpan.FromMilliseconds(50)) // = 1/20th second.
            {
                lastTelnetIncrementalRepaint = newTime;
                foreach (TelnetSingletonServer telnet in telnets)
                {
                    RepaintTelnet(telnet, false); // try the incremental differ update.
                }
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
            for( int i = 0 ; i < telnets.Count ; ++i)
            {
                TelnetSingletonServer telnet = telnets[i];
                System.Console.WriteLine("eraseme:ProcessTelnetInput: working on a telnet number ["+i+"] which is "+(telnet==null?"null":"NOT null"));
                while (telnet.InputWaiting())
                {
                    System.Console.WriteLine("eraseme:ProcessTelnetInput: now calling ProcessOneInputChar, with telnet = " + (telnet==null ? "null" : "NOT null"));
                    ProcessOneInputChar(telnet.ReadChar(), telnet);
                }
            }
        }

        /// <summary>
        /// Respond to one single input character in unicode, using the pretend
        /// virtual unicode terminal keycodes described in the UnicodeCommand enum.
        /// To keep things simple, all key input is coerced into single unicode chars
        /// even if the actual keypress takes multiple characters to express in its
        /// native form (i.e. ESC [ A means left-arrow on a VT100 terminal.  If
        /// a telnet is talking via VT100 codes, that ESC [ A will get converted into
        /// a single UnicdeCommand.LEFTCURSORONE character before being sent here.)
        /// <br/>
        /// This method is public because it is also how other mods should send input
        /// to the terminal if they want some other soruce to send simulated keystrokes.
        /// </summary>
        /// <param name="ch">The character, which might be a UnicodeCommand char</param>
        /// <param name="whichTelnet">If this came from a telnet session, which one did it come from?
        /// Set to null in order to say it wasn't from a telnet but was from the interactive GUI</param>
        public void ProcessOneInputChar(char ch, TelnetSingletonServer whichTelnet)
        {
            System.Console.WriteLine("eraseme:ProcessOneInputChar( "+(char)ch+", "+ (whichTelnet==null ? "null" : "NOT null") + ")");
            // Weird exceptions for multi-char data combos that would have been begun on previous calls to this method:
            switch (inputExpected)
            {
                case ExpectNextChar.RESIZEWIDTH:
                    pendingWidth = (int)ch;
                    inputExpected = ExpectNextChar.RESIZEHEIGHT;
                    return;
                case ExpectNextChar.RESIZEHEIGHT:
                    int height = (int)ch;
                    shared.Screen.SetSize(height, pendingWidth);
                    inputExpected = ExpectNextChar.NORMAL;
                    return;
                default:
                    break;
            }

            // Printable ascii section of unicode - the common vanila situation
            // (Idea: Since this is all unicode anyway, should we allow a wider range to
            // include multi-language accent characters and so on?  Answer: to do so we'd
            // first need to expand the font pictures in the fontimage file, so it's a
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
                            {
                                System.Console.WriteLine("eraseme:ProcessOneInputChar: in whichTelnet=null condition.");
                                Close();
                            }
                            else
                            {
                                System.Console.WriteLine("eraseme:ProcessOneInputChar: in whichTelnet=NOT null condition.");
                                whichTelnet.DisconnectFromProcessor();
                            }
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
            }
        }

        void SpecialKey(char key)
        {
            if (shared != null && shared.Interpreter != null)
            {
                shared.Interpreter.SpecialKey(key);
            }
        }
        
        void GetNewestBuffer()
        {
            DateTime newTime = DateTime.Now;
            
            // Throttle it back so the faster Update() rates don't cause pointlessly repeated work:
            // Needs to be no faster than the fastest theoretical typist or script might change the view.
            if (newTime > lastBufferGet + System.TimeSpan.FromMilliseconds(50)) // = 1/20th second.
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
            GUI.DrawTexture(new Rect(15, 20, windowRect.width-30, windowRect.height-55), terminalImage);

            if (telnets.Count > 0)
                DrawTelnetStatus();

            closeButtonRect = new Rect(windowRect.width-75, windowRect.height-30, 50, 25);

            resizeButtonCoords = new Rect(windowRect.width-resizeButtonImage.width,
                                          windowRect.height-resizeButtonImage.height,
                                          resizeButtonImage.width,
                                          resizeButtonImage.height);
            if (GUI.RepeatButton(resizeButtonCoords, resizeButtonImage ))
            {
                if (! resizeMouseDown)
                {
                    // Remember the fact that this mouseDown started on the resize button:
                    resizeMouseDown = true;
                    resizeOldSize = new Vector2(windowRect.width, windowRect.height);
                }
            }

            if (GUI.Button(closeButtonRect, "Close"))
            {
                Close();
                Event.current.Use();
            }


            if (IsPowered)
            {
                currentTextColor = isLocked ? textColor : textColorAlpha;
            }
            else
            {
                currentTextColor = isLocked ? textColorOff : textColorOffAlpha;
            }

            GUI.BeginGroup(new Rect(28, 38, screen.ColumnCount*CHARSIZE, screen.RowCount*CHARSIZE));

            List<IScreenBufferLine> buffer = mostRecentScreen.Buffer; // just to keep the name shorter below:

            // Sometimes the buffer is shorter than the terminal height if the resize JUST happened in the last Update():
            int rowsToPaint = System.Math.Min(screen.RowCount, buffer.Count);

            for (int row = 0; row < rowsToPaint; row++)
            {
                IScreenBufferLine lineBuffer = buffer[row];
                for (int column = 0; column < lineBuffer.Length; column++)
                {
                    char c = lineBuffer[column];
                    if (c != 0 && c != 9 && c != 32) ShowCharacterByAscii(c, column, row, currentTextColor);
                }
            }

            bool blinkOn = cursorBlinkTime < 0.5f &&
                           screen.CursorRowShow < screen.RowCount &&
                           IsPowered &&
                           ShowCursor;
            
            if (blinkOn)
            {
                ShowCharacterByAscii((char)1, screen.CursorColumnShow, screen.CursorRowShow, currentTextColor);
            }
            GUI.EndGroup();
            
            GUI.Label(new Rect(windowRect.width/2-40,windowRect.height-20,100,10),screen.ColumnCount+"x"+screen.RowCount);

            CheckResizeDrag(); // Has to occur before DragWindow or else DragWindow will consume the event and prevent drags from being seen by the resize icon.
            GUI.DragWindow();
        }

        /// <summary>
        ///  Draw a little status line at the bottom as a reminder that you are sharing the terminal with a telnet session:
        /// </summary>
        protected void DrawTelnetStatus()
        {
            int num = telnets.Count;
            string message = String.Format( "{0} telnet client{1} attached", num, (num == 1 ? "" : "s"));
            GUI.DrawTexture(new Rect(10, windowRect.height - 25, 25, 25), networkZigZagImage);
            GUI.Label(new Rect(40, windowRect.height - 25, 160, 20), message);
        }
        
        protected void CheckResizeDrag()
        {
            if (Input.GetMouseButton(0)) // mouse button is down
            {
                if (resizeMouseDown) // and it's in the midst of a drag.
                {
                    Vector2 dragDelta = mousePosAbsolute - mouseButtonDownPosAbsolute;
                    windowRect = new Rect(windowRect.xMin,
                                          windowRect.yMin,
                                          System.Math.Max(resizeOldSize.x + dragDelta.x, 200),
                                          System.Math.Max(resizeOldSize.y + dragDelta.y, 200));
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
        
        void ShowCharacterByAscii(char ch, int x, int y, Color textColor)
        {
            int tx = ch % FONTIMAGE_CHARS_PER_ROW;
            int ty = ch / FONTIMAGE_CHARS_PER_ROW;

            ShowCharacterByXY(x, y, tx, ty, textColor);
        }

        void ShowCharacterByXY(int x, int y, int tx, int ty, Color textColor)
        {
            GUI.BeginGroup(new Rect((x * CHARSIZE), (y * CHARSIZE), CHARSIZE, CHARSIZE));
            GUI.color = textColor;
            GUI.DrawTexture(new Rect(tx * -CHARSIZE, ty * -CHARSIZE, fontImage.width, fontImage.height), fontImage);
            GUI.EndGroup();
        }

        public Rect GetRect()
        {
            return windowRect;
        }
        
        public void Print( string str )
        {
            shared.Screen.Print( str );
        }

        internal void AttachTo(SharedObjects shared)
        {
            this.shared = shared;
            this.shared.Window = this;
            NotifyOfScreenResize(this.shared.Screen);
            this.shared.Screen.AddResizeNotifier(NotifyOfScreenResize);
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
            System.Console.WriteLine("eraseme: Before DetachTelnet: There are now " + telnets.Count + " telnet servers attached.");
            server.DisconnectFromProcessor();
            telnets.Remove(server);
            System.Console.WriteLine("eraseme: After DetachTelnet: There are now " + telnets.Count + " telnet servers attached.");
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
            windowRect = new Rect(windowRect.xMin, windowRect.yMin, sb.ColumnCount*CHARSIZE + 65, sb.RowCount*CHARSIZE + 100);

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
            System.Console.WriteLine("eraseme: debug: sendTitleToTelnet is sending this:");
            for (int i=0; i<changeTitleCmd.Length; ++i) { System.Console.WriteLine("eraseme: changeTitleCmd["+i+"] = (int)" + (int)changeTitleCmd[i] + ", (char)"+ (char)changeTitleCmd[i]); }
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
            
            System.Console.WriteLine("eraseme: incremental repaint of telnet.");
            string updateText = mostRecentScreen.DiffFrom(prevTelnetScreens[telnet]);
            telnet.Write(updateText);
            
            prevTelnetScreens[telnet] = mostRecentScreen.DeepCopy();
        }
        
        /// <summary>
        /// Cover the case where the whole screen needs to be repainted from scratch.
        /// </summary>
        /// <param name="telnet">which telnet to paint to.</param>
        private void RepaintTelnetFull(TelnetSingletonServer telnet)
        {   
            System.Console.WriteLine("eraseme: fullrepaint of telnet.");
            List<IScreenBufferLine> buffer = mostRecentScreen.Buffer; // just to keep the name shorter below:

            // Sometimes the buffer is shorter than the terminal height if the resize JUST happened in the last Update():
            int rowsToPaint = System.Math.Min(shared.Screen.RowCount, buffer.Count);
            
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
            System.Console.WriteLine("eraseme: moving cursor to "+shared.Screen.CursorColumnShow +"x" + shared.Screen.CursorRowShow +".");
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
                telnet.Write((char)UnicodeCommand.CLEARSCREEN);
        }

        public int NumTelnets()
        {
            return telnets.Count;
        }
        
        private int HowManyRowsFit()
        {
            return (int)(windowRect.height - 100) / CHARSIZE;
        }

        private int HowManyColumnsFit()
        {
            return (int)(windowRect.width - 65) / CHARSIZE;
        }

    }
}
