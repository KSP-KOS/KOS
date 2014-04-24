using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace kOS.Screen
{
    public class ScreenBuffer
    {
        public const int MAX_ROWS = 36;
        public const int MAX_COLUMNS = 50;
        
        protected int _topRow;
        private List<char[]> _buffer;
        private List<SubBuffer> _subBuffers;

        public int RowCount { get; private set; }
        public int ColumnCount { get; private set; }
        public int CursorRow { get; protected set; }
        public int CursorColumn { get; protected set; }
        public virtual int CursorRowShow { get { return CursorRow; } }
        public virtual int CursorColumnShow { get { return CursorColumn; } }

        public int AbsoluteCursorRow
        {
            get { return CursorRow + _topRow; }
            set { CursorRow = value - _topRow; }
        }


        public ScreenBuffer()
        {
            _buffer = new List<char[]>();
            _subBuffers = new List<SubBuffer>();

            RowCount = MAX_ROWS;
            ColumnCount = MAX_COLUMNS;
            InitializeBuffer();
        }

        private void InitializeBuffer()
        {
            for (int row = 0; row < RowCount; row++)
            {
                AddNewBufferLine();
            }

            _topRow = 0;
            CursorRow = 0;
            CursorColumn = 0;
        }

        private void AddNewBufferLine()
        {
            _buffer.Add(new char[ColumnCount]);
        }

        public void SetSize(int rowCount, int columnCount)
        {
            if (rowCount <= 36 && columnCount <= 50)
            {
                RowCount = rowCount;
                ColumnCount = columnCount;
                _buffer.Clear();
                InitializeBuffer();
            }
        }

        private int ScrollVerticalInternal(int deltaRows)
        {
            int maxTopRow = _buffer.Count - RowCount;

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
            if (row >= RowCount)
            {
                row = RowCount - 1;
                MoveToNextLine();
            }

            if (column >= ColumnCount) column = ColumnCount - 1;
            if (column < 0) column = 0;

            CursorRow = row;
            CursorColumn = column;
        }

        public void MoveToNextLine()
        {
            if ((CursorRow + 1) >= RowCount)
            {
                // scrolling up
                AddNewBufferLine();
                ScrollVerticalInternal(1);
            }
            else
            {
                CursorRow++;
            }

            CursorColumn = 0;
        }

        private void MoveColumn(int deltaPosition)
        {
            if (deltaPosition > 0)
            {
                CursorColumn += deltaPosition;
                while (CursorColumn >= ColumnCount)
                {
                    CursorColumn -= ColumnCount;
                    MoveToNextLine();
                }
            }
        }

        public virtual void PrintAt(string textToPrint, int row, int column)
        {
            MoveCursor(row, column);
            Print(textToPrint, false);
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

                if (CursorColumn > 0 && addNewLine)
                {
                    MoveToNextLine();
                    CursorColumn = 0;
                }
            }
        }

        protected List<string> SplitIntoLines(string textToPrint)
        {
            List<string> lineList = new List<string>();
            int availableColumns = ColumnCount - CursorColumn;
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
                    availableColumns = ColumnCount;
                }

                lineList.Add(lineToPrint.Substring(startIndex));
                availableColumns = ColumnCount;
            }

            return lineList;
        }

        private void PrintLine(string textToPrint)
        {
            char[] lineBuffer = _buffer[AbsoluteCursorRow];
            textToPrint.ToCharArray().CopyTo(lineBuffer, CursorColumn);
            MoveColumn(textToPrint.Length);
        }

        public void ClearScreen()
        {
            _buffer.Clear();
            InitializeBuffer();
        }

        public void AddSubBuffer(SubBuffer subBuffer)
        {
            _subBuffers.Add(subBuffer);
        }

        public void RemoveSubBuffer(SubBuffer subBuffer)
        {
            _subBuffers.Remove(subBuffer);
        }

        public List<char[]> GetBuffer()
        { 
            // base buffer
            List<char[]> mergedBuffer = new List<char[]>(_buffer.GetRange(_topRow, RowCount));

            // merge sub buffers
            UpdateSubBuffers();
            foreach (SubBuffer subBuffer in _subBuffers)
            {
                if (subBuffer.RowCount > 0 && subBuffer.Enabled)
                {
                    int mergeRow = subBuffer.Fixed ? subBuffer.PositionRow : (subBuffer.PositionRow - _topRow);

                    if ((mergeRow + subBuffer.RowCount) > 0 && mergeRow < RowCount)
                    {
                        int startRow = (mergeRow < 0) ? -mergeRow : 0;
                        int rowsToMerge = subBuffer.RowCount - startRow;
                        if ((mergeRow + rowsToMerge) > RowCount) rowsToMerge = (RowCount - mergeRow);
                        List<char[]> bufferRange = subBuffer.Buffer.GetRange(startRow, rowsToMerge);

                        // remove the replaced rows
                        mergedBuffer.RemoveRange(mergeRow, rowsToMerge);
                        // insert the new ones
                        mergedBuffer.InsertRange(mergeRow, bufferRange);
                    }
                }
            }

            return mergedBuffer;
        }

        protected virtual void UpdateSubBuffers()
        {
            // so subclasses can do something with their subbuffers before they are merged
        }
    }
}
