using System.Collections.Generic;
using System.Text;

namespace kOS.Safe.Screen
{
    public class SubBuffer
    {
        public readonly List<IScreenBufferLine> Buffer = new List<IScreenBufferLine>();

        public int RowCount { get; private set; }
        public int ColumnCount { get; private set; }
        public bool Fixed { get; set; }
        public int PositionRow { get; set; }
        public int PositionColumn { get; set; }
        public bool Enabled { get; set; }
        public bool WillTruncate { get; set; }

        /// <summary>
        /// Stretch/shrink the subbuffer to a new size, trying as best as possible to
        /// preserve existing contents, including wrapping lines if needed.  Note that
        /// if the new size is too small to hold the contents, and WillTruncate is false,
        /// then it will refuse to make it that small, and will use more rows than you
        /// requested as needed to preserve contents.
        /// You can override this by setting this SubBuffer's WillTrungate property to true.
        /// </summary>
        /// <param name="rowCount">Requested new number of rows (only guaranteed to be obeyed if truncateOkay is true)</param>
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
        /// over.
        /// </summary>
        /// <returns>The buffer.</returns>
        protected virtual int ResizeBuffer()
        {
            // This method also is being left open for potential overriding in subclasses
            // if we want subbuffers that don't behave this way.  The reason is that
            // this behavior is very specific to implementing a command line editor, and
            // the assumption that the entire subbuffer was meant to 'flow' and wrap as one long line.
            // That assumption may be very different for other cases, if any exist in the future.

            List<IScreenBufferLine> newBuffer = new List<IScreenBufferLine>();
            
            int newRow = 0;
            int oldRow = 0;
            int newCol = 0;
            bool isFull = false;

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
                    ++newCol;
                    ++oldCol;
                }
                ++oldRow;
            }
            
            // Then maybe append more empty rows if the above wasn't enough to fill the new size:
            while (newBuffer.Count < RowCount)
                newBuffer.Add(new ScreenBufferLine(ColumnCount));
            
            // Reset the subbuffer row size since the new size might have had to grow (i.e. shrinking the width of a 80 col to 60 col so wrap text added a row.)
            // int scrollDiff = (newBuffer.Count - RowCount); - this logic not quite working - disabled for now.

            RowCount = newBuffer.Count;

            // Because Buffer is readonly, copy the data from newBuffer into it rather than just resetting the reference to newBuffer:
            Buffer.Clear();
            foreach (var t in newBuffer)
            {
                Buffer.Add(t);
            }

            // return Fixed ? 0 : scrollDiff; - this logic not quite working - disabled for now.
            return 0;
        }
        
        public string DebugDump()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("DebugDump Subbuffer: Fixed="+Fixed+", Enabled="+Enabled+", RowCount="+RowCount+
                      ", ColumnCount=" + ColumnCount + ", PositionRow="+PositionRow + ", PositionCol="+PositionColumn+"\n");
            for (int i = 0; i < Buffer.Count ; ++i)
            {
                sb.Append(" line "+i+" = [");
                for (int j = 0 ; j < Buffer[i].Length ; ++j)
                {
                    char ch = Buffer[i][j];
                    sb.Append((int)ch < 32 ? (" \\"+(int)ch) : (" "+ch) );
                }
                sb.Append("]\n");
            }
            return sb.ToString();
        }
    }
}
