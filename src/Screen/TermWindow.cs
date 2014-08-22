using System;
using System.Collections.Generic;
using UnityEngine;
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
            if (Event.current.type == EventType.KeyDown)
            {
                // Unity handles some keys in a particular way
                // e.g. Keypad7 is mapped to 0xffb7 instead of 0x37
                char c = (char)(Event.current.character & 0x007f);

                if (0x20 <= c && c < 0x7f) // printable characters
                {
                    Type(c);
                    consumeEvent = true;
                }
                else if (Event.current.keyCode != KeyCode.None) 
                {
                    Keydown(Event.current.keyCode);
                    consumeEvent = true;
                }

                cursorBlinkTime = 0.0f;
            }
        }

        private void Keydown(KeyCode code)
        {
            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            bool control = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

            if (code == (KeyCode.C) && control)
            {
                SpecialKey(kOSKeys.BREAK);
                return;
            }

            if (code == (KeyCode.X) && control && shift)
            {
                Close();
                return;
            }

            switch (code)
            {
                case (KeyCode.Break):
                    SpecialKey(kOSKeys.BREAK);
                    return;
                case (KeyCode.F1):
                    SpecialKey(kOSKeys.F1);
                    return;
                case (KeyCode.F2):
                    SpecialKey(kOSKeys.F2);
                    return;
                case (KeyCode.F3):
                    SpecialKey(kOSKeys.F3);
                    return;
                case (KeyCode.F4):
                    SpecialKey(kOSKeys.F4);
                    return;
                case (KeyCode.F5):
                    SpecialKey(kOSKeys.F5);
                    return;
                case (KeyCode.F6):
                    SpecialKey(kOSKeys.F6);
                    return;
                case (KeyCode.F7):
                    SpecialKey(kOSKeys.F7);
                    return;
                case (KeyCode.F8):
                    SpecialKey(kOSKeys.F8);
                    return;
                case (KeyCode.F9):
                    SpecialKey(kOSKeys.F9);
                    return;
                case (KeyCode.F10):
                    SpecialKey(kOSKeys.F10);
                    return;
                case (KeyCode.F11):
                    SpecialKey(kOSKeys.F11);
                    return;
                case (KeyCode.F12):
                    SpecialKey(kOSKeys.F12);
                    return;
                case (KeyCode.UpArrow):
                    SpecialKey(kOSKeys.UP);
                    return;
                case (KeyCode.DownArrow):
                    SpecialKey(kOSKeys.DOWN);
                    return;
                case (KeyCode.LeftArrow):
                    SpecialKey(kOSKeys.LEFT);
                    return;
                case (KeyCode.RightArrow):
                    SpecialKey(kOSKeys.RIGHT);
                    return;
                case (KeyCode.Home):
                    SpecialKey(kOSKeys.HOME);
                    return;
                case (KeyCode.End):
                    SpecialKey(kOSKeys.END);
                    return;
                case (KeyCode.Backspace):
                    Type((char)8);
                    return;
                case (KeyCode.Delete):
                    SpecialKey(kOSKeys.DEL);
                    return;
                case (KeyCode.KeypadEnter):
                case (KeyCode.Return):
                    Type((char)13);
                    return;
                case (KeyCode.PageUp):
                    SpecialKey(kOSKeys.PGUP);
                    return;
                case (KeyCode.PageDown):
                    SpecialKey(kOSKeys.PGDN);
                    return;
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

            ScreenBuffer screen = shared.Screen;
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
