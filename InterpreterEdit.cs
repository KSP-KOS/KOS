using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kOS
{
    public class InterpreterEdit : ExecutionContext
    {
        int BufferWidth { get { return buffer.GetLength(0); } }
        int BufferHeight { get { return buffer.GetLength(1); } }
        int CursorLine = 0;
        int CursorCol = 0;
        int ProgramSize = 0;
        String CurrentLine { get { return File[CursorLine]; } set { File[CursorLine] = value; } }
        int LocalCursorCol { get { return CursorCol < CurrentLine.Length ? CursorCol : CurrentLine.Length; } }
        int ScrollY = 0;
        int ScrollX = 0;
        int CursorX = 0;
        int CursorY = 0;

        private new char[,] buffer = new char[COLUMNS, ROWS];

        String StatusAnimString = "";
        float StatusAnimProg = 0;
        bool StatusAnimActive = false;

        File File;

        public override int GetCursorX()
        {
            return ChildContext != null ? ChildContext.GetCursorX() : CursorX;
        }

        public override int GetCursorY()
        {
            return ChildContext != null ? ChildContext.GetCursorX() : CursorY;
        }

        public InterpreterEdit(String fileName, ExecutionContext parent) : base(parent) 
        {
            File = SelectedVolume.GetByName(fileName);

            if (File == null)
            {
                File = new File(fileName);
                File.Add("");
            }

            CursorX = 0;
            CursorY = 2;
        }

        public override void Update(float time)
        {
            ClearScreen();

            Print(0, 0, File.Filename, 20);
            PrintBorder(1);
            PrintBorder(BufferHeight - 2);

            String LineColReadout = "Ln " + CursorLine + " Col " + LocalCursorCol;
            Print(BufferWidth - LineColReadout.Length, 0, LineColReadout);
             
            String ProgramSizeStr = ProgramSize.ToString() + "B";
            Print(BufferWidth - ProgramSizeStr.Length, BufferHeight - 1, ProgramSizeStr);

            if (LocalCursorCol > ScrollX + BufferWidth) ScrollX = LocalCursorCol - BufferWidth;
            else if (LocalCursorCol < ScrollX) ScrollX = 0;

            if (CursorLine > ScrollY + (BufferHeight - 5)) ScrollY = CursorLine - (BufferHeight - 5);
            else if (CursorLine < ScrollY) ScrollY = CursorLine;

            UpdateBuffer();

            CursorX = LocalCursorCol - ScrollX;
            CursorY = CursorLine - ScrollY + 2;

            // Status bar
            if (StatusAnimActive)
            {
                StatusAnimProg += time;
                if (StatusAnimProg > 3) StatusAnimActive = false;

                int charsToDisp = (int)(StatusAnimProg * 20);

                Print(0, BufferHeight - 1, StatusAnimString, charsToDisp);
            }
            else
            {
                String MenuOptions = "[F5:Save] [F10:Exit]";
                Print(0, BufferHeight - 1, MenuOptions);
            }
        }

        private void RecalcProgramSize()
        {
            ProgramSize = File.GetSize();
        }

        private void ClearScreen()
        {
            for (int y = 0; y < buffer.GetLength(1); y++)
            {
                for (int x = 0; x < buffer.GetLength(0); x++)
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
            for (int y = 2; y < BufferHeight - 2; y++)
            {
                int row = (y - 2) + ScrollY;

                if (row < File.Count())
                {
                    Print(0, y, File[row].Substring(ScrollX));
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
                    File[CursorLine] = File[CursorLine].Insert(LocalCursorCol, new String(ch, 1));
                    CursorCol = LocalCursorCol + 1;
                    break;
            }

            RecalcProgramSize();

            return true;
        }        

        public void ArrowKey(kOSKeys key)
        {
            if (key == kOSKeys.UP && CursorLine > 0) CursorLine--;
            if (key == kOSKeys.DOWN && CursorLine < File.Count - 1) CursorLine++;
            if (key == kOSKeys.RIGHT) 
            {
                if (CursorCol < CurrentLine.Length)
                {
                    CursorCol = LocalCursorCol + 1;
                }
                else
                {
                    if (CursorLine < File.Count - 1)
                    {
                        CursorLine ++;
                        CursorCol = 0;
                    }
                }
            }

            if (key == kOSKeys.LEFT)
            {
                if (CursorCol > 0)
                {
                    CursorCol = LocalCursorCol - 1;
                }
                else
                {
                    if (CursorLine > 0)
                    {
                        CursorLine--;
                        CursorCol = CurrentLine.Length;
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
                    CursorCol = 0;
                    return true;

                case kOSKeys.END:
                    CursorCol = CurrentLine.Length;
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

            File.Insert(CursorLine + 1, fragment);
            CursorCol = 0;
            CursorLine++;
        }

        private void Backspace()
        {
            if (LocalCursorCol > 0)
            {
                CursorCol = LocalCursorCol - 1;
                File[CursorLine] = File[CursorLine].Remove(LocalCursorCol, 1);
            }
            else
            {
                if (CursorLine > 0)
                {
                    String fragment = CurrentLine.Trim();
                    CursorLine--;
                    File.RemoveAt(CursorLine + 1);
                    CursorCol = CurrentLine.Length;
                    CurrentLine += fragment;
                }
            }
        }

        public  void Delete()
        {
            if (CursorCol < CurrentLine.Length)
            {
                File[CursorLine] = File[CursorLine].Remove(LocalCursorCol, 1);
            }
            else if (File.Count() > CursorLine + 1)
            {
                String otherLine = File[CursorLine + 1];
                File.RemoveAt(CursorLine + 1);
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
            if (SelectedVolume.SaveFile(File))
            {
                StatusAnimString = "SAVED.";
            }
            else
            {
                StatusAnimString = "CAN'T SAVE - DISK FULL.";
            }

            StatusAnimActive = true;
            StatusAnimProg = 0;
        }
    }
}
