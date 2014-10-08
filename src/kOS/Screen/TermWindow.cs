using System;
using System.Collections.Generic;
using UnityEngine;
using kOS.Safe.Screen;
using kOS.Safe.Utilities;
using kOS.Utilities;
using kOS.Persistence;

namespace kOS.Screen
{
    // Blockotronix 550 Computor Monitor
    public class TermWindow : KOSManagedWindow
    {
        private const int CHARSIZE = 8;
        private const int CHARS_PER_ROW = 16;
        private static readonly string root = KSPUtil.ApplicationRootPath.Replace("\\", "/");
        private static readonly Color color = new Color(1, 1, 1, 1);
        private static readonly Color colorAlpha = new Color(0.9f, 0.9f, 0.9f, 0.6f);
        private static readonly Color textColor = new Color(0.45f, 0.92f, 0.23f, 0.9f);
        private static readonly Color textColorAlpha = new Color(0.45f, 0.92f, 0.23f, 0.5f);
        private static readonly Color textColorOff = new Color(0.8f, 0.8f, 0.8f, 0.7f);
        private static readonly Color textColorOffAlpha = new Color(0.8f, 0.8f, 0.8f, 0.3f);
        private static Rect closeButtonRect = new Rect(398, 359, 59, 30);
        
        
        private bool consumeEvent;

        private KeyBinding rememberThrottleCutoffKey = null;
        private KeyBinding rememberThrottleFullKey = null;

        private bool allTexturesFound = true;
        private CameraManager cameraManager;
        private float cursorBlinkTime;
        private Texture2D fontImage = new Texture2D(0, 0, TextureFormat.DXT1, false);
        private bool isLocked;
        private Texture2D terminalImage = new Texture2D(0, 0, TextureFormat.DXT1, false);

        private SharedObjects shared;
        private bool showCursor = true;
        private KOSTextEditPopup popupEditor;

        public bool IsPowered { get; protected set; }


        public TermWindow()
        {
            IsPowered = true;
            windowRect = new Rect(60, 50, 470, 395);
        }

        public void Awake()
        {
            LoadTexture("GameData/kOS/GFX/font_sml.png", ref fontImage);
            LoadTexture("GameData/kOS/GFX/monitor_minimal.png", ref terminalImage);
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
            if (!isLocked)
            {
                isLocked = true;
                BringToFront();

                cameraManager = CameraManager.Instance;
                cameraManager.enabled = false;

                InputLockManager.SetControlLock("kOSTerminal");

                // Prevent editor keys from being pressed while typing
                EditorLogic editor = EditorLogic.fetch;
                if (editor != null && !EditorLogic.softLock) editor.Lock(true, true, true, "kOSTerminal");
                
                // This seems to be the only way to force KSP to let me lock out the "X" throttle
                // key.  It seems to entirely bypass the logic of every other keypress in the game,
                // so the only way to fix it is to use the keybindings system from the Setup screen.
                // When the terminal is focused, the THROTTLE_CUTOFF action gets unbound, and then
                // when its unfocused later, its put back the way it was:
                rememberThrottleCutoffKey = GameSettings.THROTTLE_CUTOFF;
                GameSettings.THROTTLE_CUTOFF = new KeyBinding(KeyCode.None);
                // TODO for KSP 0.25: when 0.25 is released, uncomment these lines, and
                // check what the name in the API actually is (THROTTLE_FULL is just my guess what
                // they might call it):
                //    rememberThrottleFullKey = GameSettings.THROTTLE_FULL;
                //    GameSettings.THROTTLE_FULL = new KeyBinding(KeyCode.None);
            }
        }

        private void Unlock()
        {
            if (isLocked)
            {
                isLocked = false;

                InputLockManager.RemoveControlLock("kOSTerminal");

                cameraManager.enabled = true;

                EditorLogic editor = EditorLogic.fetch;
                if (editor != null) editor.Unlock("kOSTerminal");

                // This seems to be the only way to force KSP to let me lock out the "X" throttle
                // key.  It seems to entirely bypass the logic of every other keypress in the game:
                if (rememberThrottleCutoffKey != null)
                    GameSettings.THROTTLE_CUTOFF = rememberThrottleCutoffKey;
                // TODO for KSP 0.25: when 0.25 is released, uncomment these lines, and
                // check what the name in the API actually is (THROTTLE_FULL is just my guess what
                // they might call it):
                //    if (rememberThrottleFullKey != null)
                //        GameSettings.THROTTLE_FULL = rememberThrottleFullKey;
            }
        }

        void OnGUI()
        {
            if (!IsOpen()) return;

            if (isLocked) ProcessKeyStrokes();
            
            try
            {
                if (PauseMenu.isOpen || FlightResultsDialog.isDisplaying) return;
            }
            catch(NullReferenceException)
            {
            }
            
            GUI.skin = HighLogic.Skin;
            GUI.color = isLocked ? color : colorAlpha;
            
            windowRect = GUI.Window(uniqueId, windowRect, TerminalGui, "");

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

            if (!IsOpen() ) return;
         
            UpdateLogic();

            if (!isLocked) return;

            cursorBlinkTime += Time.deltaTime;
            if (cursorBlinkTime > 1) cursorBlinkTime -= 1;
        }

