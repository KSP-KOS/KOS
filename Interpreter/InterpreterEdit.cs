using System;
using System.Linq;
using kOS.Context;
using kOS.Persistance;
using kOS.Utilities;

namespace kOS.Interpreter
{
    public class InterpreterEdit : ExecutionContext
    {
        private int BufferWidth { get { return buffer.GetLength(0); } }
        private int BufferHeight { get { return buffer.GetLength(1); } }
        private int cursorLine;
        private int cursorCol;
        private int programSize;
        private String CurrentLine { get { return file[cursorLine]; } set { file[cursorLine] = value; } }
        private int LocalCursorCol { get { return cursorCol < CurrentLine.Length ? cursorCol : CurrentLine.Length; } }
        private int scrollY;
        private int scrollX;
        private int cursorX;
        private int cursorY;
        private readonly char[,] buffer = new char[COLUMNS, ROWS];
        private string statusAnimString = "";
        private float statusAnimProg;
        private bool statusAnimActive;
        private readonly File file;
        const string MENU_OPTIONS = "[F5:Save] [F10:Exit]";

        public override int GetCursorX()
        {
            return ChildContext != null ? ChildContext.GetCursorX() : cursorX;
        }

        public override int GetCursorY()
        {
            return ChildContext != null ? ChildContext.GetCursorX() : cursorY;
        }

        public InterpreterEdit(String fileName, ExecutionContext parent) : base(parent) 
        {
            cursorX = 0;
            file = SelectedVolume.GetByName(fileName) ?? new File(fileName) {""};

            cursorX = 0;
            cursorY = 2;

            RecalcProgramSize();
        }

        public override void Update(float time)
        {
            ClearScreen();

            Print(0, 0, file.Filename, 20);
            PrintBorder(1);
            PrintBorder(BufferHeight - 2);

            var lineColReadout = "Ln " + cursorLine + " Col " + LocalCursorCol;
            Print(BufferWidth - lineColReadout.Length, 0, lineColReadout);
             
            var programSizeStr = programSize + "B";
            Print(BufferWidth - programSizeStr.Length, BufferHeight - 1, programSizeStr);

            if (LocalCursorCol > scrollX + BufferWidth) scrollX = LocalCursorCol - BufferWidth;
            else if (LocalCursorCol < scrollX) scrollX = 0;

            if (cursorLine > scrollY + (BufferHeight - 5)) scrollY = cursorLine - (BufferHeight - 5);
            else if (cursorLine < scrollY) scrollY = cursorLine;

            UpdateBuffer();

            cursorX = LocalCursorCol - scrollX;
            cursorY = cursorLine - scrollY + 2;

            // Status bar
            if (statusAnimActive)
            {
                statusAnimProg += time;
                if (statusAnimProg > 3) statusAnimActive = false;

                var charsToDisp = (int)(statusAnimProg * 20);

                Print(0, BufferHeight - 1, statusAnimString, charsToDisp);
            }
            else
            {
                Print(0, BufferHeight - 1, MENU_OPTIONS);
            }
        }

        private void RecalcProgramSize()
        {
            programSize = file.GetSize();
        }

        private void ClearScreen()
        {
            for (var y = 0; y < buffer.GetLength(1); y++)
            {
                for (var x = 0; x < buffer.GetLength(0); x++)
                {
                    buffer[x, y] = (char)0;
                }
            }
        }

        public override char[,] GetBuffer()
        {
            return buffer;
        }

        private void UpdateBuffer()
        {
            for (var y = 2; y < BufferHeight - 2; y++)
            {
                var row = (y - 2) + scrollY;

                if (row >= file.Count()) continue;
                if (scrollX < file[row].Length)
                {
                    Print(0, y, file[row].Substring(scrollX));
                }
            }
        }

