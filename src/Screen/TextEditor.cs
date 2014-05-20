using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using kOS.Utilities;

namespace kOS.Screen
{
    public class TextEditor : ScreenBuffer
    {
        protected StringBuilder _lineBuilder;
        protected int _lineCursorIndex = 0;
        protected SubBuffer _lineSubBuffer;
        private int _savedCursorRow;
        private int _savedCursorColumn;
        private int _cursorColumnBuffer;
        private int _cursorRowBuffer;
        
        public override int CursorColumnShow { get { return _cursorColumnBuffer; } }
        public override int CursorRowShow { get { return CursorRow + _cursorRowBuffer; } }

        
        public TextEditor() : base()
        {
            _lineBuilder = new StringBuilder();
            CreateSubBuffer();
        }

        private void CreateSubBuffer()
        {
            _lineSubBuffer = new SubBuffer();
            _lineSubBuffer.SetSize(1, ColumnCount);
            _lineSubBuffer.Enabled = true;
            AddSubBuffer(_lineSubBuffer);
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

        public virtual void SpecialKey(kOSKeys key)
        {
            switch (key)
            {
                case kOSKeys.LEFT:
                    TryMoveCursor(-1);
                    break;
                case kOSKeys.RIGHT:
                    TryMoveCursor(1);
                    break;
                case kOSKeys.HOME:
                    _lineCursorIndex = 0;
                    UpdateSubBufferCursor();
                    break;
                case kOSKeys.END:
                    _lineCursorIndex = _lineBuilder.Length;
                    UpdateSubBufferCursor();
                    break;
                case kOSKeys.DEL:
                    RemoveChar();
                    break;
                case kOSKeys.PGUP:
                    ScrollVertical(-10);
                    break;
                case kOSKeys.PGDN:
                    ScrollVertical(10);
                    break;
            }
        }

        protected void SaveCursorPos()
        {
            _savedCursorRow = AbsoluteCursorRow;
            _savedCursorColumn = CursorColumn;
        }

        protected void RestoreCursorPos()
        {
            AbsoluteCursorRow = _savedCursorRow;
            CursorColumn = _savedCursorColumn;
        }

        protected void InsertChar(char character)
        {
            _lineBuilder.Insert(_lineCursorIndex, character);
            _lineCursorIndex++;
            UpdateLineSubBuffer();
        }

        protected void RemoveChar()
        {
            if (_lineBuilder.Length > 0)
            {
                if (_lineCursorIndex >= 0 && _lineCursorIndex < _lineBuilder.Length)
                {
                    _lineBuilder.Remove(_lineCursorIndex, 1);
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
            string commandText = _lineBuilder.ToString();
            List<string> lines = SplitIntoLinesPreserveNewLine(commandText);

            if (lines.Count != _lineSubBuffer.RowCount)
                _lineSubBuffer.SetSize(lines.Count, _lineSubBuffer.ColumnCount);

            for (int lineIndex = 0; lineIndex < lines.Count; lineIndex++)
            {
                char[] lineCharArray = lines[lineIndex].PadRight(_lineSubBuffer.ColumnCount, ' ').ToCharArray();
                lineCharArray.CopyTo(_lineSubBuffer.Buffer[lineIndex], 0);   
            }

            UpdateSubBufferCursor(lines);
        }

        private void UpdateSubBufferCursor()
        {
            string commandText = _lineBuilder.ToString();
            List<string> lines = SplitIntoLinesPreserveNewLine(commandText);
            UpdateSubBufferCursor(lines);
        }

        private void UpdateSubBufferCursor(List<string> lines)
        {
            int lineIndex = 0;
            int lineCursorIndex = _lineCursorIndex;

            while ((lineIndex < lines.Count)
                && (lineCursorIndex > lines[lineIndex].Length))
            {
                lineCursorIndex -= lines[lineIndex].Length;
                // if the line is shorter than the width of the screen then move
                // the cursor another position to compensate for the newline character
                if (lines[lineIndex].Length < MAX_COLUMNS)
                    lineCursorIndex--;
                lineIndex++;
            }

            _cursorRowBuffer = lineIndex;
            _cursorColumnBuffer = lineCursorIndex;

            KeepCursorInBounds();
        }
        
        protected void KeepCursorInBounds()
        {
            // Check to see if wrapping or scrolling needs to be done
            // because the cursor went off the screen, and if so, do it:
            if (CursorColumnShow >= MAX_COLUMNS)
            {
                int tooBigColumn = CursorColumnShow;
                _cursorColumnBuffer = (tooBigColumn % MAX_COLUMNS);
                _cursorRowBuffer += (tooBigColumn / MAX_COLUMNS); // truncating integer division.
            }
            if (CursorRowShow >= MAX_ROWS)
            {
                int rowsToScroll = (CursorRowShow-MAX_ROWS) + 1;
                CursorRow -= rowsToScroll;
                ScrollVertical(rowsToScroll);
                AddNewBufferLines(rowsToScroll);
            }
        }

        protected virtual void NewLine()
        {
            Print(_lineBuilder.ToString());
            _lineBuilder = new StringBuilder();
            _lineCursorIndex = 0;
            UpdateLineSubBuffer();
        }

        protected virtual bool TryMoveCursor(int deltaPosition)
        {
            bool success = false;

            if (_lineBuilder.Length > 0 && deltaPosition != 0)
            {
                int newCursorIndex = _lineCursorIndex + deltaPosition;
                if (newCursorIndex >= 0 && newCursorIndex <= _lineBuilder.Length)
                {
                    _lineCursorIndex += deltaPosition;
                    UpdateSubBufferCursor();
                    success = true;
                }
            }

            return success;
        }

        public virtual void Reset()
        {
            _lineBuilder = new StringBuilder();
            _lineCursorIndex = 0;
        }

        protected override void UpdateSubBuffers()
        {
            _lineSubBuffer.PositionRow = AbsoluteCursorRow;
        }
    }
}
