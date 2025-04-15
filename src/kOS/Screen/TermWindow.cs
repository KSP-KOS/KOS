using System;
using System.Linq;
using System.Collections.Generic;
using kOS.Safe.Persistence;
using UnityEngine;
using UnityEngine.Networking;
using kOS.Safe.Screen;
using kOS.Safe.Encapsulation;
using kOS.Module;
using kOS.UserIO;
using kOS.Safe.UserIO;
using KSP.UI.Dialogs;
using kOS.Safe.Utilities;

namespace kOS.Screen
{
    // Blockotronix 550 Computor Monitor
    public class TermWindow : KOSManagedWindow , ITermWindow
    {
        public const string CONTROL_LOCKOUT = "kOSTerminal";

        /// <summary>
        /// Set to true only when compiling a version specifically for the purpose
        /// of debugging the use of international Unicode chars on a US-market
        /// keyboard that cannot type characters above ascii 127.  (The "alt-NNNN" method
        /// doesn't work in the terminal so you need to actually physically have the keys
        /// on the keyboard to type the letters.  This turns on a mapping of a few of
        /// the lesser used ASCII values to some other keys > 127.  This mapping
        /// will be very wrong to publish in a release, or even to use in a normal
        /// DEBUG compile (which is why it's not triggering on the DEBUG flag).
        /// ONLY set to true for testing this one feature, never for anything else.)
        /// 
        /// NOTE:  The mapping is only implemented for the in-game terminal, NOT for the
        /// telnet terminal.  This is because (presumably) the telnet terminal has other
        /// ways to type these characters (i.e. Putty can obey the ALT-numberpad technique)
        /// and doesn't need this hack-y test.
        /// </summary>
        private const bool DebugInternational = false;

        private static string root;
        private static readonly Color color = new Color(1f, 1f, 1f, 1.1f); // opaque window color when focused
        private static readonly Color bgColor = new Color(0.0f, 0.0f, 0.0f, 1.0f); // black background of terminal
        private static readonly Color textColor = new Color(0.5f, 1.0f, 0.5f, 1.0f); // font color on terminal
        private static readonly Color textColorOff = new Color(0.8f, 0.8f, 0.8f, 0.7f); // font color when power starved.
        private Rect closeButtonRect;
        private Rect resizeButtonCoords;
        private GUIStyle tinyToggleStyle;
        private Vector2 resizeOldSize;
        private bool resizeMouseDown;
        private int formerCharPixelHeight;
        private int formerCharPixelWidth;
        private int postponedCharPixelHeight;
        
        private bool consumeEvent;
        private bool fontGotResized;
        private bool keyClickEnabled;
        
        private bool collapseFastBeepsToOneBeep = false; // This is a setting we might want to fiddle with depending on opinion.

        private bool allTexturesFound = true;
        private float cursorBlinkTime;

        private Font font;
        private int fontSize;
        private string[] tryFontNames = {
            "User pick Goes Here",  // overwrite this first one with the user selection - the rest are a fallback just in case
            "Consolas Bold",        // typical Windows good programming font
            "Consolas",
            "Monaco Bold",          // typical Mac good programming font
            "Monaco",
            "Liberation Mono Bold", // typical Linux good programming font
            "Liberation Mono",
            "Courier New Bold",     // The Courier ones are ugly fallbacks just in case.
            "Courier Bold",
            "Courier New",
            "Courier",
            "Arial"                 // very bad, proportional, but guaranteed to exist in Unity no matter what.
        };
        private GUISkin terminalLetterSkin;
            
        private bool isLocked;
        /// <summary>How long blinks should last for, for various blinking needs</summary>
        private readonly TimeSpan blinkDuration = System.TimeSpan.FromMilliseconds(150);
        /// <summary>How long to pad between consecutive blinks to ensure they are visibly detectable as distinct blinks.</summary>
        private readonly TimeSpan blinkCoolDownDuration = System.TimeSpan.FromMilliseconds(50);
        /// <summary>At what milliseconds-from-epoch timestamp will the current blink be over.</summary>
        private DateTime blinkEndTime;
        /// <summary>text color that changes depending on if the computer is on</summary>
        private Color currentTextColor;

        /// <summary>Telnet repaints happen less often than Update()s.  Not every Update() has a telnet repaint happening.
        /// This tells you whether there was one this update.</summary>
        private bool telnetsGotRepainted;

        private Texture2D terminalImage;
        private Texture2D terminalFrameImage;
        private Texture2D terminalFrameActiveImage;
        private GUIStyle terminalImageStyle;
        private GUIStyle terminalFrameStyle;
        private GUIStyle terminalFrameActiveStyle;

