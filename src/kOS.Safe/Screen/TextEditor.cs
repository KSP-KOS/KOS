using System.Collections.Generic;
using System.Text;
using kOS.Safe.Utilities;
using kOS.Safe.UserIO;

namespace kOS.Safe.Screen
{
    public class TextEditor : ScreenBuffer
    {
        private int savedCursorRow;
        private int savedCursorColumn;
        private int cursorColumnBuffer;
        private int cursorRowBuffer;
        
        public override int CursorColumnShow { get { return cursorColumnBuffer; } }
        public override int CursorRowShow { get { return CursorRow + cursorRowBuffer; } }

        protected StringBuilder LineBuilder { get; set; }
        protected SubBuffer LineSubBuffer { get; set; }
        protected int LineCursorIndex { get; set; }


        public TextEditor()
        {
            LineBuilder = new StringBuilder();
            CreateSubBuffer();
        }

        private void CreateSubBuffer()
        {
            LineSubBuffer = new SubBuffer();
            LineSubBuffer.SetSize(1, ColumnCount);
            LineSubBuffer.Enabled = true;
            AddSubBuffer(LineSubBuffer);
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
                default:
                    InsertChar(ch);
                    break;
            }
        }

        public virtual void SpecialKey(char key)
        {
            switch (key)
            {
                case (char)UnicodeCommand.LEFTCURSORONE:
                    TryMoveCursor(-1);
                    break;
                case (char)UnicodeCommand.RIGHTCURSORONE:
                    TryMoveCursor(1);
                    break;
                case (char)UnicodeCommand.HOMECURSOR:
                    LineCursorIndex = 0;
                    UpdateSubBufferCursor();
                    break;
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
            }
        }

        protected void SaveCursorPos()
        {
            savedCursorRow = AbsoluteCursorRow;
            savedCursorColumn = CursorColumn;
        }

        protected void RestoreCursorPos()
        {
            AbsoluteCursorRow = savedCursorRow;
            CursorColumn = savedCursorColumn;
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
                    UpdateLineSubBuffer();
                }
            }
        }

        private List<string> SplitIntoLinesPreserveNewLine(string text)
        {
            List<string> lines = SplitIntoLines(text);

            if (text.EndsWith("\n"))
            {
                int newLinesCount = text.Length - text.TrimEnd('\n').Length;
                while(newLinesCount-- > 0)
                    lines.Add("");
            }

            return lines;
        }

        protected void UpdateLineSubBuffer()
        {
            string commandText = LineBuilder.ToString();
            List<string> lines = SplitIntoLinesPreserveNewLine(commandText);
                        
            if (lines.Count != LineSubBuffer.RowCount)
                LineSubBuffer.SetSize(lines.Count, LineSubBuffer.ColumnCount);

            for (int lineIndex = 0; lineIndex < lines.Count; lineIndex++)
            {
                char[] lineCharArray = lines[lineIndex].PadRight(LineSubBuffer.ColumnCount, ' ').ToCharArray();
                lineCharArray.CopyTo(LineSubBuffer.Buffer[lineIndex], 0);   
            }

            UpdateSubBufferCursor(lines);
        }

        private void UpdateSubBufferCursor()
        {
            string commandText = LineBuilder.ToString();
            List<string> lines = SplitIntoLinesPreserveNewLine(commandText);
            UpdateSubBufferCursor(lines);
        }

        private void UpdateSubBufferCursor(List<string> lines)
        {
            int lineIndex = 0;
            int lineCursorIndex = LineCursorIndex;

            while ((lineIndex < lines.Count)
                && (lineCursorIndex > lines[lineIndex].Length))
            {
                lineCursorIndex -= lines[lineIndex].Length;
                // if the line is shorter than the width of the screen then move
                // the cursor another position to compensate for the newline character
                if (lines[lineIndex].Length < ColumnCount)
                    lineCursorIndex--;
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
            if (CursorColumnShow >= ColumnCount)
            {
                int tooBigColumn = CursorColumnShow;
                cursorColumnBuffer = (tooBigColumn % ColumnCount);
                cursorRowBuffer += (tooBigColumn / ColumnCount); // truncating integer division.
            }
            if (CursorRowShow >= RowCount)
            {
                int rowsToScroll = (CursorRowShow-RowCount) + 1;
                CursorRow -= rowsToScroll;
                ScrollVertical(rowsToScroll);
                AddNewBufferLines(rowsToScroll);
            }
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
        }

        protected override void UpdateSubBuffers()
        {
            LineSubBuffer.PositionRow = AbsoluteCursorRow;
        }
    }
}
