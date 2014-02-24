using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using kOS.Utilities;

namespace kOS
{
    public class TextEditor : ScreenBuffer
    {
        protected StringBuilder _lineBuilder = new StringBuilder();
        protected int _lineStartRow;
        protected bool _updateLineStartRow = true;
        private int _savedCursorRow;
        private int _savedCursorColumn;
        
        public virtual void Type(char ch)
        {
            UpdateLineStartRow();
            
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
            UpdateLineStartRow();

            switch (key)
            {
                case kOSKeys.LEFT:
                    MoveCursor(-1);
                    break;
                case kOSKeys.RIGHT:
                    MoveCursor(1);
                    break;
                case kOSKeys.HOME:
                    MoveCursor(-_cursorColumn);
                    break;
                case kOSKeys.END:
                    MoveCursor((_lineBuilder.Length % _columnCount) - _cursorColumn);
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

        protected void UpdateLineStartRow()
        {
            if (_updateLineStartRow)
            {
                _lineStartRow = AbsoluteCursorRow;
                _updateLineStartRow = false;
            }
        }
        
        private int GetCursorIndex(int referenceRow)
        {
            return ((AbsoluteCursorRow - referenceRow) * _columnCount) + _cursorColumn;
        }

        protected void SaveCursorPos()
        {
            _savedCursorRow = AbsoluteCursorRow;
            _savedCursorColumn = AbsoluteCursorColumn;
        }

        protected void RestoreCursorPos()
        {
            AbsoluteCursorRow = _savedCursorRow;
            AbsoluteCursorColumn = _savedCursorColumn;
        }

        protected override void InsertChar(char character)
        {
            int cursorIndex = GetCursorIndex(_lineStartRow);
            char lastChar = _buffer[AbsoluteCursorRow][_columnCount - 1];
            
            _lineBuilder.Insert(cursorIndex, character);
            base.InsertChar(character);

            SaveCursorPos();
            OverflowLast(lastChar);
            RestoreCursorPos();
        }

        protected virtual void OverflowLast(char overflowChar)
        {
            // if the last character of the line is not empty then
            // it gets inserted at the beggining of the next line
            if (overflowChar != 0)
            {
                int originalCursorRow = _cursorRow;
                int originalCursorColumn = _cursorColumn;
                
                MoveToNextLine();
                char lastChar = _buffer[AbsoluteCursorRow][_columnCount - 1];
                base.InsertChar(overflowChar);
                OverflowLast(lastChar);

                _cursorRow = originalCursorRow;
                _cursorColumn = originalCursorColumn;
            }
        }

        protected override void RemoveChar()
        {
            if (_lineBuilder.Length > 0)
            {
                int cursorIndex = GetCursorIndex(_lineStartRow);
                if (cursorIndex >= 0 && cursorIndex < _lineBuilder.Length)
                {
                    _lineBuilder.Remove(cursorIndex, 1);
                    base.RemoveChar();

                    SaveCursorPos();
                    OverflowFirst();
                    RestoreCursorPos();
                }
            }
        }

        protected virtual void OverflowFirst()
        {
            int charCursorRow = AbsoluteCursorRow + 1;
            if (charCursorRow < _buffer.Count)
            {
                char firstChar = _buffer[charCursorRow][0];
                if (firstChar != 0)
                {
                    PrintAt(firstChar, _cursorRow, _columnCount - 1);
                    // PrintAt moves the cursor to the next character and since we are printing
                    // at the last column the cursor ends up at the beggining of the next line
                    base.RemoveChar();
                    OverflowFirst();
                }
            }
        }

        protected virtual void NewLine()
        {
            MoveToNextLine();
            _lineBuilder = new StringBuilder();
            _lineStartRow = AbsoluteCursorRow;
            _updateLineStartRow = true;
        }

        protected virtual bool TryMoveCursor(int deltaPosition)
        {
            bool success = false;

            if (_lineBuilder.Length > 0 && deltaPosition != 0)
            {
                int newCursorIndex = GetCursorIndex(_lineStartRow) + deltaPosition;

                if (newCursorIndex >= 0 && newCursorIndex < _lineBuilder.Length)
                {
                    MoveCursor(deltaPosition);
                    success = true;
                }
            }

            return success;
        }

        protected virtual void MoveCursor(int deltaPosition)
        {
            int newCursorColumn = _cursorColumn + deltaPosition;
            int newCursorRow = _cursorRow;
            
            while (newCursorColumn < 0)
            {
                newCursorColumn += _columnCount;
                newCursorRow--;
            }

            while (newCursorColumn >= _columnCount)
            {
                newCursorColumn -= _columnCount;
                newCursorRow++;
            }

            if (newCursorColumn < 0) newCursorColumn = 0;
            if (newCursorColumn >= _columnCount) newCursorColumn = _columnCount;

            MoveCursor(newCursorRow, newCursorColumn);
        }

        public override int ScrollVertical(int deltaRows)
        {
            deltaRows = base.ScrollVertical(deltaRows);
            _cursorRow -= deltaRows;
            return deltaRows;
        }

        public virtual void Reset()
        {
            _lineBuilder = new StringBuilder();
            _updateLineStartRow = true;
        }
    }
}