        private Texture2D resizeButtonImage;
        private Texture2D networkZigZagImage;
        private Texture2D brightnessButtonImage;
        private Texture2D fontHeightButtonImage;
        private int guiTerminalBeepsPending;

        private SharedObjects shared;
        private KOSTextEditPopup popupEditor;

        // data stored per telnet client attached:
        private volatile List<TelnetSingletonServer> telnets; // support exists for more than one telnet client to be attached to the same terminal, thus this is a list.
        private readonly Dictionary<TelnetSingletonServer, IScreenSnapShot> prevTelnetScreens;
        
        private ExpectNextChar inputExpected = ExpectNextChar.NORMAL;
        private int pendingWidth; // width to come from a resize combo.
        
        public string TitleText {get; private set;}

        private IScreenSnapShot mostRecentScreen;
        
        private DateTime lastBufferGet = DateTime.Now;
        private DateTime lastTelnetIncrementalRepaint = DateTime.Now;
        private GUISkin customSkin;
        
        private bool uiGloballyHidden = false;

        private Sound.SoundMaker soundMaker;        

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
            // set dummy rectangles
            closeButtonRect = new Rect(0, 0, 0, 0); // will be resized later.
            resizeButtonCoords = new Rect(0, 0, 0, 0); // will be resized later.

            terminalImage = Utilities.Utils.GetTextureWithErrorMsg("kOS/GFX/dds_monitor_minimal", false);
            terminalFrameImage = Utilities.Utils.GetTextureWithErrorMsg("kOS/GFX/dds_monitor_minimal_frame", false);
            terminalFrameActiveImage = Utilities.Utils.GetTextureWithErrorMsg("kOS/GFX/dds_monitor_minimal_frame_active", false);
            resizeButtonImage = Utilities.Utils.GetTextureWithErrorMsg("kOS/GFX/dds_resize-button", false);
            networkZigZagImage = Utilities.Utils.GetTextureWithErrorMsg("kOS/GFX/dds_network-zigzag", false);
            brightnessButtonImage = Utilities.Utils.GetTextureWithErrorMsg("kOS/GFX/dds_brightness-button", false);
            fontHeightButtonImage = Utilities.Utils.GetTextureWithErrorMsg("kOS/GFX/dds_font-height-button", false);

            allTexturesFound =
                terminalImage != null &&
                terminalFrameImage != null &&
                terminalFrameActiveImage != null &&
                resizeButtonImage != null &&
                networkZigZagImage != null &&
                brightnessButtonImage != null &&
                fontHeightButtonImage != null;
;
            terminalImageStyle = Create9SliceStyle(terminalImage);
            terminalFrameStyle = Create9SliceStyle(terminalFrameImage);
            terminalFrameActiveStyle = Create9SliceStyle(terminalFrameActiveImage);

            tinyToggleStyle = new GUIStyle(HighLogic.Skin.toggle)
            {
                fontSize = 10
            };

            popupEditor = gameObject.AddComponent<KOSTextEditPopup>();
            popupEditor.SetUniqueId(UniqueId + 5);
            
            customSkin = BuildPanelSkin();
            terminalLetterSkin = BuildPanelSkin();

            GameEvents.onHideUI.Add (OnHideUI);
            GameEvents.onShowUI.Add (OnShowUI);

            soundMaker = gameObject.AddComponent<Sound.SoundMaker>();
        }


        /// <summary>
        /// Unity lacks gui styles for GUI.DrawTexture(), so to make it do
        /// 9-slice stretching, we have to draw the 9slice image as a GUI.Label.
        /// But GUI.Labels that render a Texture2D instead of text, won't stretch
        /// larger than the size of the image file no matter what you do (only smaller).
        /// So to make it stretch the image in a label, the image has to be implemented
        /// as part of the label's background defined in the GUIStyle instead of as a
        /// normal image element.  This sets up that style, which you can then render
        /// by making a GUILabel use this style and have dummy empty string content.
        /// </summary>
        /// <returns>The slice style.</returns>
        /// <param name="fromTexture">From texture.</param>
        private GUIStyle Create9SliceStyle(Texture2D fromTexture)
        {
            GUIStyle style = new GUIStyle();
            style.normal.background = fromTexture;
            style.border = new RectOffset(10, 10, 10, 10);
            style.stretchWidth = true;
            style.stretchHeight = true;
            return style;
        }

        new public void OnDestroy()
        {
            LoseFocus();
            GameEvents.onHideUI.Remove(OnHideUI);
            GameEvents.onShowUI.Remove(OnShowUI);

            base.OnDestroy();
        }
        
        public kOS.Safe.Sound.ISoundMaker GetSoundMaker()
        {
            return soundMaker;
        }

        public void OpenPopupEditor(Volume v, GlobalPath path)
        {
            popupEditor.AttachTo(this, v, path);
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
            base.GetFocus();
            Lock();
        }

