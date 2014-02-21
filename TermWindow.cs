using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.IO;
using UnityEngine;

namespace kOS
{
    // Blockotronix 550 Computor Monitor
    public class TermWindow : MonoBehaviour
    {
        private static string root = KSPUtil.ApplicationRootPath.Replace("\\", "/");

        private Rect windowRect = new Rect(60, 50, 470, 395);
        private Texture2D fontImage = new Texture2D(0, 0, TextureFormat.DXT1, false);
        private Texture2D terminalImage = new Texture2D(0, 0, TextureFormat.DXT1, false);
        private bool isOpen = false;
        private bool showPilcrows = false;
        private CameraManager cameraManager;
        private CameraManager.CameraMode cameraModeWhenOpened;
        private bool isLocked = false;
        private float cursorBlinkTime;
        public static int CHARSIZE = 8;
        public static int CHARS_PER_ROW = 16;
        public static int XOFFSET = 15;
        public static int YOFFSET = 35;
        public static int XBGOFFSET = 15;
        public static int YBGOFFSET = 35;
        public static Color COLOR = new Color(1,1,1,1);
        public static Color COLOR_ALPHA = new Color(0.9f, 0.9f, 0.9f, 0.2f);
        public static Color TEXTCOLOR = new Color(0.45f, 0.92f, 0.23f, 0.9f);
        public static Color TEXTCOLOR_ALPHA = new Color(0.45f, 0.92f, 0.23f, 0.5f);
        public static Color TEXTCOLOROFF = new Color(0.8f, 0.8f, 0.8f, 0.7f);
        public static Color TEXTCOLOROFF_ALPHA = new Color(0.8f, 0.8f, 0.8f, 0.3f);
        public static Rect CLOSEBUTTON_RECT = new Rect(398, 359, 59, 30);

        public bool allTexturesFound = true;

        private SharedObjects _shared;
        private bool _isPowered = true;
        private bool _showCursor = true;

        public void Awake()
        {
            string pluginFolder = string.Empty;
            string gameDataFolder = Path.Combine(root, "GameData");
            
            if (Directory.Exists(Path.Combine(gameDataFolder, "kOS")))
            {
                pluginFolder = "GameData/kOS/";
            }
            else if (Directory.Exists(Path.Combine(gameDataFolder, "kRISC")))
            {
                pluginFolder = "GameData/kRISC/";
            }

            if (pluginFolder != string.Empty)
            {
                LoadTexture(pluginFolder + "GFX/font_sml.png", ref fontImage);
                LoadTexture(pluginFolder + "GFX/monitor_minimal.png", ref terminalImage);
            }
            else
            {
                allTexturesFound = false;
            }
        }

        public void LoadTexture(String relativePath, ref Texture2D targetTexture)
        {
            var imageLoader = new WWW("file://" + root + relativePath);
            imageLoader.LoadImageIntoTexture(targetTexture);

            if (imageLoader.isDone && imageLoader.size == 0) allTexturesFound = false;
        }

        public void Open()
        {
            isOpen = true;

            Lock();
        }

        public void Close()
        {
            // Diable GUI and release all locks
            isOpen = false;

            Unlock();
        }

        public void Toggle()
        {
            if (isOpen) Close();
            else Open();
        }