        void ProcessKeyStrokes()
        {
            Event e = Event.current;
            if (e.type == EventType.KeyDown)
            {
                // Unity handles some keys in a particular way
                // e.g. Keypad7 is mapped to 0xffb7 instead of 0x37
                char c = (char)(e.character & 0x007f);

                // command sequences
                if (e.keyCode == KeyCode.C && e.control) // Ctrl+C
                {
                    SpecialKey(kOSKeys.BREAK);
                    consumeEvent = true;
                    return;
                }
                if (e.keyCode == KeyCode.X && e.control && e.shift) // Ctrl+Shift+X
                {
                    Close();
                    consumeEvent = true;
                    return;
                }

                if (0x20 <= c && c < 0x7f) // printable characters
                {
                    Type(c);
                    consumeEvent = true;
                }
                else if (e.keyCode != KeyCode.None) 
                {
                    Keydown(e.keyCode);
                    consumeEvent = true;
                }

                cursorBlinkTime = 0.0f;
            }
        }

        private void Keydown(KeyCode code)
        {
            switch (code)
            {
                case KeyCode.Break:      SpecialKey(kOSKeys.BREAK); break;
                case KeyCode.F1:         SpecialKey(kOSKeys.F1);    break;
                case KeyCode.F2:         SpecialKey(kOSKeys.F2);    break;
                case KeyCode.F3:         SpecialKey(kOSKeys.F3);    break;
                case KeyCode.F4:         SpecialKey(kOSKeys.F4);    break;
                case KeyCode.F5:         SpecialKey(kOSKeys.F5);    break;
                case KeyCode.F6:         SpecialKey(kOSKeys.F6);    break;
                case KeyCode.F7:         SpecialKey(kOSKeys.F7);    break;
                case KeyCode.F8:         SpecialKey(kOSKeys.F8);    break;
                case KeyCode.F9:         SpecialKey(kOSKeys.F9);    break;
                case KeyCode.F10:        SpecialKey(kOSKeys.F10);   break;
                case KeyCode.F11:        SpecialKey(kOSKeys.F11);   break;
                case KeyCode.F12:        SpecialKey(kOSKeys.F12);   break;
                case KeyCode.UpArrow:    SpecialKey(kOSKeys.UP);    break;
                case KeyCode.DownArrow:  SpecialKey(kOSKeys.DOWN);  break;
                case KeyCode.LeftArrow:  SpecialKey(kOSKeys.LEFT);  break;
                case KeyCode.RightArrow: SpecialKey(kOSKeys.RIGHT); break;
                case KeyCode.Home:       SpecialKey(kOSKeys.HOME);  break;
                case KeyCode.End:        SpecialKey(kOSKeys.END);   break;
                case KeyCode.Delete:     SpecialKey(kOSKeys.DEL);   break;
                case KeyCode.PageUp:     SpecialKey(kOSKeys.PGUP);  break;
                case KeyCode.PageDown:   SpecialKey(kOSKeys.PGDN);  break;

                case (KeyCode.Backspace):
                    Type((char)8);
                    break;

                case (KeyCode.KeypadEnter):
                case (KeyCode.Return):
                    Type('\r');
                    break;

                case (KeyCode.Tab):
                    Type('\t');
                    break;
            }
        }

        void Type(char ch)
        {
            if (shared != null && shared.Interpreter != null)
            {
                shared.Interpreter.Type(ch);
            }
        }

        void SpecialKey(kOSKeys key)
        {
            if (shared != null && shared.Interpreter != null)
            {
                shared.Interpreter.SpecialKey(key);
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

            GUI.color = isLocked ? color : colorAlpha;
            GUI.DrawTexture(new Rect(10, 10, terminalImage.width, terminalImage.height), terminalImage);

            if (GUI.Button(closeButtonRect, "Close"))
            {
                Close();
                Event.current.Use();
            }

            GUI.DragWindow(new Rect(0, 0, 10000, 500));

            Color currentTextColor;
            if (IsPowered)
            {
                currentTextColor = isLocked ? textColor : textColorAlpha;
            }
            else
            {
                currentTextColor = isLocked ? textColorOff : textColorOffAlpha;
            }

            GUI.BeginGroup(new Rect(31, 38, 420, 340));

            IScreenBuffer screen = shared.Screen;
            List<char[]> buffer = screen.GetBuffer();

            for (int row = 0; row < screen.RowCount; row++)
            {
                char[] lineBuffer = buffer[row];
                for (int column = 0; column < screen.ColumnCount; column++)
                {
                    char c = lineBuffer[column];
                    if (c != 0 && c != 9 && c != 32) ShowCharacterByAscii(c, column, row, currentTextColor);
                }
            }

            bool blinkOn = cursorBlinkTime < 0.5f &&
                           screen.CursorRowShow < screen.RowCount &&
                           IsPowered &&
                           showCursor;
            if (blinkOn)
            {
                ShowCharacterByAscii((char)1, screen.CursorColumnShow, screen.CursorRowShow, currentTextColor);
            }
            GUI.EndGroup();

        }

        void ShowCharacterByAscii(char ch, int x, int y, Color textColor)
        {
            int tx = ch % CHARS_PER_ROW;
            int ty = ch / CHARS_PER_ROW;

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
        }

        public void SetPowered(bool isPowered)
        {
            IsPowered = isPowered;
        }

        public void SetShowCursor(bool showCursor)
        {
            this.showCursor = showCursor;
        }
    }
}
