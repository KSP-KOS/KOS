using kOS.Safe.Screen;
using System;
using System.Collections.Generic;
using System.Linq;

namespace kOS.Safe.Test
{
    internal class Screen : IScreenBuffer
    {
        private List<String> output = new List<string>();

        public override string ToString()
        {
            return string.Join(Environment.NewLine, output);
        }

        public void ClearOutput()
        {
            output.Clear();
        }

        public void AssertOutput(params string[] expected)
        {
            bool matched = expected.Length == output.Count;
            if (matched)
            {
                for (int i = 0; i < expected.Length; i++)
                {
                    if (expected[i] != output[i])
                    {
                        matched = false;
                    }
                }
            }
            if (!matched)
            {
                string failText = "Output Mismatched\nExpected:\n";
                foreach (string line in expected)
                {
                    failText += line + "\n";
                }
                failText += "\nFound:\n";
                foreach (string line in output)
                {
                    failText += line + "\n";
                }
                throw new Exception(failText);
            }
        }

        public void Print(string textToPrint)
        {
            output.Add(textToPrint);
        }

        public void Print(string textToPrint, bool addNewLine)
        {
            Print(textToPrint);
        }

        public void PrintAt(string textToPrint, int row, int column)
        {
            Print(textToPrint);
        }

        public int AbsoluteCursorRow
        {
            get
            {
                return 0;
            }

            set
            {
            }
        }

        public int BeepsPending
        {
            get
            {
                return 0;
            }

            set
            {
            }
        }

        public double Brightness
        {
            get
            {
                return 1;
            }

            set
            {
            }
        }

        public int CharacterPixelHeight
        {
            get
            {
                return 0;
            }

            set
            {
            }
        }

        public int CharacterPixelWidth
        {
            get
            {
                return 0;
            }

            set
            {
            }
        }

        public Queue<char> CharInputQueue
        {
            get
            {
                return new Queue<char>();
            }
        }

        public int ColumnCount
        {
            get
            {
                return 0;
            }
        }

        public int CursorColumnShow
        {
            get
            {
                return 0;
            }
        }

        public int CursorRowShow
        {
            get
            {
                return 0;
            }
        }

        public bool ReverseScreen
        {
            get
            {
                return false;
            }

            set
            {
            }
        }

        public int RowCount
        {
            get
            {
                return 0;
            }
        }

        public int TopRow
        {
            get
            {
                return 0;
            }
        }

        public bool VisualBeep
        {
            get
            {
                return false;
            }

            set
            {
            }
        }

        public void AddResizeNotifier(ScreenBuffer.ResizeNotifier notifier)
        {
            throw new NotImplementedException();
        }

        public void AddSubBuffer(SubBuffer subBuffer)
        {
            throw new NotImplementedException();
        }

        public void ClearScreen()
        {
        }

        public string DebugDump()
        {
            return "";
        }

        public List<IScreenBufferLine> GetBuffer()
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

        public void RemoveAllResizeNotifiers()
        {
            throw new NotImplementedException();
        }

        public void RemoveResizeNotifier(ScreenBuffer.ResizeNotifier notifier)
        {
            throw new NotImplementedException();
        }

        public void RemoveSubBuffer(SubBuffer subBuffer)
        {
            throw new NotImplementedException();
        }

        public int ScrollVertical(int deltaRows)
        {
            throw new NotImplementedException();
        }

        public void SetSize(int rowCount, int columnCount)
        {
            throw new NotImplementedException();
        }
    }
}