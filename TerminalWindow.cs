using System;
using UnityEngine;
using kOS.Context;
using kOS.Utilities;

namespace kOS
{
    // Blockotronix 550 Computor Monitor
    public class TerminalWindow : MonoBehaviour
    {
        private const int CHARSIZE = 8;
        private const int CHARS_PER_ROW = 16;
        private static readonly string root = KSPUtil.ApplicationRootPath.Replace("\\", "/");
        private static readonly Color color = new Color(1, 1, 1, 1);
        private static readonly Color colorAlpha = new Color(0.9f, 0.9f, 0.9f, 0.2f);
        private static readonly Color textcolor = new Color(0.45f, 0.92f, 0.23f, 0.9f);
        private static readonly Color textcolorAlpha = new Color(0.45f, 0.92f, 0.23f, 0.5f);
        private static Rect closebuttonRect = new Rect(398, 359, 59, 30);

        public Core Core;
        public ICPU Cpu;
        private bool allTexturesFound = true;
        private CameraManager cameraManager;
        private float cursorBlinkTime;
        private Texture2D fontImage = new Texture2D(0, 0, TextureFormat.DXT1, false);
        private bool isLocked;
        private bool isOpen;
        private Texture2D terminalImage = new Texture2D(0, 0, TextureFormat.DXT1, false);
        private Rect windowRect = new Rect(60, 50, 470, 395);

        public void Awake()
        {
            LoadTexture("GameData/kOS/GFX/font_sml.png", ref fontImage);
            LoadTexture("GameData/kOS/GFX/monitor_minimal.png", ref terminalImage);
        }

        public void LoadTexture(string relativePath, ref Texture2D targetTexture)
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
            if (isLocked) return;
            isLocked = true;

            cameraManager = CameraManager.Instance;
            cameraManager.enabled = false;

            InputLockManager.SetControlLock("kOSTerminal");

            // Prevent editor keys from being pressed while typing
            var editor = EditorLogic.fetch;
            if (editor != null && !EditorLogic.softLock) editor.Lock(true, true, true, "kOSTerminal");
        }

        private void Unlock()
        {
            if (!isLocked) return;
            isLocked = false;

            InputLockManager.RemoveControlLock("kOSTerminal");

            cameraManager.enabled = true;

            var editor = EditorLogic.fetch;
            if (editor != null) editor.Unlock("kOSTerminal");
        }

        private void OnGUI()
        {
            if (isOpen && isLocked) ProcessKeyStrokes();

            try
            {
                if (PauseMenu.isOpen || FlightResultsDialog.isDisplaying) return;
            }
            catch (NullReferenceException)
            {
            }

            if (!isOpen) return;

            GUI.skin = HighLogic.Skin;
            GUI.color = isLocked ? color : colorAlpha;

            windowRect = GUI.Window(0, windowRect, TerminalGui, "");
        }

        private void Update()
        {
            if (Cpu == null || Cpu.Vessel == null || Cpu.Vessel.parts.Count == 0)
            {
                // Holding onto a vessel instance that no longer exists?
                Close();
            }

            if (!isOpen || !isLocked) return;

            cursorBlinkTime += Time.deltaTime;
            if (cursorBlinkTime > 1) cursorBlinkTime -= 1;
        }

        private void ProcessKeyStrokes()
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
            var shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            var control = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

            if (code == (KeyCode.C) && control)
            {
                SpecialKey(kOSKeys.BREAK);
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
                    Type((char) 8);
                    return;
                case (KeyCode.Delete):
                    SpecialKey(kOSKeys.DEL);
                    return;
                case (KeyCode.KeypadEnter):
                case (KeyCode.Return):
                    Type((char) 13);
                    return;
            }
        }

        public void ClearScreen()
        {
        }

        private void Type(char ch)
        {
            if (Cpu != null) Cpu.KeyInput(ch);
        }

        private void SpecialKey(kOSKeys key)
        {
            if (Cpu != null) Cpu.SpecialKey(key);
        }

        private void TerminalGui(int windowID)
        {
            if (Input.GetMouseButtonDown(0))
            {
                var mousePos = new Vector2(Event.current.mousePosition.x, Event.current.mousePosition.y);

                if (closebuttonRect.Contains(mousePos))
                {
                    Close();
                }
                else if (new Rect(0, 0, terminalImage.width, terminalImage.height).Contains(mousePos))
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

                GUI.Label(closebuttonRect, "Close");

                return;
            }

            if (Cpu == null) return;

            GUI.color = isLocked ? color : colorAlpha;
            GUI.DrawTexture(new Rect(10, 10, terminalImage.width, terminalImage.height), terminalImage);

            if (GUI.Button(new Rect(580, 10, 80, 30), "Close"))
            {
                isOpen = false;
                Close();
            }

            GUI.DragWindow(new Rect(0, 0, 10000, 500));

            if (Cpu != null && Cpu.Mode == CPUMode.READY && Cpu.IsAlive())
            {
                var textColor = isLocked ? textcolor : textcolorAlpha;

                GUI.BeginGroup(new Rect(31, 38, 420, 340));

                if (Cpu != null)
                {
                    var buffer = Cpu.GetBuffer();

                    for (var x = 0; x < buffer.GetLength(0); x++)
                        for (var y = 0; y < buffer.GetLength(1); y++)
                        {
                            var c = buffer[x, y];

                            if (c != 0 && c != 9 && c != 32) ShowCharacterByAscii(buffer[x, y], x, y, textColor);
                        }

                    var blinkOn = cursorBlinkTime < 0.5f;
                    if (blinkOn && Cpu.GetCursorX() > -1)
                    {
                        ShowCharacterByAscii((char) 1, Cpu.GetCursorX(), Cpu.GetCursorY(), textColor);
                    }
                }

                GUI.EndGroup();
            }
        }

        private void ShowCharacterByAscii(char ch, int x, int y, Color textColor)
        {
            var tx = ch%CHARS_PER_ROW;
            var ty = ch/CHARS_PER_ROW;

            ShowCharacterByXY(x, y, tx, ty, textColor);
        }

        private void ShowCharacterByXY(int x, int y, int tx, int ty, Color textColor)
        {
            GUI.BeginGroup(new Rect((x*CHARSIZE), (y*CHARSIZE), CHARSIZE, CHARSIZE));
            GUI.color = textColor;
            GUI.DrawTexture(new Rect(tx*-CHARSIZE, ty*-CHARSIZE, fontImage.width, fontImage.height), fontImage);
            GUI.EndGroup();
        }

        public void SetOptionPilcrows(bool val)
        {
        }

        internal void AttachTo(ICPU cpu)
        {
            Cpu = cpu;
        }

        internal void PrintLine(string line)
        {
            //if (Cpu != null) Cpu.PrintLine(line);
        }

        public class KeyEvent
        {
            public KeyCode Code;
            public float Duration = 0;
        }
    }
}