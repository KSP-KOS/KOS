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
        
        public override int CursorColumnShow { get { return _lineCursorIndex % ColumnCount; } }
        public override int CursorRowShow { get { return CursorRow + (_lineCursorIndex / ColumnCount); } }    // integer division

        
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
                    break;
                case kOSKeys.END:
                    _lineCursorIndex = _lineBuilder.Length;
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

        protected void UpdateLineSubBuffer()
        {
            int startIndex = 0;
            int lineIndex = 0;
            int lineCount = ((_lineBuilder.Length - 1) / _lineSubBuffer.ColumnCount) + 1;   // integer division

            if (lineCount != _lineSubBuffer.RowCount) _lineSubBuffer.SetSize(lineCount, _lineSubBuffer.ColumnCount);

            while ((_lineBuilder.Length - startIndex) > _lineSubBuffer.ColumnCount)
            {
                _lineBuilder.CopyTo(startIndex, _lineSubBuffer.Buffer[lineIndex], 0, _lineSubBuffer.ColumnCount);
                startIndex += _lineSubBuffer.ColumnCount;
                lineIndex++;
            }

            char[] bufferLine = new char[_lineSubBuffer.ColumnCount];
            _lineBuilder.CopyTo(startIndex, bufferLine, 0, (_lineBuilder.Length - startIndex));
            bufferLine.CopyTo(_lineSubBuffer.Buffer[lineIndex], 0);
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
