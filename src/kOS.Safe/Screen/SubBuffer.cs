using System;
using System.Collections.Generic;
using System.Text;

namespace kOS.Safe.Screen
{
    public class SubBuffer
    {
        public readonly List<IScreenBufferLine> Buffer = new List<IScreenBufferLine>();

        public int RowCount { get; protected set; }
        public int ColumnCount { get; private set; }
        public int PositionRow { get; set; }

        /// <summary> is the position of this buffer fixed relative to the View, or is it absolute? </summary>
        public bool Fixed { get; set; }

        /// <summary> should ScreenBuffer merge this into the View? </summary>
        public bool Enabled { get; set; } 

        /// <summary> should resizing truncate contents, or extend the rows? </summary>
        public bool WillTruncate { get; set; }

        /// <summary> should print and movecursor autoextend if it goes past the existing rows? </summary>
        public bool AutoExtend { get; set; }

        /// <summary> should resize not wrap lines back around when resizing to larger?</summary>
        public bool KeepShortenedLines { get; set; }

        /// <summary>
        /// Stretch/shrink the subbuffer to a new size, trying as best as possible to
        /// preserve existing contents, including wrapping lines if needed.  Note that
        /// if the new size is too small to hold the contents, and WillTruncate is false,
        /// then it will refuse to make it that small, and will use more rows than you
        /// requested as needed to preserve contents.
        /// You can override this by setting this SubBuffer's WillTruncate property to true.
        /// </summary>
        /// <param name="rowCount">Requested new number of rows (only guaranteed to be obeyed if WillTruncate is true)</param>
        /// <param name="columnCount">Desired new number of columns (this will be adhered to faithfully).</param>
        public void SetSize(int rowCount, int columnCount)
        {
            foreach( IScreenBufferLine lineBuff in Buffer)
                lineBuff.TouchTime();
            RowCount = rowCount;
            ColumnCount = columnCount;
            ResizeBuffer();
        }
        
        /// <summary>Clear the contents entirely and set the size to 0,ColumnCount.</summary>
        public void Wipe()
        {
            Buffer.Clear();
            RowCount = 0;
        }

        /// <summary>
        /// Marks the given section of rows in the buffer as dirty and
        /// in need of a diff check.
        /// </summary>
        /// <param name="startRow">Starting with this row number</param>
        /// <param name="numRows">for this many rows, or up to the max row the buffer has if this number is too large</param>
        public void MarkRowsDirty(int startRow, int numRows)
        {
            //limit to rows actually existing in the buffer
            if(startRow < 0)
            {
                numRows += startRow;
                startRow = 0;
            }
            int numSafeRows = (numRows + startRow <= RowCount) ? numRows : RowCount - startRow;

            for (int i = 0; i < numSafeRows; ++i)
                Buffer[startRow + i].TouchTime();
        }

        /// <summary>
        /// Write this subbuffer to another subbuffer. Null-characters are not written but used as a mask.
        /// In general, only the overlapping area is actually transfered. If AutoExtend is true, new rows may be added to the end of target to accomodate source.
        /// </summary>
        /// <param name="other">Target SubBuffer</param>
        /// <param name="fixedOffset">PositionRow offset for SubBuffers that are Fixed</param>
        public void MergeTo(SubBuffer other, int fixedOffset)
        {
            int myAbsoluteStartRow = PositionRow + (Fixed ? fixedOffset : 0);
            int otherAbsoluteStartRow = other.PositionRow + (other.Fixed ? fixedOffset : 0);
            
            int myInternalRow = Math.Max(0, otherAbsoluteStartRow - myAbsoluteStartRow);
            int otherInternalRow = Math.Max(0, myAbsoluteStartRow - otherAbsoluteStartRow);

            if(other.AutoExtend)
            {
                int delta = (RowCount - myInternalRow) - (other.RowCount - otherInternalRow);
                if(delta > 0)
                {
                    other.SetSize(other.RowCount + delta, other.ColumnCount);
                }
            }
            
            if (myAbsoluteStartRow + RowCount < otherAbsoluteStartRow || otherAbsoluteStartRow + other.RowCount < myAbsoluteStartRow)
            {
                //no overlap, nothing to do
                return;
            }

            while (myInternalRow < RowCount && otherInternalRow < other.RowCount)
            {
                bool actuallyChanged = false;
                IScreenBufferLine myRow = Buffer[myInternalRow];
                IScreenBufferLine otherRow = other.Buffer[otherInternalRow];

                for(int i = 0; i < ColumnCount && i < other.ColumnCount; ++i)
                {
                    if(myRow[i] != '\0')
                    {
                        otherRow.SetCharIgnoreTime(i, myRow[i]);
                        actuallyChanged = true;
                    }
                }
                if(actuallyChanged)
                {
                    otherRow.SetTimestamp(Math.Max(otherRow.LastChangeTick, myRow.LastChangeTick));
                }

                myInternalRow++;
                otherInternalRow++;
            }
        }

