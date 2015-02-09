using System.Collections.Generic;

namespace kOS.Safe.Screen
{
    public interface IScreenBuffer
    {
        int CursorRowShow { get; }
        int CursorColumnShow { get; }
        int RowCount { get; }
        int ColumnCount { get; }
        int AbsoluteCursorRow { get; set; }
        void SetSize(int rowCount, int columnCount);
        int ScrollVertical(int deltaRows);
        void MoveCursor(int row, int column);
        void MoveToNextLine();
        void PrintAt(string textToPrint, int row, int column);
        void Print(string textToPrint);
-       void Print(string textToPrint, bool addNewLine);
		void HudTxt(string textToHud, int delay, int style, int size, string color, int mirror );
        void ClearScreen();
        void AddSubBuffer(SubBuffer subBuffer);
        void RemoveSubBuffer(SubBuffer subBuffer);
        List<char[]> GetBuffer();
    }

    public class ScreenBuffer : IScreenBuffer
    {
        public const int MAX_ROWS = 36;
        public const int MAX_COLUMNS = 50;
        
        private int topRow;
        private readonly List<char[]> buffer;
        private readonly List<SubBuffer> subBuffers;

        protected int CursorRow { get; set; }
        protected int CursorColumn { get; set; }
        public virtual int CursorRowShow { get { return CursorRow; } }
        public virtual int CursorColumnShow { get { return CursorColumn; } }
        public int RowCount { get; private set; }
        public int ColumnCount { get; private set; }
		public int AbsoluteCursorRow
        {
            get { return CursorRow + topRow; }
            set { CursorRow = value - topRow; }
        }


        public ScreenBuffer()
        {
            buffer = new List<char[]>();
            subBuffers = new List<SubBuffer>();

            RowCount = MAX_ROWS;
            ColumnCount = MAX_COLUMNS;
            InitializeBuffer();
        }

        private void InitializeBuffer()
        {
            AddNewBufferLines(RowCount);

            topRow = 0;
            CursorRow = 0;
            CursorColumn = 0;
        }

        protected void AddNewBufferLines( int howMany = 1)
        {
            while (howMany-- > 0)
                buffer.Add(new char[ColumnCount]);
        }

        public void SetSize(int rowCount, int columnCount)
        {
            if (rowCount <= MAX_ROWS && columnCount <= MAX_COLUMNS)
            {
                RowCount = rowCount;
                ColumnCount = columnCount;
                buffer.Clear();
                InitializeBuffer();
            }
        }

        private int ScrollVerticalInternal(int deltaRows = 1)
        {
            int maxTopRow = buffer.Count - 1;

            // boundary checks
            if (topRow + deltaRows < 0) deltaRows = -topRow;
            if (topRow + deltaRows > maxTopRow) deltaRows = (maxTopRow - topRow);

            topRow += deltaRows;

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
            if (row < 0 ) row = 0;

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
                AddNewBufferLines();
                ScrollVerticalInternal();
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
            var lineList = new List<string>();
            int availableColumns = ColumnCount - CursorColumn;

            string[] lines = textToPrint.Trim(new[] { '\r', '\n' }).Split('\n');
            
            foreach (string line in lines)
            {
                string lineToPrint = line.TrimEnd('\r');
                int startIndex = 0;
                
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
            char[] lineBuffer = buffer[AbsoluteCursorRow];
            textToPrint.ToCharArray().CopyTo(lineBuffer, CursorColumn);
            MoveColumn(textToPrint.Length);
        }

		public void HudTxt(string textToHud, int delay, int style, int size, string color, int mirror)
		{
		}

        public void ClearScreen()
        {
            buffer.Clear();
            InitializeBuffer();
        }

	
        public void AddSubBuffer(SubBuffer subBuffer)
        {
            subBuffers.Add(subBuffer);
        }

        public void RemoveSubBuffer(SubBuffer subBuffer)
        {
            subBuffers.Remove(subBuffer);
        }

        public List<char[]> GetBuffer()
        {
            
            // base buffer
            var mergedBuffer = new List<char[]>(buffer.GetRange(topRow, RowCount));

            // The screen may be scrolled such that the bottom of the text content doesn't
            // go all the way to the bottom of the screen.  If so, pad it for display:
            while (mergedBuffer.Count < RowCount)
            {
                mergedBuffer.Add(new char[ColumnCount]);
            }

            // merge sub buffers
            UpdateSubBuffers();
            foreach (SubBuffer subBuffer in subBuffers)
            {
                if (subBuffer.RowCount > 0 && subBuffer.Enabled)
                {
                    int mergeRow = subBuffer.Fixed ? subBuffer.PositionRow : (subBuffer.PositionRow - topRow);

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
