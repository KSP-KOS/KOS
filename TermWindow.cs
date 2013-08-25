using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace kOS
{
    // Blockotronix 550 Computor Monitor
    public class TermWindow : MonoBehaviour
    {
        private static string root = KSPUtil.ApplicationRootPath.Replace("\\", "/");

        private Rect windowRect = new Rect(60, 50, 470, 395);
        private Texture2D fontImage = new Texture2D(0, 0);
        private Texture2D terminalImage = new Texture2D(0, 0);
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
        public static Rect CLOSEBUTTON_RECT = new Rect(398, 359, 59, 30);

        public bool allTexturesFound = true;

        public enum SkinType
        {
            SMALL, MINIMAL
        }
        private SkinType skinType = SkinType.MINIMAL;

        public Core Core;
        public CPU Cpu;

        public void Awake()
        {
            LoadTexture("Plugins/PluginData/kos/gfx/font_sml.png", ref fontImage);
            LoadTexture("Plugins/PluginData/kos/gfx/monitor_minimal.png", ref terminalImage);
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

                /*
                foreach (ControlTypes c in Enum.GetValues(typeof(ControlTypes)))
                {
                    
                    InputLockManager.SetControlLock(c, "kOSTerminal-" + c.ToString());
                }*/

                InputLockManager.SetControlLock("kOSTerminal");

                // Prevent editor keys from being pressed while typing
                EditorLogic editor = EditorLogic.fetch;
                if (editor != null && !EditorLogic.softLock) editor.Lock(true, true, true);
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
                if (editor != null) editor.Unlock();
            }
        }

        void OnGUI()
        {
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
            if (!isOpen || !isLocked) return;

            ProcessKeyStrokes();

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
            foreach (KeyEvent e in new List<KeyEvent>(KeyStates))
            {
                if (!Input.GetKey(e.code))
                {
                    KeyStates.Remove(e);
                }
                else
                {
                    e.duration += Time.deltaTime;

                    if (e.duration > 0.35f)
                    {
                        e.duration = 0.30f;
                        Keydown(e.code);
                    }
                }
            }

            foreach (KeyCode code in Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(code))
                {
                    KeyEvent e = new KeyEvent();
                    e.code = code;
                    KeyStates.Add(e);

                    Keydown(code);
                }
            }
        }

        private void Keydown(KeyCode code)
        {
            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            bool control = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

            if (code == (KeyCode.Break)) { SpecialKey(kOSKeys.BREAK); return; }
            if (code == (KeyCode.C) && control) { SpecialKey(kOSKeys.BREAK); return; }

            if (code == KeyCode.A) { Type(shift ? 'A' : 'a'); return; }
            if (code == KeyCode.B) { Type(shift ? 'B' : 'b'); return; }
            if (code == KeyCode.C) { Type(shift ? 'C' : 'c'); return; }
            if (code == KeyCode.D) { Type(shift ? 'D' : 'd'); return; }
            if (code == KeyCode.E) { Type(shift ? 'E' : 'e'); return; }
            if (code == KeyCode.F) { Type(shift ? 'F' : 'f'); return; }
            if (code == KeyCode.G) { Type(shift ? 'G' : 'g'); return; }
            if (code == KeyCode.H) { Type(shift ? 'H' : 'h'); return; }
            if (code == KeyCode.I) { Type(shift ? 'I' : 'i'); return; }
            if (code == KeyCode.J) { Type(shift ? 'J' : 'j'); return; }
            if (code == KeyCode.K) { Type(shift ? 'K' : 'k'); return; }
            if (code == KeyCode.L) { Type(shift ? 'L' : 'l'); return; }
            if (code == KeyCode.M) { Type(shift ? 'M' : 'm'); return; }
            if (code == KeyCode.N) { Type(shift ? 'N' : 'n'); return; }
            if (code == KeyCode.O) { Type(shift ? 'O' : 'o'); return; }
            if (code == KeyCode.P) { Type(shift ? 'P' : 'p'); return; }
            if (code == KeyCode.Q) { Type(shift ? 'Q' : 'q'); return; }
            if (code == KeyCode.R) { Type(shift ? 'R' : 'r'); return; }
            if (code == KeyCode.S) { Type(shift ? 'S' : 's'); return; }
            if (code == KeyCode.T) { Type(shift ? 'T' : 't'); return; }
            if (code == KeyCode.U) { Type(shift ? 'U' : 'u'); return; }
            if (code == KeyCode.V) { Type(shift ? 'V' : 'v'); return; }
            if (code == KeyCode.W) { Type(shift ? 'W' : 'w'); return; }
            if (code == KeyCode.X) { Type(shift ? 'X' : 'x'); return; }
            if (code == KeyCode.Y) { Type(shift ? 'Y' : 'y'); return; }
            if (code == KeyCode.Z) { Type(shift ? 'Z' : 'z'); return; }

            if (code == (KeyCode.Alpha0) || code == (KeyCode.Keypad0)) { Type(shift ? ')' : '0'); return; }
            if (code == (KeyCode.Alpha1) || code == (KeyCode.Keypad1)) { Type(shift ? '!' : '1'); return; }
            if (code == (KeyCode.Alpha2) || code == (KeyCode.Keypad2)) { Type(shift ? '@' : '2'); return; }
            if (code == (KeyCode.Alpha3) || code == (KeyCode.Keypad3)) { Type(shift ? '#' : '3'); return; }
            if (code == (KeyCode.Alpha4) || code == (KeyCode.Keypad4)) { Type(shift ? '$' : '4'); return; }
            if (code == (KeyCode.Alpha5) || code == (KeyCode.Keypad5)) { Type(shift ? '%' : '5'); return; }
            if (code == (KeyCode.Alpha6) || code == (KeyCode.Keypad6)) { Type(shift ? '^' : '6'); return; }
            if (code == (KeyCode.Alpha7) || code == (KeyCode.Keypad7)) { Type(shift ? '&' : '7'); return; }
            if (code == (KeyCode.Alpha8) || code == (KeyCode.Keypad8)) { Type(shift ? '*' : '8'); return; }
            if (code == (KeyCode.Alpha9) || code == (KeyCode.Keypad9)) { Type(shift ? '(' : '9'); return; }

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

            if (code == (KeyCode.LeftBracket)) { Type(shift ? '{' : '['); return; }
            if (code == (KeyCode.RightBracket)) { Type(shift ? '}' : ']'); return; }

            if ((code == (KeyCode.Minus) && !shift) || code == (KeyCode.KeypadMinus)) { Type('-'); return; }
            if ((code == (KeyCode.Equals) && shift) || code == (KeyCode.KeypadPlus)) { Type('+'); return; }
            if (code == (KeyCode.KeypadMultiply)) { Type('*'); return; }
            if (code == (KeyCode.Slash) || code == (KeyCode.KeypadDivide)) { Type('/'); return; }
            if ((code == (KeyCode.Equals) && !shift)) { Type('='); return; }
            if (code == (KeyCode.Semicolon)) { Type(shift ? ':' : ';'); return; }

            if (code == (KeyCode.UpArrow)) { SpecialKey(kOSKeys.UP); return; }
            if (code == (KeyCode.DownArrow)) { SpecialKey(kOSKeys.DOWN); return; }
            if (code == (KeyCode.LeftArrow)) { SpecialKey(kOSKeys.LEFT); return; }
            if (code == (KeyCode.RightArrow)) { SpecialKey(kOSKeys.RIGHT); return; }
            if (code == (KeyCode.Home)) { SpecialKey(kOSKeys.HOME); return; }
            if (code == (KeyCode.End)) { SpecialKey(kOSKeys.END); return; }
            if (code == (KeyCode.Backspace)) { Type((char)8); return; }
            if (code == (KeyCode.Delete)) { SpecialKey(kOSKeys.DEL); return; }

            if (code == (KeyCode.Quote)) { Type(shift ? '\"' : '\''); return; }
            if (code == (KeyCode.Comma)) { Type(shift ? '<' : ','); return; }
            if (code == (KeyCode.Period)) { Type(shift ? '>' : '.'); return; }
            if (code == (KeyCode.Return) || code == (KeyCode.KeypadEnter)) { Type((char)13); return; }
            if (code == (KeyCode.Space)) { Type(' '); return; }
        }
        
        public void ClearScreen()
        {
            
        }
        
        void Type(char ch)
        {
            if (Cpu != null) Cpu.KeyInput(ch);
        }

        void SpecialKey(kOSKeys key)
        {
            if (Cpu != null) Cpu.SpecialKey(key);
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
                           "go to the following folder: \n\n<Your KSP Folder>\\Plugins\\PluginData\\kOS\\gfx \n\nand ensure that the png texture files are there.");

                GUI.Label(CLOSEBUTTON_RECT, "Close");

                return;
            }

            if (Cpu == null) return;

            GUI.color = isLocked ? COLOR : COLOR_ALPHA;
            GUI.DrawTexture(new Rect(10, 10, terminalImage.width, terminalImage.height), terminalImage);

            if (GUI.Button(new Rect(580, 10, 80, 30), "Close"))
            {
                isOpen = false;
                Close();
            }

            GUI.DragWindow(new Rect(0, 0, 10000, 500));

            if (Cpu != null && Cpu.Mode == CPU.Modes.READY && Cpu.IsAlive())
            {
                Color textColor = isLocked ? TEXTCOLOR : TEXTCOLOR_ALPHA;

                GUI.BeginGroup(new Rect(31, 38, 420, 340));

                if (Cpu != null)
                {
                    char[,] buffer = Cpu.GetBuffer();

                    for (var x = 0; x < buffer.GetLength(0); x++)
                        for (var y = 0; y < buffer.GetLength(1); y++)
                        {
                            ShowCharacterByAscii(buffer[x, y], x, y, textColor);
                        }

                    bool blinkOn = cursorBlinkTime < 0.5f;
                    if (blinkOn && Cpu.GetCursorX() > -1)
                    {
                        ShowCharacterByAscii((char)1, Cpu.GetCursorX(), Cpu.GetCursorY(), textColor);
                    }
                }

                GUI.EndGroup();
            }
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

        internal void AttachTo(CPU cpu)
        {
            this.Cpu = cpu;
        }

        internal void PrintLine(string line)
        {
            //if (Cpu != null) Cpu.PrintLine(line);
        }
    }
}
