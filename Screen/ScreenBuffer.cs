using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace kOS.Screen
{
    public class ScreenBuffer
    {
        protected static readonly int _rowCount = 36;
        protected static readonly int _columnCount = 50;
        protected int _cursorRow;
        protected int _cursorColumn;
        protected int _topRow;
        protected List<char[]> _buffer;

        public List<char[]> Buffer { get { return _buffer.GetRange(_topRow, _rowCount); } }


        public int RowCount
        {
            get { return _rowCount; }
        }

        public int ColumnCount
        {
            get { return _columnCount; }
        }

        public int CursorRow
        {
            get { return _cursorRow; }
        }

        public int CursorColumn
        {
            get { return _cursorColumn; }
        }

        public int AbsoluteCursorRow
        {
            get { return _topRow + _cursorRow; }
            set { _cursorRow = value - _topRow; }
        }

        public int AbsoluteCursorColumn
        {
            get { return _cursorColumn; }
            set { _cursorColumn = value; }
        }

        
        public ScreenBuffer()
        {
            _buffer = new List<char[]>();
            InitializeBuffer();
        }

        private void InitializeBuffer()
        {
            for (int row = 0; row < _rowCount; row++)
            {
                AddNewBufferLine();
            }

            _topRow = 0;
            _cursorRow = 0;
            _cursorColumn = 0;
        }

        private void AddNewBufferLine()
        {
            _buffer.Add(new char[_columnCount]);
        }

        private int ScrollVerticalInternal(int deltaRows)
        {
            int maxTopRow = _buffer.Count - _rowCount;
            
            // boundary checks
            if (_topRow + deltaRows < 0) deltaRows = -_topRow;
            if (_topRow + deltaRows > maxTopRow) deltaRows = (maxTopRow - _topRow);

            _topRow += deltaRows;
            
            return deltaRows;
        }

        public virtual int ScrollVertical(int deltaRows)
        {
            return ScrollVerticalInternal(deltaRows);
        }

        public void MoveCursor(int row, int column)
        {
            if (row >= _rowCount)
            {
                row = _rowCount - 1;
                MoveToNextLine();
            }

            if (column >= _columnCount) column = _columnCount - 1;
            if (column < 0) column = 0;

            _cursorRow = row;
            _cursorColumn = column;
        }

        public void MoveToNextLine()
        {
            if ((_cursorRow + 1) >= _rowCount)
            {
                // scrolling up
                AddNewBufferLine();
                ScrollVerticalInternal(1);
            }
            else
            {
                _cursorRow++;
            }

            _cursorColumn = 0;
        }

        private void MoveColumn(int deltaPosition)
        {
            if (deltaPosition > 0)
            {
                _cursorColumn += deltaPosition;
                while (_cursorColumn >= _columnCount)
                {
                    _cursorColumn -= _columnCount;
                    MoveToNextLine();
                }
            }
        }

        public void PrintAtAbsolute(string textToPrint, int row, int column)
        {
            int relativeRow = row - _topRow;
            PrintAt(textToPrint, relativeRow, column);
        }

        public virtual void PrintAt(string textToPrint, int row, int column)
        {
            MoveCursor(row, column);
            Print(textToPrint, false);
        }

        public virtual void PrintAt(char character, int row, int column)
        {
            MoveCursor(row, column);
            PrintChar(character);
        }

        public void Print(string textToPrint)
        {
            Print(textToPrint, true);
        }

        public void Print(string textToPrint, bool addNewLine)
        {
            List<string> lines = SplitIntoLines(textToPrint);
            foreach (string line in lines)
            {
                PrintLine(line);

                if (_cursorColumn > 0 && addNewLine)
                {
                    MoveToNextLine();
                    _cursorColumn = 0;
                }
            }
        }

        private List<string> SplitIntoLines(string textToPrint)
        {
            List<string> lineList = new List<string>();
            int availableColumns = _columnCount - _cursorColumn;
            int startIndex;

            string[] lines = textToPrint.Trim(new char[] { '\r', '\n' }).Split('\n');
            
            for (int index = 0; index < lines.Length; index++)
            {
                string lineToPrint = lines[index].TrimEnd('\r');
                startIndex = 0;
                
                while ((lineToPrint.Length - startIndex) > availableColumns)
                {
                    lineList.Add(lineToPrint.Substring(startIndex, availableColumns));
                    startIndex += availableColumns;
                    availableColumns = _columnCount;
                }

                lineList.Add(lineToPrint.Substring(startIndex));
                availableColumns = _columnCount;
            }

            return lineList;
        }

        private void PrintLine(string textToPrint)
        {
            char[] lineBuffer = _buffer[AbsoluteCursorRow];
            textToPrint.ToCharArray().CopyTo(lineBuffer, _cursorColumn);
            MoveColumn(textToPrint.Length);
        }

        public void PrintChar(char character)
        {
            _buffer[AbsoluteCursorRow][AbsoluteCursorColumn] = character;
            MoveColumn(1);
        }

        protected virtual void InsertChar(char character)
        {
            char[] lineBuffer = _buffer[AbsoluteCursorRow];
            char[] newLineBuffer = new char[_columnCount];
            char lastChar = lineBuffer[lineBuffer.Length - 1];

            _buffer[AbsoluteCursorRow] = newLineBuffer;
            newLineBuffer[_cursorColumn] = character;
            // copy characters before the cursor
            if (_cursorColumn > 0)
                Array.Copy(lineBuffer, newLineBuffer, _cursorColumn);
            // copy characters after the cursor
            if (_cursorColumn < _columnCount)
                Array.Copy(lineBuffer, _cursorColumn, newLineBuffer, _cursorColumn + 1,  _columnCount - _cursorColumn - 1);
            // move the cursor
            MoveColumn(1);
        }

        protected virtual void RemoveChar()
        {
            char[] lineBuffer = _buffer[AbsoluteCursorRow];
            char[] newLineBuffer = new char[_columnCount];

            _buffer[AbsoluteCursorRow] = newLineBuffer;
            // copy characters before the cursor
            if (_cursorColumn > 0)
                Array.Copy(lineBuffer, newLineBuffer, _cursorColumn);
            // copy characters after the cursor
            if (_cursorColumn < _columnCount)
                Array.Copy(lineBuffer, _cursorColumn + 1, newLineBuffer, _cursorColumn, _columnCount - _cursorColumn - 1);
        }

        public void ClearScreen()
        {
            _buffer.Clear();
            InitializeBuffer();
        }
    }
}