        public override bool Type(char ch)
        {
            switch (ch)
            {
                case (char)8:
                    Backspace();
                    break;

                case (char)13:
                    Enter();
                    break;

                default:
                    file[cursorLine] = file[cursorLine].Insert(LocalCursorCol, new String(ch, 1));
                    cursorCol = LocalCursorCol + 1;
                    break;
            }

            RecalcProgramSize();

            return true;
        }        

        public void ArrowKey(kOSKeys key)
        {
            if (key == kOSKeys.UP && cursorLine > 0) cursorLine--;
            if (key == kOSKeys.DOWN && cursorLine < file.Count - 1) cursorLine++;
            if (key == kOSKeys.RIGHT) 
            {
                if (cursorCol < CurrentLine.Length)
                {
                    cursorCol = LocalCursorCol + 1;
                }
                else
                {
                    if (cursorLine < file.Count - 1)
                    {
                        cursorLine ++;
                        cursorCol = 0;
                    }
                }
            }

            if (key == kOSKeys.LEFT)
            {
                if (cursorCol > 0)
                {
                    cursorCol = LocalCursorCol - 1;
                }
                else
                {
                    if (cursorLine > 0)
                    {
                        cursorLine--;
                        cursorCol = CurrentLine.Length;
                    }
                }
            }
        }

        public override bool SpecialKey(kOSKeys key)
        {
            switch (key)
            {
                case kOSKeys.F5:
                    Save();
                    return true;

                case kOSKeys.F10:
                    Exit();
                    return true;

                case kOSKeys.HOME:
                    cursorCol = 0;
                    return true;

                case kOSKeys.END:
                    cursorCol = CurrentLine.Length;
                    return true;
                    
                case kOSKeys.DEL:
                    Delete();
                    return true;

                case kOSKeys.UP:
                case kOSKeys.LEFT:
                case kOSKeys.RIGHT:
                case kOSKeys.DOWN:
                    ArrowKey(key);
                    return true;
            }

            return false;
        }
        
        private void Enter()
        {
            String fragment = CurrentLine.Substring(LocalCursorCol);
            CurrentLine = CurrentLine.Substring(0, LocalCursorCol);

            file.Insert(cursorLine + 1, fragment);
            cursorCol = 0;
            cursorLine++;
        }

        private void Backspace()
        {
            if (LocalCursorCol > 0)
            {
                cursorCol = LocalCursorCol - 1;
                file[cursorLine] = file[cursorLine].Remove(LocalCursorCol, 1);
            }
            else
            {
                if (cursorLine > 0)
                {
                    String fragment = CurrentLine.Trim();
                    cursorLine--;
                    file.RemoveAt(cursorLine + 1);
                    cursorCol = CurrentLine.Length;
                    CurrentLine += fragment;
                }
            }
        }

        public  void Delete()
        {
            if (cursorCol < CurrentLine.Length)
            {
                file[cursorLine] = file[cursorLine].Remove(LocalCursorCol, 1);
            }
            else if (file.Count() > cursorLine + 1)
            {
                String otherLine = file[cursorLine + 1];
                file.RemoveAt(cursorLine + 1);
                CurrentLine += otherLine;
            }
        }

        public void Print(int sx, int sy, String value)
        {
            Print(sx, sy, value, value.Length);
        }

        public void Print(int sx, int sy, String value, int max)
        {
            char[] chars = value.ToCharArray();
            int i = 0;
            for (int x = sx; (x < BufferWidth && i < chars.Count() && i < max); x++)
            {
                buffer[x, sy] = chars[i];
                i++;
            }
        }

        public void PrintBorder(int y)
        {
            for (int x = 0; x < BufferWidth; x++)
            {
                buffer[x, y] = '-';
            }
        }

        private void Exit()
        {
            State = ExecutionState.DONE;
        }

        private void Save()
        {
            statusAnimString = SelectedVolume.SaveFile(file) ? "SAVED." : "CAN'T SAVE - DISK FULL.";

            statusAnimActive = true;
            statusAnimProg = 0;
        }
    }
}
