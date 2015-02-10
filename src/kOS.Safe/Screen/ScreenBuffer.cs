using System.Collections.Generic;
using System;
using System.Text;

namespace kOS.Safe.Screen
{
    public class ScreenBuffer : IScreenBuffer
    {
        private const int DEFAULT_ROWS = 36;
        private const int DEFAULT_COLUMNS = 50;
        
        private int topRow;
        
        // Needed so the terminal knows when it's been scrolled, for its diffing purposes.
        public int TopRow {get {return topRow;}}
        
        private readonly List<IScreenBufferLine> buffer;
        private readonly List<SubBuffer> subBuffers;

        protected int CursorRow { get; set; }
        protected int CursorColumn { get; set; }
        public virtual int CursorRowShow { get { return CursorRow; } }
        public virtual int CursorColumnShow { get { return CursorColumn; } }
        public int RowCount { get; private set; }
        public int ColumnCount { get; private set; }
        protected List<ResizeNotifier> Notifyees { get; set; }

        /// <summary>Delegate prototype expected by AddResizeNotifier</summary>
        /// <param name="sb">This screenbuffer telling the callback who it is</param>
        /// <returns>telling this screenbuffer how many vertical rows to scroll as a result of the resize.</returns>
        public delegate int ResizeNotifier(IScreenBuffer sb);

        public int AbsoluteCursorRow
        {
            get { return CursorRow + topRow; }
            set { CursorRow = value - topRow; }
        }

        public ScreenBuffer()
        {
            buffer = new List<IScreenBufferLine>();
            Notifyees = new List<ResizeNotifier>();
            
            subBuffers = new List<SubBuffer>();

            RowCount = DEFAULT_ROWS;
            ColumnCount = DEFAULT_COLUMNS;
            InitializeBuffer();
        }

        private void InitializeBuffer()
        {
            buffer.Clear();
            AddNewBufferLines(RowCount);

            topRow = 0;
            CursorRow = 0;
            CursorColumn = 0;
        }

        protected void AddNewBufferLines( int howMany = 1)
        {
            while (howMany-- > 0)
                buffer.Add(new ScreenBufferLine(ColumnCount));
        }

        public void AddResizeNotifier(ScreenBuffer.ResizeNotifier notifier)
        {
            if (Notifyees.IndexOf(notifier) < 0)
                Notifyees.Add(notifier);
        }

        public void RemoveResizeNotifier(ScreenBuffer.ResizeNotifier notifier)
        {
            Notifyees.Remove(notifier);
        }

        public void SetSize(int rows, int columns)
        {
            RowCount = rows;
            ColumnCount = columns;
            ResizeBuffer();
            int scrollDiff = 0;
            foreach (ResizeNotifier notifier in Notifyees)
            {
                if (notifier != null)
                    scrollDiff += notifier(this);
            }
            ScrollVertical(scrollDiff);
        }
        
        protected void ResizeBuffer()
        {
            // Grow or shrink the width of the buffer lines to match the new
            // value.  Note that this does not (yet) account for preserving lines and wrapping them.
            for (int row = 0 ; row < buffer.Count ; ++row)
            {
                ScreenBufferLine newRow = new ScreenBufferLine(ColumnCount);
                newRow.ArrayCopyFrom(buffer[row], 0, 0, Math.Min(buffer[row].Length, ColumnCount));
                buffer[row] = newRow;
            }
            
            // Add more buffer lines if needed to pad out the rest of the screen:
            while (buffer.Count - topRow < RowCount)
                buffer.Add(new ScreenBufferLine(ColumnCount));
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
            IScreenBufferLine lineBuffer = buffer[AbsoluteCursorRow];
            lineBuffer.ArrayCopyFrom(textToPrint.ToCharArray(), 0, CursorColumn);
            MoveColumn(textToPrint.Length);
        }

        public void ClearScreen()
        {
            buffer.Clear();
            InitializeBuffer();
        }

        public void AddSubBuffer(SubBuffer subBuffer)
        {
            subBuffers.Add(subBuffer);
            AddResizeNotifier(subBuffer.NotifyOfParentResize);
        }

        public void RemoveSubBuffer(SubBuffer subBuffer)
        {
            subBuffers.Remove(subBuffer);
        }

        public List<IScreenBufferLine> GetBuffer()
        {
            // base buffer
            var mergedBuffer = new List<IScreenBufferLine>(buffer.GetRange(topRow, RowCount));

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
                        List<IScreenBufferLine> bufferRange = subBuffer.Buffer.GetRange(startRow, rowsToMerge);

                        // remove the replaced rows
                        mergedBuffer.RemoveRange(mergeRow, rowsToMerge);
                        // insert the new ones
                        mergedBuffer.InsertRange(mergeRow, bufferRange);
                    }
                }
            }

            return mergedBuffer;
        }

        // This was handy when trying to figure out what was going on.  
        public string DebugDump()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("DebugDump ScreenBuffer: RowCount="+RowCount+", ColumnCount="+ColumnCount+", topRow="+topRow+", buffer.count="+buffer.Count+"\n");
            for (int i = 0; i < buffer.Count ; ++i)
            {
                sb.Append(" line "+i+" = [");
                for (int j = 0 ; j < buffer[i].Length ; ++j)
                {
                    char ch = buffer[i][j];
                    sb.Append((int)ch < 32 ? (" \\"+(int)ch) : (" "+ch) );
                }
                sb.Append("]\n");
            }
            foreach (SubBuffer sub in subBuffers)
                sb.Append(sub.DebugDump());
            return sb.ToString();
        }
        

        protected virtual void UpdateSubBuffers()
        {
            // so subclasses can do something with their subbuffers before they are merged
        }
    }

}