        private void Lock()
        {
            if (!isLocked)
            {
                isLocked = true;

                cameraManager = CameraManager.Instance;
                cameraModeWhenOpened = cameraManager.currentCameraMode;
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
            if (isOpen && isLocked) ProcessKeyStrokes();
            
            try
            {
                if (PauseMenu.isOpen || FlightResultsDialog.isDisplaying) return;
            }
            catch(NullReferenceException)
            {
            }

            if (!isOpen) return;
            
            GUI.skin = HighLogic.Skin;
            GUI.color = isLocked ? COLOR : COLOR_ALPHA;

            windowRect = GUI.Window(0, windowRect, TerminalGui, "");
        }

        void Update()
        {
            if (_shared == null || _shared.Vessel == null || _shared.Vessel.parts.Count == 0)
            {
                // Holding onto a vessel instance that no longer exists?
                Close();
            }

            if (!isOpen || !isLocked) return;

            cursorBlinkTime += Time.deltaTime;
            if (cursorBlinkTime > 1) cursorBlinkTime -= 1;
        }

        private List<KeyEvent> KeyStates = new List<KeyEvent>();

        public class KeyEvent
        {
            public KeyCode code;
            public float duration = 0;
        }
        
        void ProcessKeyStrokes()
        {
            if (Event.current.type == EventType.KeyDown)
            {
                if (Event.current.character != 0 && Event.current.character != 13 && Event.current.character != 10)
                {
                    Type(Event.current.character);
                }
                else if (Event.current.keyCode != KeyCode.None) 
                {
                    Keydown(Event.current.keyCode);
                }

                cursorBlinkTime = 0.0f;
            }
        }

        private void Keydown(KeyCode code)
        {
            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            bool control = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

            if (code == (KeyCode.Break)) { SpecialKey(kOSKeys.BREAK); return; }
            if (code == (KeyCode.C) && control) { SpecialKey(kOSKeys.BREAK); return; }

            if (code == (KeyCode.F1)) { SpecialKey(kOSKeys.F1); return; }
            if (code == (KeyCode.F2)) { SpecialKey(kOSKeys.F2); return; }
            if (code == (KeyCode.F3)) { SpecialKey(kOSKeys.F3); return; }
            if (code == (KeyCode.F4)) { SpecialKey(kOSKeys.F4); return; }
            if (code == (KeyCode.F5)) { SpecialKey(kOSKeys.F5); return; }
            if (code == (KeyCode.F6)) { SpecialKey(kOSKeys.F6); return; }
            if (code == (KeyCode.F7)) { SpecialKey(kOSKeys.F7); return; }
            if (code == (KeyCode.F8)) { SpecialKey(kOSKeys.F8); return; }
            if (code == (KeyCode.F9)) { SpecialKey(kOSKeys.F9); return; }
            if (code == (KeyCode.F10)) { SpecialKey(kOSKeys.F10); return; }
            if (code == (KeyCode.F11)) { SpecialKey(kOSKeys.F11); return; }
            if (code == (KeyCode.F12)) { SpecialKey(kOSKeys.F12); return; }

            if (code == (KeyCode.UpArrow)) { SpecialKey(kOSKeys.UP); return; }
            if (code == (KeyCode.DownArrow)) { SpecialKey(kOSKeys.DOWN); return; }
            if (code == (KeyCode.LeftArrow)) { SpecialKey(kOSKeys.LEFT); return; }
            if (code == (KeyCode.RightArrow)) { SpecialKey(kOSKeys.RIGHT); return; }
            if (code == (KeyCode.Home)) { SpecialKey(kOSKeys.HOME); return; }
            if (code == (KeyCode.End)) { SpecialKey(kOSKeys.END); return; }
            if (code == (KeyCode.Backspace)) { Type((char)8); return; }
            if (code == (KeyCode.Delete)) { SpecialKey(kOSKeys.DEL); return; }
            if (code == (KeyCode.PageUp)) { SpecialKey(kOSKeys.PGUP); return; }
            if (code == (KeyCode.PageDown)) { SpecialKey(kOSKeys.PGDN); return; }

            if (code == (KeyCode.Return) || code == (KeyCode.KeypadEnter)) { Type((char)13); return; }
        }

        void Type(char ch)
        {
            if (_shared != null && _shared.Interpreter != null)
            {
                _shared.Interpreter.Type(ch);
            }
        }

        void SpecialKey(kOSKeys key)
        {
            if (key == kOSKeys.BREAK)
            {
                _shared.Cpu.BreakExecution(true);
            }

            if (_shared != null && _shared.Interpreter != null)
            {
                _shared.Interpreter.SpecialKey(key);
            }
        }

        void TerminalGui(int windowID)
        {
            if (Input.GetMouseButtonDown(0))
            {
                var mousePos = new Vector2(Event.current.mousePosition.x, Event.current.mousePosition.y);

                if (CLOSEBUTTON_RECT.Contains(mousePos))
                {
                    Close();
                }
                else if (new Rect(0,0,terminalImage.width, terminalImage.height).Contains(mousePos))
                {
                    Lock();
                }
                else
                {
                    Unlock();
                }
            }

            if (!allTexturesFound)
            {
                GUI.Label(new Rect(15, 15, 450, 300), "Error: Some or all kOS textures were not found. Please " +
                           "go to the following folder: \n\n<Your KSP Folder>\\GameData\\kOS\\GFX\\ \n\nand ensure that the png texture files are there.");

                GUI.Label(CLOSEBUTTON_RECT, "Close");

                return;
            }

            if (_shared == null || _shared.Screen == null)
            {
                return;
            }

            GUI.color = isLocked ? COLOR : COLOR_ALPHA;
            GUI.DrawTexture(new Rect(10, 10, terminalImage.width, terminalImage.height), terminalImage);

            if (GUI.Button(new Rect(580, 10, 80, 30), "Close"))
            {
                isOpen = false;
                Close();
            }

            GUI.DragWindow(new Rect(0, 0, 10000, 500));

            Color textColor;
            if (_isPowered)
            {
                textColor = isLocked ? TEXTCOLOR : TEXTCOLOR_ALPHA;
            }
            else
            {
                textColor = isLocked ? TEXTCOLOROFF : TEXTCOLOROFF_ALPHA;
            }

            GUI.BeginGroup(new Rect(31, 38, 420, 340));

            ScreenBuffer screen = _shared.Screen;
            List<char[]> buffer = screen.Buffer;

            for (int row = 0; row < screen.RowCount; row++)
            {
                char[] lineBuffer = buffer[row];
                for (int column = 0; column < screen.ColumnCount; column++)
                {
                    char c = lineBuffer[column];
                    if (c != 0 && c != 9 && c != 32) ShowCharacterByAscii(c, column, row, textColor);
                }
            }

            bool blinkOn = cursorBlinkTime < 0.5f &&
                           screen.CursorColumn > -1 &&
                           screen.CursorRow < screen.RowCount &&
                           _isPowered &&
                           _showCursor;
            if (blinkOn)
            {
                ShowCharacterByAscii((char)1, screen.CursorColumn, screen.CursorRow, textColor);
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

        public void SetOptionPilcrows(bool val)
        {
            showPilcrows = val;
        }

        internal void AttachTo(SharedObjects shared)
        {
            _shared = shared;
            _shared.Window = this;
        }

        public void SetPowered(bool isPowered)
        {
            _isPowered = isPowered;
        }

        public void SetShowCursor(bool showCursor)
        {
            _showCursor = showCursor;
        }
    }
}