        /// <summary>
        /// The default behavior is to resize the subbuffer's width to match the parent
        /// screenbuffer's width, but to leave the height alone as-is.  All the subbuffers
        /// in kOS as of this writing need that behavior (they're used to edit the command line).
        /// The way is being left open for other subclasses of subbuffer to override this
        /// method later.  (i.e. you might want a subbuffer of fixed size that won't change
        /// when the parent resizes. If we start using subbuffers to enable curses-like
        /// overlay zones in script displays we'd want that.)
        /// </summary>
        /// <param name="sb">the parent screen buffer that is being resized</param>
        /// <returns>number of lines the parent should probably be shifted down because the subbuffer is now more lines.</returns>
        public virtual int NotifyOfParentResize(IScreenBuffer sb)
        {
            ColumnCount = sb.ColumnCount;
            return ResizeBuffer();
        }

        /// <summary>
        /// Replaces Buffer with one of the new current size, copying old contents
        /// over. The base implementation does no longer wrap over lines in any way, it just truncates/leaves empty space.
        /// </summary>
        /// <returns>(Not implemented, hardcoded to zero) The amount of scrolling to happen as a result</returns>
        protected virtual int ResizeBuffer()
        {
            List<IScreenBufferLine> newBuffer = new List<IScreenBufferLine>();
            
            // First, copy everything that fits from old to new
            for(int row = 0; row < RowCount; ++row)
            {
                newBuffer.Add(new ScreenBufferLine(ColumnCount));
                if (row >= Buffer.Count) break;
                for(int col = 0; col < ColumnCount; ++col)
                {
                    if (col >= Buffer[0].Length) break;
                    newBuffer[row][col] = Buffer[row][col];
                }
            }
            
            // Then maybe append more empty rows if the above wasn't enough to fill the new size:
            while (newBuffer.Count < RowCount)
                newBuffer.Add(new ScreenBufferLine(ColumnCount));
            
            RowCount = newBuffer.Count;

            // Because Buffer is readonly, copy the data from newBuffer into it rather than just resetting the reference to newBuffer:
            Buffer.Clear();
            foreach (var t in newBuffer)
            {
                Buffer.Add(t);
            }
            
            return 0;
        }

        /// <summary>
        /// Get the actual List Buffer. 
        /// WARNING: right now this returns a reference to the internal buffer, 
        /// because this is currently only used to extract the list from a temporary SubBuffer.
        /// </summary>
        /// <returns>A reference to the internal List</returns>
        public List<IScreenBufferLine> GetBuffer()
        {
            return Buffer;
        }

        public virtual string DebugDump()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("DebugDump Subbuffer: Fixed="+Fixed+", Enabled="+Enabled+", RowCount="+RowCount+
                      ", ColumnCount=" + ColumnCount + ", PositionRow="+PositionRow + "\n");
            for (int i = 0; i < Buffer.Count ; ++i)
            {
                sb.Append(" line "+i+" = [");
                for (int j = 0 ; j < Buffer[i].Length ; ++j)
                {
                    char ch = Buffer[i][j];
                    sb.Append(" " + ScreenBuffer.DebugCharacter(ch));
                }
                sb.Append("]\n");
            }
            return sb.ToString();
        }
    }
}
