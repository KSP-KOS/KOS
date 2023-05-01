using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace kOS.Safe.Screen
{
    public class PrintingBuffer : SubBuffer
    {
        public int CursorRow { private set; get; }
        public int CursorColumn { private set; get; }

        /// <summary>
        /// Move the cursor.
        /// Clamps the cursor to buffer bounds, generally.
        /// If AutoExtend is true, this may extend the Buffer to move the cursor past the previous last row.
        /// </summary>
        /// <param name="row">Target row</param>
        /// <param name="column">Target column</param>
        /// <returns>If the cursor was moved to the given position (and not clamped to bounds)</returns>
        public bool MoveCursor(int row, int column)
        {
            bool outOfBounds = false;
            
            if(column < 0)
            {
                column = 0;
                outOfBounds = true;
            }
            else if(column >= ColumnCount)
            {
                column = ColumnCount - 1;
                outOfBounds = true;
            }

            if (row >= RowCount)
            {
                if (AutoExtend)
                {
                    SetSize(row + 1, ColumnCount);
                }
                else
                {
                    row = RowCount - 1;
                    outOfBounds = true;
                }
            }
            else if(row < 0)
            {
                row = 0;
                outOfBounds = true;
            }

            CursorColumn = column;
            CursorRow = row;

            return !outOfBounds;
        }

        /// <summary>
        /// Invokes MoveCursor, and if the cursor is clamped, reset it to the previous position
        /// </summary>
        /// <param name="row">Target row</param>
        /// <param name="column">Target column</param>
        /// <returns>If the cursor was moved to the given position (and not reset to previous)</returns>
        public bool TryMoveCursor(int row, int column)
        {
            int oldRow = CursorRow;
            int oldColumn = CursorColumn;

            if(!MoveCursor(row, column))
            {
                CursorRow = oldRow;
                CursorColumn = oldColumn;
                return false;
            }
            return true;
        }


        /// <summary>
        /// Prints the given text, including wrapover as necessary, and advances the cursor.
        /// If AutoExtend is true, this may add rows to accomodate the text.
        /// </summary>
        /// <param name="text">Text to print</param>
        /// <param name="trailingNewLine">Ensure there is a newline after the text</param>
        /// <returns>Could we print the full text or did we run out of buffer?</returns>
        public bool Print(string text, bool trailingNewLine)
        {
            List<string> lines = SplitIntoLines(text);
            bool internalNewLine = false;
            foreach (string line in lines)
            {
                if(internalNewLine)
                {
                    bool inBounds = TryMoveCursor(CursorRow + 1, 0);
                    if(!inBounds)
                    {
                        return false;
                    }
                }
                PrintLine(line);
                internalNewLine = true;
            }
            if(trailingNewLine || CursorColumn == ColumnCount)
            {
                TryMoveCursor(CursorRow + 1, 0);
            }
            return true;
        }

        /// <summary>
        /// Insert a string at the current cursor, which must not be longer than the remaining line.
        /// </summary>
        private void PrintLine(string textToPrint)
        {
            IScreenBufferLine lineBuffer = Buffer[CursorRow];
            textToPrint = StripUnprintables(textToPrint);
            lineBuffer.ArrayCopyFrom(textToPrint.ToCharArray(), 0, CursorColumn);
            CursorColumn += textToPrint.Length;
        }
        
        private string StripUnprintables(string textToPrint)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char ch in textToPrint)
            {
                if (0x0020 <= ch)
                    sb.Append(ch);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Split a text into lines starting from the current cursor.
        /// </summary>
        protected List<string> SplitIntoLines(string textToPrint)
        {
            return ScreenBuffer.SplitIntoLines(textToPrint, ColumnCount, CursorColumn);
        }

        public override string DebugDump()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("DebugDump PrintingBuffer: CursorRow = " + CursorRow + ", CursorColumn = " + CursorColumn + ", Base: ");
            sb.Append(base.DebugDump());
            return sb.ToString();
        }

        /// <summary>
        /// Replaces Buffer with one of the new current size, copying old contents
        /// over. This implementation respects WillTruncate and KeepShortenedLines, as well as moving the cursor along.
        /// </summary>
        /// <returns>The amount of scrolling to happen as a result</returns>
        protected override int ResizeBuffer()
        {
            List<IScreenBufferLine> newBuffer = new List<IScreenBufferLine>();

            int newRow = 0;
            int oldRow = 0;
            int newCol = 0;
            bool isFull = false;
            int scrollDiff = 0;

            // First, copy everything from old to new, and perhaps enlarge the new if the old won't fit.
            while (oldRow < Buffer.Count && !isFull)
            {
                int oldCol = 0;
                while (oldCol < Buffer[oldRow].Length && !isFull)
                {
                    if (newCol >= ColumnCount) // wrap to new line when cur line is full.
                    {
                        ++newRow;
                        newCol = 0;
                    }
                    if (newRow >= newBuffer.Count) // grow to required size, or quit if we aren't supposed to grow
                    {
                        if (WillTruncate)
                        {
                            isFull = true;
                            break;
                        }
                        else
                            newBuffer.Add(new ScreenBufferLine(ColumnCount));
                    }
                    newBuffer[newRow][newCol] = Buffer[oldRow][oldCol];
                    if (oldCol == CursorColumn && oldRow == CursorRow)
                    { //keep cursor position relative to content
                        CursorColumn = newCol;
                        scrollDiff = newRow - CursorRow;
                        CursorRow = newRow;
                    }
                    ++newCol;
                    ++oldCol;
                }
                ++oldRow;
                if (KeepShortenedLines)
                { //create empty space instead of wrapping back over
                    ++newRow;
                    newCol = 0;
                }
            }

            // Then maybe append more empty rows if the above wasn't enough to fill the new size:
            while (newBuffer.Count < RowCount)
                newBuffer.Add(new ScreenBufferLine(ColumnCount));
            
            // Because Buffer is readonly, copy the data from newBuffer into it rather than just resetting the reference to newBuffer:
            Buffer.Clear();
            foreach (var t in newBuffer)
            {
                Buffer.Add(t);
            }

            RowCount = newBuffer.Count;

            return Fixed ? 0 : scrollDiff;
        }
    }
}
