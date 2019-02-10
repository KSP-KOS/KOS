using System.Collections.Generic;
using System.Text;
using System;
using kOS.Safe.UserIO;

namespace kOS.Safe.Screen
{
    public class TextEditor : ScreenBuffer
    {
        private int cursorColumnBuffer;
        private int cursorRowBuffer;
        
        public override int CursorColumnShow { get { return cursorRowBuffer == 0 ? base.CursorColumnShow + cursorColumnBuffer : cursorColumnBuffer; } }
        public override int CursorRowShow { get { return base.CursorRowShow + cursorRowBuffer; } }

        protected StringBuilder LineBuilder { get; set; }
        protected PrintingBuffer LineSubBuffer { get; set; }
        protected int LineCursorIndex { get; set; }


        public TextEditor()
        {
            LineBuilder = new StringBuilder();
            CreateSubBuffer();
        }

        private void CreateSubBuffer()
        {
            LineSubBuffer = new PrintingBuffer();
            LineSubBuffer.SetSize(1, ColumnCount);
            LineSubBuffer.Enabled = true;
            LineSubBuffer.AutoExtend = true;
            AddSubBuffer(LineSubBuffer);
        }

        // Hook into MoveCursor to dirty the lines we leave behind
        public override void MoveCursor(int row, int column)
        {
            if (row > AbsoluteCursorRow)
            {
                MarkRowsDirty(AbsoluteCursorRow, row - AbsoluteCursorRow);
            }
            else
            {
                MarkRowsDirty(row + LineSubBuffer.RowCount - 1, LineSubBuffer.RowCount + 2);
            }
            base.MoveCursor(row, column);
            UpdateLineSubBuffer();
        }

        public virtual void Type(char ch)
        {
            switch ((int)ch)
            {
                case 8:  // backspace
                    if (TryMoveCursor(-1)) RemoveChar();
                    break;
                case 13:  // enter
                    NewLine();
                    break;
                case 7:
                case (char)UnicodeCommand.BEEP:
                    ++base.BeepsPending;
                    break;
                default:
                    InsertChar(ch);
                    break;
            }
        }

        public virtual bool SpecialKey(char key)
        {
            bool gotUsed = true;
            switch (key)
            {
                case (char)UnicodeCommand.LEFTCURSORONE:
                    TryMoveCursor(-1);
                    break;
                case (char)UnicodeCommand.RIGHTCURSORONE:
                    TryMoveCursor(1);
                    break;
                case (char)0x0001: // control-A, same as home key
                case (char)UnicodeCommand.HOMECURSOR:
                    LineCursorIndex = 0;
                    UpdateSubBufferCursor();
                    break;
                case (char)0x0005: // control-E, same as end key
                case (char)UnicodeCommand.ENDCURSOR:
                    LineCursorIndex = LineBuilder.Length;
                    UpdateSubBufferCursor();
                    break;
                case (char)UnicodeCommand.DELETERIGHT:
                    RemoveChar();
                    break;
                case (char)UnicodeCommand.PAGEUPCURSOR:
                    ScrollVertical(-10);
                    break;
                case (char)UnicodeCommand.PAGEDOWNCURSOR:
                    ScrollVertical(10);
                    break;
                case (char)0x007:
                case (char)UnicodeCommand.BEEP:
                    ++base.BeepsPending;
                    break;
                default:
                    gotUsed = false;
                    break;
            }

            return gotUsed;
        }

        protected void InsertChar(char character)
        {
            LineBuilder.Insert(LineCursorIndex, character);
            LineCursorIndex++;
            UpdateLineSubBuffer();
        }

        protected void RemoveChar()
        {
            if (LineBuilder.Length > 0)
            {
                if (LineCursorIndex >= 0 && LineCursorIndex < LineBuilder.Length)
                {
                    LineBuilder.Remove(LineCursorIndex, 1);
                    MarkRowsDirty(LineSubBuffer.PositionRow, LineSubBuffer.RowCount); // just in case removing it reduces the number of subbuffer lines.
                    UpdateLineSubBuffer();
                }
            }
        }

        /// <summary> SplitIntoLines with our base cursor </summary>
        private List<string> SplitIntoLines(string text)
        {
            return SplitIntoLines(text, ColumnCount, base.CursorColumnShow);
        }
        
        protected void UpdateLineSubBuffer()
        {
            string commandText = LineBuilder.ToString();

            LineSubBuffer.Wipe();
            LineSubBuffer.PositionRow = AbsoluteCursorRow;
            LineSubBuffer.MoveCursor(0, base.CursorColumnShow);

            if (commandText.Length == 0)
            {
                cursorColumnBuffer = 0;
                cursorRowBuffer = 0;
            }
            else
            {
                LineSubBuffer.Print(commandText, false);

                UpdateSubBufferCursor();
            }
        }

        private void UpdateSubBufferCursor()
        {
            string commandText = LineBuilder.ToString();
            List<string> lines = SplitIntoLines(commandText);

            int lineIndex = 0;
            int lineCursorIndex = LineCursorIndex;

            while ((lineIndex < lines.Count)
                && (lineCursorIndex > lines[lineIndex].Length))
            {
                lineCursorIndex -= lines[lineIndex].Length;
                // if the line is shorter than the width of the screen then move
                // the cursor another position to compensate for the newline character
                if (lines[lineIndex].Length < ColumnCount &&
                    // we need to catch and not count the case where the line is actually full, because we didn't start on the first column
                    !(lineIndex == 0 && lines[lineIndex].Length == ColumnCount - base.CursorColumnShow))
                {
                    lineCursorIndex--;
                }
                lineIndex++;
            }

            cursorRowBuffer = lineIndex;
            cursorColumnBuffer = lineCursorIndex;

            KeepCursorInBounds();
        }
        
        protected void KeepCursorInBounds()
        {
            // Check to see if wrapping or scrolling needs to be done
            // because the cursor went off the screen, and if so, do it:
            if (CursorColumnShow == ColumnCount)
            {
                cursorColumnBuffer = 0;
                cursorRowBuffer++;
            }
            ScrollCursorVisible();
        }

        protected virtual void NewLine()
        {
            Print(LineBuilder.ToString());
            LineBuilder = new StringBuilder();
            LineCursorIndex = 0;
            UpdateLineSubBuffer();
        }

        protected virtual bool TryMoveCursor(int deltaPosition)
        {
            bool success = false;

            if (LineBuilder.Length > 0 && deltaPosition != 0)
            {
                int newCursorIndex = LineCursorIndex + deltaPosition;
                if (newCursorIndex >= 0 && newCursorIndex <= LineBuilder.Length)
                {
                    LineCursorIndex += deltaPosition;
                    UpdateSubBufferCursor();
                    success = true;
                }
            }

            return success;
        }

        public virtual void Reset()
        {
            LineBuilder = new StringBuilder();
            LineCursorIndex = 0;
            UpdateLineSubBuffer();
        }
    }
}