        public override void LoseFocus()
        {
            base.LoseFocus();
            Unlock();
        }

        public override void Open()
        {
            base.Open();
            BringToFront();
            guiTerminalBeepsPending = 0; // Closing and opening the window will wipe pending beeps from the beep queue.
        }
            
        /// <param name="recalcWidth">If true, recalc the font width from height if there's any font change.
        /// Setting this to true should only be done from inside an OnGUI call.
        /// If you call this from outside an OnGUI context, set this to false.</param>
        private void GetFontIfChanged(bool recalcWidth)
        {
            int newSize = shared.Screen.CharacterPixelHeight;
            string newName =  SafeHouse.Config.TerminalFontName;
            if (fontSize != newSize || !(tryFontNames[0].Equals(newName)))
            {
                fontSize = newSize;
                tryFontNames[0] = newName;
                font = AssetManager.Instance.GetSystemFontByNameAndSize(tryFontNames, fontSize, false);

                terminalLetterSkin.label.font = font;
                terminalLetterSkin.label.fontSize = fontSize;
                if (recalcWidth)
                {
                    CharacterInfo chInfo;
                    terminalLetterSkin.label.font.RequestCharactersInTexture("X"); // Make sure the char in the font is lazy-loaded by Unity.
                    terminalLetterSkin.label.font.GetCharacterInfo('X', out chInfo);
                    shared.Screen.CharacterPixelWidth = chInfo.advance;

                    NotifyOfScreenResize(shared.Screen);
                }
            }
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


            InputLockManager.SetControlLock(ControlTypes.All, CONTROL_LOCKOUT);
            // Prevent editor keys from being pressed while typing
            EditorLogic editor = EditorLogic.fetch;
                //TODO: POST 0.90 REVIEW
            if (editor != null && InputLockManager.IsUnlocked(ControlTypes.All)) editor.Lock(true, true, true, CONTROL_LOCKOUT);
        }

        private void Unlock()
        {
            if (!isLocked) return;
            
            isLocked = false;

            InputLockManager.RemoveControlLock(CONTROL_LOCKOUT);

            EditorLogic editor = EditorLogic.fetch;
            if (editor != null) editor.Unlock(CONTROL_LOCKOUT);

        }

