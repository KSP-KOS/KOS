using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using kOS.Safe.Screen;

namespace kOS.CommandLine.Screen
{
    class Interpreter : IInterpreter
    {
        public void Type(char ch)
        {
            throw new NotImplementedException();
        }

        public bool SpecialKey(char key)
        {
            throw new NotImplementedException();
        }

        public string GetCommandHistoryAbsolute(int absoluteIndex)
        {
            throw new NotImplementedException();
        }

        public void SetInputLock(bool isLocked)
        {
            throw new NotImplementedException();
        }

        public bool IsAtStartOfCommand()
        {
            throw new NotImplementedException();
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }

        public int CharacterPixelWidth
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public int CharacterPixelHeight
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public float Brightness
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public int CursorRowShow
        {
            get { throw new NotImplementedException(); }
        }

        public int CursorColumnShow
        {
            get { throw new NotImplementedException(); }
        }

        public int RowCount
        {
            get { throw new NotImplementedException(); }
        }

        public int ColumnCount
        {
            get { throw new NotImplementedException(); }
        }

        public int AbsoluteCursorRow
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public int BeepsPending
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public bool ReverseScreen
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public bool VisualBeep
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public int TopRow
        {
            get { throw new NotImplementedException(); }
        }

        public void SetSize(int rowCount, int columnCount)
        {
            throw new NotImplementedException();
        }

        public int ScrollVertical(int deltaRows)
        {
            throw new NotImplementedException();
        }

        public void MoveCursor(int row, int column)
        {
            throw new NotImplementedException();
        }

        public void MoveToNextLine()
        {
            throw new NotImplementedException();
        }

        public void PrintAt(string textToPrint, int row, int column)
        {
            throw new NotImplementedException();
        }

        public void Print(string textToPrint)
        {
            throw new NotImplementedException();
        }

        public void Print(string textToPrint, bool addNewLine)
        {
            throw new NotImplementedException();
        }

        public void ClearScreen()
        {
            throw new NotImplementedException();
        }

        public void AddSubBuffer(SubBuffer subBuffer)
        {
            throw new NotImplementedException();
        }

        public void RemoveSubBuffer(SubBuffer subBuffer)
        {
            throw new NotImplementedException();
        }

        public List<IScreenBufferLine> GetBuffer()
        {
            throw new NotImplementedException();
        }

        public void AddResizeNotifier(ScreenBuffer.ResizeNotifier notifier)
        {
            throw new NotImplementedException();
        }

        public void RemoveResizeNotifier(ScreenBuffer.ResizeNotifier notifier)
        {
            throw new NotImplementedException();
        }

        public string DebugDump()
        {
            throw new NotImplementedException();
        }
    }
}