        void OnGUI()
        {
            if (!IsOpen) return;

            GetFontIfChanged(true);

            if (isLocked) ProcessKeyEvents();
            if (FlightResultsDialog.isDisplaying) return;
            if (uiGloballyHidden)
            {
                kOS.Safe.Encapsulation.IConfig cfg = kOS.Safe.Utilities.SafeHouse.Config;
                if (cfg == null || cfg.ObeyHideUI)
                    return;
            }

            GUI.skin = HighLogic.Skin;
            
            GUI.color = color;

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
            ProcessUnconsumedInput(); // Moved here from OnGUI because it needs to run even when the GUI terminal is closed.
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
                return soundMaker.BeginFileSound("beep");
            }
            return true;
        }

        void TelnetOutputUpdate()
        {
            DateTime newTime = DateTime.Now;
            telnetsGotRepainted = false;
            
            // Throttle it back so the faster Update() rates don't cause pointlessly repeated work:
            // Needs to be no faster than the fastest theoretical typist or script might change the view.
            if (newTime > lastTelnetIncrementalRepaint + System.TimeSpan.FromMilliseconds(50)) // = 1/20th second.
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
                // This ugly hack is here to solve a bug with Unity mapping
                // Keycodes to Unicode chars incorrectly on its Linux version:
                char c;
                if ((e.character & 0xff00) == 0xff00) // Only trigger on Unicode values 0xff00 through 0xffff, to avoid issue #2061
                    c = (char)(e.character & 0x007f); // When doing this to solve issue #206 (yes, #206, separate from #2061 above)
                else
                    c = e.character;

                // command sequences
                if (e.keyCode == KeyCode.C && e.control) // Ctrl+C
                {
                    ProcessOneInputChar((char)UnicodeCommand.BREAK, null, true, true);
                    consumeEvent = true;
                    return;
                }
                // Command used to be Control-shift-X, now we don't care if shift is down aymore, to match the telnet expereince
                // where there is no such thing as "uppercasing" a control char.
                if ((e.keyCode == KeyCode.X && e.control) ||
                    (e.keyCode == KeyCode.D && e.control) // control-D to match the telnet experience
                   )
                {
                    ProcessOneInputChar((char)0x000d, null, true, true);
                    consumeEvent = true;
                    return;
                }
                
                if (e.keyCode == KeyCode.A && e.control)
                {
                    ProcessOneInputChar((char)0x0001, null, true, true);
                    consumeEvent = true;
                    return;
                }
                
                if (e.keyCode == KeyCode.E && e.control)
                {
                    ProcessOneInputChar((char)0x0005, null, true, true);
                    consumeEvent = true;
                    return;
                }
                
                if (!IsSpecial(c)) // printable characters
                {
#pragma warning disable CS0162
                    if (DebugInternational)
                        c = DebugInternationalMapping(c);
#pragma warning restore CS0162
                    ProcessOneInputChar(c, null, true, true);
                    consumeEvent = true;
                    cursorBlinkTime = 0.0f; // Don't blink while the user is still actively typing.
                }
                else if (e.keyCode != KeyCode.None)
                {
                    consumeEvent = true;
                    switch (e.keyCode)
                    {
                        case KeyCode.Tab:          ProcessOneInputChar('\t', null, true, true);                                  break;
                        case KeyCode.LeftArrow:    ProcessOneInputChar((char)UnicodeCommand.LEFTCURSORONE, null, true, true);    break;
                        case KeyCode.RightArrow:   ProcessOneInputChar((char)UnicodeCommand.RIGHTCURSORONE, null, true, true);   break;
                        case KeyCode.UpArrow:      ProcessOneInputChar((char)UnicodeCommand.UPCURSORONE, null, true, true);      break;
                        case KeyCode.DownArrow:    ProcessOneInputChar((char)UnicodeCommand.DOWNCURSORONE, null, true, true);    break;
                        case KeyCode.Home:         ProcessOneInputChar((char)UnicodeCommand.HOMECURSOR, null, true, true);       break;
                        case KeyCode.End:          ProcessOneInputChar((char)UnicodeCommand.ENDCURSOR, null, true, true);        break;
                        case KeyCode.PageUp:       ProcessOneInputChar((char)UnicodeCommand.PAGEUPCURSOR, null, true, true);     break;
                        case KeyCode.PageDown:     ProcessOneInputChar((char)UnicodeCommand.PAGEDOWNCURSOR, null, true, true);   break;
                        case KeyCode.Delete:       ProcessOneInputChar((char)UnicodeCommand.DELETERIGHT, null, true, true);      break;
                        case KeyCode.Backspace:    ProcessOneInputChar((char)UnicodeCommand.DELETELEFT, null, true, true);       break;
                        case KeyCode.KeypadEnter:  // (deliberate fall through to next case)
                        case KeyCode.Return:       ProcessOneInputChar((char)UnicodeCommand.STARTNEXTLINE, null, true, true);    break;
                        
                        // More can be added to the list here to support things like F1, F2, etc.  But at the moment we don't use them yet.
                        
                        // default: ignore and allow the event to pass through to whatever else wants to read it:
                        default:                   consumeEvent = false;                                             break;
                    }
                    cursorBlinkTime = 0.0f;// Don't blink while the user is still actively typing.
                }
            }
        }

        private static bool IsSpecial(char c)
        {
            if (c < 0x0020 || c > 0xE000)
                return true;
            if (Enum.IsDefined(typeof(UnicodeCommand), (int)c))
                return true;
            return false;
        }

        private static char DebugInternationalMapping(char c)
        {
            // Hex codes are used for the unicode letters here, just
            // in case some kOS contributor tries to edit this source
            // file in a non-Unicode-aware editor that would break
            // the code.  (This way it only would break what's written
            // in the comment, not the code).
            if (c == '%')
                return (char)0x00c6; // 'Æ'
            if (c == '$')
                return (char)0x015d; // 'ŝ'
            if (c == '&')
                return (char)0x042f; // 'Я'
            if (c == '~')
                return (char)0x00f1; // 'ñ'
            return c;
        }

        /// <summary>
        /// A means to get the current terminal font size without
        /// having to expose the terminal's inner members.
        /// </summary>
        /// <returns>The font size.</returns>
        public int GetFontSize()
        {
            return shared.Screen.CharacterPixelHeight;
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
            // It's possible to close and remove telnets from the list during the processing
            // of input (if the detach signal Ctrl-D is sent).  Therefore we have to
            // make this temp copy to prorect against the C# error "Collection was Modified"
            // during the foreach loop:
            TelnetSingletonServer[] tempTelnetList = new TelnetSingletonServer[telnets.Count()];
            telnets.CopyTo(tempTelnetList);
            foreach (TelnetSingletonServer telnet in tempTelnetList)
            {
                if (telnet.ConnectedProcessor != null)
                {
                    while (telnet.InputWaiting())
                    {
                        ProcessOneInputChar(telnet.ReadChar(), telnet, true, true);
                    }
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
        /// <param name="allowQueue">true if the keypress should get queued if we're not ready for it
        /// right now.  If false, then the keypress will be ignored if we're not ready for it.</param>
        /// <param name="forceQueue">true if the keypress MUST get queued even if we are ready for it.
        /// Use this for input that can jam in quickly faster than the interpeter could respond, to ensure it
        /// doesn't get the order mixed up.  (I.e. use it for paste buffer dumps or telnet input, but not
        /// live GUI typed stuff.)</param>
        /// <returns>True if the input got consuemed or enqueued.  If the input was blocked and not ignored, it returns false.</returns>
        public bool ProcessOneInputChar(char ch, TelnetSingletonServer whichTelnet, bool allowQueue, bool forceQueue)
        {
            // Weird exceptions for multi-char data combos that would have been begun on previous calls to this method:
            switch (inputExpected)
            {
                case ExpectNextChar.RESIZEWIDTH:
                    pendingWidth = ch;
                    inputExpected = ExpectNextChar.RESIZEHEIGHT;
                    return true;
                case ExpectNextChar.RESIZEHEIGHT:
                    int height = ch;
                    shared.Screen.SetSize(height, pendingWidth);
                    inputExpected = ExpectNextChar.NORMAL;
                    return true;
                default:
                    break;
            }

            if (! IsSpecial(ch))
            {
                 return Type(ch, allowQueue, forceQueue);
            }
            else
            {
                switch(ch)
                {
                    // WARNING: CHARACTERS IN THIS SECTION ARE BYPASSING THE QUEUE.  BUT THEY ARE
                    // WEIRD ASYNC THINGS THAT PROBABLY SHOULD:

                    // A few conversions from UnicodeCommand into those parts of ASCII that it 
                    // maps directly into nicely, otherwise just pass it through to SpecialKey():

                    case (char)UnicodeCommand.DELETELEFT:
                    case (char)8:
                        Type((char)8, allowQueue, forceQueue);
                        break;
                    case (char)UnicodeCommand.STARTNEXTLINE:
                    case '\r':
                        Type('\r', allowQueue, forceQueue);
                        break;
                    case '\t':
                        Type('\t', allowQueue, forceQueue);
                        break;
                    case (char)UnicodeCommand.RESIZESCREEN:
                        inputExpected = ExpectNextChar.RESIZEWIDTH;
                        break; // next expected char is the width.
                        
                    // Finish session:  If GUI, then close window.  If Telnet, then detatch from the session:
                    
                    case (char)0x0004/*control-D*/: // How users of unix shells are used to doing this.
                    case (char)0x0018/*control-X*/: // How kOS did it in the past in the GUI window.
                        if (shared.Terminal.IsAtStartOfCommand())
                        {
                            if (whichTelnet == null)
                                Close();
                            else
                                DetachTelnet(whichTelnet);
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
                        return SpecialKey(ch, allowQueue, forceQueue);
                }
                return true;
            }

            // else ignore it - unimplemented char.
        }

        /// <summary>
        /// This is identical to calling ProcessOneInputChar with forceQueue defaulted to true,
        /// and it returns void instead of bool.
        /// <para>This is being done this way because it has to match exactly to how the
        /// signature of the method used to look, to keep it compatible with the DLL for
        /// kOSPropMonitor without kOSPropMonitor being recompiled.</para>
        /// </summary>
        /// <param name="ch"></param>
        /// <param name="whichTelnet"></param>
        /// <param name="allowQueue"></param>
        /// <returns></returns>
        public void ProcessOneInputChar(char ch, TelnetSingletonServer whichTelnet, bool allowQueue)
        {
            ProcessOneInputChar(ch, whichTelnet, allowQueue, true);
        }

        /// <summary>
        /// This is identical to calling ProcessOneInputChar with allowQueu and forceQueue both defaulted to true,
        /// and it reutrns void instead of bool.
        /// <para>This is being done this way because it has to match exactly to how the
        /// signature of the method used to look, to keep it compatible with the DLL for
        /// kOSPropMonitor without kOSPropMonitor being recompiled.</para>
        /// </summary>
        /// <param name="ch"></param>
        /// <param name="whichTelnet"></param>
        /// <param name="allowQueue"></param>
        /// <returns></returns>
        public void ProcessOneInputChar(char ch, TelnetSingletonServer whichTelnet)
        {
            ProcessOneInputChar(ch, whichTelnet, true, true);
        }

        /// <summary>
        /// Type a normal unicode char (not a magic control char) to the terminal,
        /// or if the interpreter is busy queue it for later if flags allow.
        /// </summary>
        /// <param name="ch">char to type</param>
        /// <param name="doQueuing">true if you want to queue the char when the interpreter isn't at the prompt
        /// accepting input yet (i.e. the prev command is still running.) if false it will just do nothing in
        /// that case.</param>
        /// <param name="forceQueue">true if you want to queue the char even when the interpreter could have accepted
        /// it.  (never have this true if doQueueing is false.)</param>
        /// <returns>true if the char got either used or queued.  False means calling the method had no effect.
        /// This can only return false when doQueuing is false.</returns>
        bool Type(char ch, bool doQueuing = true, bool forceQueue = true)
        {
            bool accepted = false;
            if (shared != null)
            {
                if ((!forceQueue) && shared.Terminal != null && shared.Terminal.IsWaitingForCommand())
                {
                    shared.Terminal.Type(ch);
                    accepted = true;
                }
                else if (doQueuing)
                {
                    shared.Screen.CharInputQueue.Enqueue(ch);
                    accepted = true;
                }
                if (IsOpen && keyClickEnabled && doQueuing)
                    shared.SoundMaker.BeginFileSound("click");
            }
            return accepted;
        }

        /// <summary>
        /// Type a special control char to the terminal,
        /// or if the interpreter is busy queue it for later if flags allow.
        /// </summary>
        /// <param name="ch">char to type</param>
        /// <param name="doQueuing">true if you want to queue the char when the interpreter isn't at the prompt
        /// accepting input yet (i.e. the prev command is still running.) if false it will just do nothing in
        /// that case. (*EXCEPTION* The BREAK char will bypass the queue and happen right away.)</param>
        /// <param name="forceQueue">true if you want to queue the char even when the interpreter could have accepted
        /// it.  (never have this true if doQueueing is false.)</param>
        /// <returns>true if the char got either used or queued.  False means calling the method had no effect.
        /// This can only return false when doQueuing is false.</returns>
        bool SpecialKey(char key, bool doQueuing = true, bool forceQueue = true)
        {
            bool accepted = false;

            // Force async out of order processing right now for interrupt keys:
            bool rudeQueueSkipping = (key == (char)UnicodeCommand.BREAK);
            if (rudeQueueSkipping)
            {
                forceQueue = false;
                WipeAllTypeAhead();
            }

            if (shared != null)
            {
                bool wasUsed = false;

                if ((!forceQueue) &&
                    shared.Terminal != null && 
                    (shared.Terminal.IsWaitingForCommand() || rudeQueueSkipping))
                {
                    wasUsed = shared.Terminal.SpecialKey(key);
                    accepted = true;
                }
                else if (doQueuing)
                {
                    shared.Screen.CharInputQueue.Enqueue(key);
                    wasUsed = true;
                    accepted = true;
                }
                if (IsOpen && keyClickEnabled && wasUsed && doQueuing)
                    shared.SoundMaker.BeginFileSound("click");
            }
            return accepted;
        }

        /// <summary>
        /// When the input queue is not empty and the program or command is done such that
        /// the input cursor is now all the way back to the interpreter awaiting new input,
        /// send that queue out to the interpreter.  This allows you to type the next command
        /// blindly while waiting for the previous one to finish.
        /// This is how people used to terminals would expect things to work.
        /// </summary>
        void ProcessUnconsumedInput()
        {
            if (!shared.Processor.HasBooted)
            {
                return; // Fix race condition (Github issue #2925) where Update() calls this before FixedUpdate() has set up the CPU.
            }
            if (shared != null && shared.Terminal != null && shared.Terminal.IsWaitingForCommand())
            {
                Queue<char> q = shared.Screen.CharInputQueue;
                
                while (q.Count > 0 && shared.Terminal.IsWaitingForCommand())
                {
                    // Setting doQueuing to false here just as an
                    // additional safety measure.  Hypothetically it
                    // should never re-queue this input because we're
                    // in the condition where it will consume it right
                    // away.  But just in case there's some error in
                    // that logic, we want to avoid an infinite loop here
                    // (which could happen if we kept re-queuing the
                    // chars every time we processed one in this loop.)
                    char key = q.Peek();
                    bool wasAccepted = ProcessOneInputChar(key, null, false, false);
                    if (wasAccepted)
                        q.Dequeue();
                    else
                        break; // do NOT consume any more of the queue until it's accepting input again in a future pass.
                }
            }
        }

        /// <summary>
        /// Throws away everything in the typeahead buffer and any state vars from multi-char combo command
        /// sequences we might be in the middle of.  Used when you want to fully flush and start input over.
        /// </summary>
        void WipeAllTypeAhead()
        {
            shared.Screen.CharInputQueue.Clear();
            inputExpected = ExpectNextChar.NORMAL;
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
                GUI.Label(new Rect(15, 15, 450, 300),
                    "Error: Some or all kOS textures were not found.\n" +
                    "Please go to the following folder: \n\n" +
                    "<Your KSP Folder>\\GameData\\kOS\\GFX\\ \n\n" +
                    "and ensure that the dds texture files are there.\n" +
                    "Check the game log to see error messages \n" +
                    "starting with \"kOS:\" that talk about Texture files." +
                    "\n" +
                    "If you see this message, it probably means that\n" +
                    "kOS isn't installed correctly and you should try\n" +
                    "installing it again.");

                closeButtonRect = new Rect(WindowRect.width - 75, WindowRect.height - 30, 50, 25);
                if (GUI.Button(closeButtonRect, "Close"))
                {
                    Close();
                    Event.current.Use();
                }
                return;
            }

            if (shared == null || shared.Screen == null)
            {
                return;
            }
            IScreenBuffer screen = shared.Screen;
            
            GUI.Label(new Rect(15, 20, WindowRect.width-30, WindowRect.height-55), "", terminalImageStyle);
            GUI.color = color;

            if (telnets.Count > 0)
                DrawTelnetStatus();

            closeButtonRect = new Rect(WindowRect.width-75, WindowRect.height-30, 50, 25);
            Rect reverseButtonRect = new Rect(WindowRect.width-180, WindowRect.height-42, 100, 18);
            Rect visualBeepButtonRect = new Rect(WindowRect.width-180, WindowRect.height-22, 100, 18);
            Rect keyClickButtonRect = new Rect(10, WindowRect.height - 22, 85, 18);
            Rect rasterBarsButtonRect = new Rect(10, WindowRect.height - 42, 85, 18);
            Rect brightnessRect = new Rect(3, WindowRect.height - 100, 8, 50);
            Rect brightnessButtonRect = new Rect(1, WindowRect.height - 48, brightnessButtonImage.width, brightnessButtonImage.height);
            Rect fontHeightButtonRect = new Rect(30, WindowRect.height-33, fontHeightButtonImage.width, fontHeightButtonImage.height);
            Rect fontHeightLabelRect = new Rect(45, WindowRect.height-33, 20, 13);
            Rect fontHeightLessButtonRect = new Rect(75, WindowRect.height-37, 12, 13);
            Rect fontHeightMoreButtonRect = new Rect(100, WindowRect.height-37, 12, 13);

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
            screen.Brightness = (double) GUI.VerticalSlider(brightnessRect, (float)screen.Brightness, 1f, 0f);
            GUI.DrawTexture(brightnessButtonRect, brightnessButtonImage);

            int charHeight = screen.CharacterPixelHeight;
            int charWidth = screen.CharacterPixelWidth;

            // Note, pressing these buttons causes a change *next* OnGUI, not on this pass.
            // Changing it in the midst of this pass confuses the terminal to change
            // it's mind about how wide the window should be halfway through painting the
            // components that make it up:
            GUI.DrawTexture(fontHeightButtonRect, fontHeightButtonImage);
            GUI.Label(fontHeightLabelRect,charHeight+"px", customSkin.label);
            postponedCharPixelHeight = -1; // -1 means "font size buttons weren't clicked on this pass".
            if (GUI.Button(fontHeightLessButtonRect, "-", customSkin.button))
                postponedCharPixelHeight = Math.Max(TerminalStruct.MINCHARPIXELS, charHeight - 2);
            if (GUI.Button(fontHeightMoreButtonRect, "+", customSkin.button))
                postponedCharPixelHeight = Math.Min(TerminalStruct.MAXCHARPIXELS, charHeight + 2);

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
            terminalLetterSkin.label.normal.textColor = AdjustColor(reversingScreen ? bgColor : currentTextColor, screen.Brightness);            
            GUI.BeginGroup(new Rect(28, 38, screen.ColumnCount * charWidth + 2, screen.RowCount * charHeight + 4)); // +4 so descenders and underscores visible on bottom row.

            // When loading a quicksave, it is possible for the teminal window to update even though
            // mostRecentScreen is null.  If that's the case, just skip the screen update.
            if (mostRecentScreen != null)
            {
                List<IScreenBufferLine> buffer = mostRecentScreen.Buffer; // just to keep the name shorter below:

                // Sometimes the buffer is shorter than the terminal height if the resize JUST happened in the last Update():
                int rowsToPaint = Math.Min(screen.RowCount, buffer.Count);

                for (int row = 0; row < rowsToPaint; ++row)
                {
                    // At first the screen is filled with null chars.  So if you do something like
                    // PRINT "AAA" AT (4,0) you can get a row of the screen like so "\0\0\0\0AAA".
                    // When the font renderer prints null chars, they don't advance the cursor
                    // even in a monospoaced font (so "\0\0\0\0AAA" looks just like "AAA" instead
                    // of looking like "    AAA" when printed).  The reason for the "cooking" of
                    // the string below is to fix this problem:
                    string lineString = buffer[row].ToString().Replace( '\0', ' ');

                    GUI.Label(new Rect(0, (row * charHeight), WindowRect.width - 10, charHeight), lineString, terminalLetterSkin.label);
                }

                int cursorRow = screen.CursorRowShow;
                int cursorCol = screen.CursorColumnShow;

                bool drawCursorThisTime =
                    // Only if the cursor is in the "on" phase of its blink right now:
                    cursorBlinkTime < 0.5f &&
                    // Only if the cursor is within terminal bounds, to avoid throwing array bounds exceptions.
                    // (Cursor can be temporarily out of bounds if the up-arrow recalled a long cmdline, or if
                    // the terminal just got resized.)
                    cursorRow < screen.RowCount && cursorRow < buffer.Count &&  cursorCol < buffer[cursorRow].Length &&
                    // Only when the CPU has power
                    IsPowered &&
                    // Only when expecting input
                    ShowCursor;

                if (drawCursorThisTime)
                {
                    char ch = buffer[cursorRow][cursorCol];
                    DrawCursorAt(ch, cursorCol, cursorRow, reversingScreen,
                                         charWidth, charHeight, screen.Brightness);
                }
            }
            
            GUI.EndGroup();
            
            GUI.color = color; // screen size label was never supposed to be in green like the terminal is:            

            // Draw the rounded corner frame atop the chars field, so it covers the sqaure corners of the character zone
            // if they bleed over a bit.  Also, change which variant is used depending on focus:
            if (isLocked)
                GUI.Label(new Rect(15, 20, WindowRect.width-30, WindowRect.height-55), "", terminalFrameActiveStyle);
            else
                GUI.Label(new Rect(15, 20, WindowRect.width-30, WindowRect.height-55), "", terminalFrameStyle);

            GUI.Label(new Rect(WindowRect.width/2-40, WindowRect.height-12,100,10), screen.ColumnCount+"x"+screen.RowCount, customSkin.label);

            if (!fontGotResized)
                CheckResizeDrag(); // Has to occur before DragWindow or else DragWindow will consume the event and prevent drags from being seen by the resize icon.
            GUI.DragWindow();
            if (postponedCharPixelHeight >= 0)
                shared.Screen.CharacterPixelHeight = postponedCharPixelHeight; // next OnGUI will repaint in the new size.
        }
        
        protected Color AdjustColor(Color baseColor, double brightness)
        {
            Color newColor = baseColor;
            newColor.a = Convert.ToSingle(brightness); // represent dimness by making it fade into the backround.
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
        
        void DrawCursorAt(char ch, int x, int y, bool reversingScreen, int charWidth, int charHeight, double brightness)
        {
            // To emulate inverting the screen character, draw a solid block, then the reversed character atop it:
            // Solid Block:
            GUI.BeginGroup(new Rect((x * charWidth), (y * charHeight), charWidth, charHeight));
            GUI.color = AdjustColor(reversingScreen ? bgColor : currentTextColor, brightness);
            GUI.DrawTexture(new Rect(0, 0, charWidth, charHeight), Texture2D.whiteTexture, ScaleMode.StretchToFill, true);
            GUI.EndGroup();
            // Inverted Character atop it in the same position:
            GUI.BeginGroup(new Rect((x * charWidth), (y * charHeight), charWidth, charHeight));
            GUI.color = AdjustColor(reversingScreen ? currentTextColor : bgColor,
                2*brightness /*it seems to need slightly higher alpha values to show up atop the solid block*/ );
            terminalLetterSkin.label.normal.textColor = GUI.color;
            GUI.Label(new Rect(0, 0, charWidth, charHeight), ch.ToString(), terminalLetterSkin.label);
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
            
            shared.Screen.CharacterPixelWidth = 8; // will be overridden later when drawing the font.
            shared.Screen.CharacterPixelHeight = SafeHouse.Config.TerminalFontDefaultSize;
            shared.Screen.Brightness = SafeHouse.Config.TerminalBrightness;
            formerCharPixelWidth = shared.Screen.CharacterPixelWidth;
            formerCharPixelHeight = shared.Screen.CharacterPixelHeight;
            shared.Screen.SetSize(SafeHouse.Config.TerminalDefaultHeight, SafeHouse.Config.TerminalDefaultWidth);

            NotifyOfScreenResize(shared.Screen);
            shared.Screen.AddResizeNotifier(NotifyOfScreenResize);
            ChangeTitle(CalcualteTitle());

            soundMaker.AttachTo(shared); // Attach the soundMaker also
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
            prevTelnetScreens.Remove(server);
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
            GUISkin theSkin = Instantiate(HighLogic.Skin); // Use Instantiate to make a copy of the Skin Object

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
