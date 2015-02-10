using System;
using System.Collections.Generic;
using System.Text;
using kOS.Safe.Screen;
using kOS.Safe.UserIO;

namespace kOS.Safe.Screen
{
    /// <summary>
    /// A snapshot of the frozen screen image as it appears at a moment in time.
    /// </summary>
    public class ScreenSnapShot : IScreenSnapShot
    {
        public List<IScreenBufferLine> Buffer { get; private set;}
        public int TopRow {get; private set;}
        public int CursorColumn {get; private set;}
        public int CursorRow {get; private set;}
        public int RowCount {get; private set;}

        // Tweakable setting:
        // If two diff chunks are side by side and this close or closer, just merge them into one.
        // Its more sane then constantly jumping the cursor around.
        //  (i.e. if old buff is "HELLO, STEVEN, HOW IS THE WEATHER", and the
        //           new buff is "HELLO, STELLA, HAVE SOME OWLS."
        // It would be stupid to drive that change by saying:
        //    Jump cursor to column 10, write "LLA", then jump to column 16, write "AVE", then
        //    jump cursor to column 21, write "OME O", then jump cursor to column 28 and write "LS.".
        // When you could just say:
        //    Jump cursor to column 10, write "LLA, HAVE SOME OWLS.".
        // This setting is how far apart diff sections on the same row have to be before it will
        // perform a cursor jump to get from one to the other, rather than just overwriting the same
        // string as was already there.
        private const int joinDiffDist = 6;

        /// <summary>
        /// Make a screen snapshot of the current state of the screenbuffer.
        /// </summary>
        public ScreenSnapShot(IScreenBuffer fromScreen)
        {
            Buffer = fromScreen.GetBuffer();
            TopRow = fromScreen.TopRow;
            CursorColumn = fromScreen.CursorColumnShow;
            CursorRow = fromScreen.CursorRowShow;
            RowCount = fromScreen.RowCount;
        }
        
        // for me to fill in from scratch when making a deep copy      
        private ScreenSnapShot()
        {
        }
        
        /// <summary>
        /// Make a copy of me for later diffing against.
        /// Almost a deep copy.
        /// <returns>The new copy</param>
        /// </summary>
        public IScreenSnapShot DeepCopy()
        {
            ScreenSnapShot newCopy = new ScreenSnapShot();
            newCopy.TopRow = TopRow;
            newCopy.CursorColumn = CursorColumn;
            newCopy.CursorRow = CursorRow;
            newCopy.RowCount = RowCount;
            
            // This will probably reset the timestamps on the rows, but for our purposes that's actually fine - we call this
            // when we want to get a fully sync'ed copy:
            newCopy.Buffer = new List<IScreenBufferLine>();
            foreach (IScreenBufferLine line in Buffer)
            {
                ScreenBufferLine newLine = new ScreenBufferLine(line.Length);
                newLine.ArrayCopyFrom(line.ToArray(), 0, 0);
                newCopy.Buffer.Add(newLine);
            }
            return newCopy;
        }
        
        /// <summary>
        /// Get a list of the operations that would make a terminal window look like this snapshot
        /// if you assume that beforehand it looked like the older snapshot you pass in.
        /// </summary>
        /// <param name="before">the older snapshot of a screen to diff from</param>
        /// <returns>the string that if output in order, will give you the desired changes</returns>
        public string DiffFrom(IScreenSnapShot older)
        {
            StringBuilder output = new StringBuilder();
            
            int verticalScroll = TopRow - older.TopRow;
            int trackCursorColumn = older.CursorColumn; // track the movements that will occur as the outputs happen.
            int trackCursorRow = older.CursorRow; // track the movements that will occur as the outputs happen.

            // First, output the command to make the terminal scroll to match:
            if (verticalScroll > 0) // scrolling text up (eyeballs panning down)
                output.Append(new String((char)UnicodeCommand.SCROLLSCREENUPONE, verticalScroll)); // A run of scrollup chars
            else if (verticalScroll < 0) // scrolling text down (eyeballs panning up)
                output.Append(new String((char)UnicodeCommand.SCROLLSCREENDOWNONE, -verticalScroll)); // A run of scrolldown chars

            System.Console.WriteLine("eraseme: DiffFrom(): RowCount = " + RowCount);
            // Check each row:
            for (int newRowNum = 0 ; newRowNum < RowCount ; ++newRowNum)
            {
                System.Console.WriteLine("eraseme: DiffFrom(): iteration RowNum = " + newRowNum);
                // Account for the diff due to the scrolling:
                int oldRowNum = newRowNum - verticalScroll;

                IScreenBufferLine newRow = Buffer[newRowNum];
                IScreenBufferLine olderRow = (oldRowNum >= 0 && oldRowNum < older.Buffer.Count) ? older.Buffer[oldRowNum] : new ScreenBufferLine(0);

                System.Console.WriteLine("eraseme: DiffFrom(): Now diffing:");
                System.Console.WriteLine(newRow.ToString());
                System.Console.WriteLine("time="+newRow.LastChangeTime.ToBinary().ToString());
                System.Console.WriteLine(olderRow.ToString());
                System.Console.WriteLine("time="+olderRow.LastChangeTime.ToBinary().ToString());
                
                // If the old row is a dummy pad just made above, or if the new row is newer than the old row, then it needs checking for diffs:
                if (olderRow.Length == 0 || newRow.LastChangeTime > olderRow.LastChangeTime)
                {
                    System.Console.WriteLine("eraseme: DiffFrom(): diffing in detail.");
                    List<DiffChunk> diffs = new List<DiffChunk>();
                    
                    for (int newCol = 0 ; newCol < newRow.Length ; ++newCol)
                    {
                        // Check if they differ, but in a way that treats ' ' and 0x00 as identical, and treats a shorter
                        // old row as if it has been padded with spaces:
                        bool oldRowTooShort = newCol >= olderRow.Length;
                        char newChar = (newRow[newCol] == '\0' ? ' ' : newRow[newCol]);
                        char oldChar = (oldRowTooShort ? ' ' : (olderRow[newCol] == '\0' ? ' ' : olderRow[newCol]));
                        System.Console.WriteLine("eraseme: DiffFrom(): new = "+(int)newChar+", old = "+(int)oldChar);
                        if (newChar != oldChar)
                        {
                            System.Console.WriteLine("eraseme: DiffFrom(): Is inside chunk check.");
                            // Start a new diff chunk if there isn't one yet, or the diff is a long enough distance from the existing one:
                            if (diffs.Count == 0 || diffs[diffs.Count-1].EndCol < newCol - joinDiffDist)
                            {
                                DiffChunk newChunk = new DiffChunk();
                                newChunk.StartCol = newCol;
                                newChunk.EndCol = newCol;
                                diffs.Add(newChunk);
                            }
                            else // stretch the existing diff chunk to here.
                            {
                                diffs[diffs.Count-1].EndCol = newCol;
                            }
                        }
                    }
                    // Now we have a list of diff chunks - next we find the most efficient way to actually output them.
                    
                    string newRowText = newRow.ToString();

                    foreach (DiffChunk diff in diffs)
                    {
                        // If we're lucky enough that the current cursor happens to be right where we need it to
                        // be, some of these are more efficient.  Else it does TELEPORTCURSOR's:
                        
                        // Just one char removed to the left of current cursor pos - do a backspace instead of the ugly work:
                        if (trackCursorRow == newRowNum && (diff.EndCol == trackCursorColumn - 1 && diff.StartCol == diff.EndCol))
                        {
                            output.Append(String.Format("{0}{1}{2}", (char)0x08, newRowText.Substring(diff.StartCol,1), (char)0x08));
                            trackCursorColumn = diff.EndCol;
                            trackCursorRow = newRowNum;
                        }
                        else
                        {
                            // If the change starts right where the cursor happens to be (i.e. when typing, one char
                            // at current cursor position will typically be the only diff from one update to the next),
                            // then don't bother moving the cursor first, else move it to the new pos:
                            string moveString;
                            if (trackCursorRow == newRowNum && diff.StartCol == trackCursorColumn)
                                moveString = "";
                            else
                                moveString = String.Format("{0}{1}{2}",
                                                           (char)UnicodeCommand.TELEPORTCURSOR,
                                                           (char)diff.StartCol,
                                                           (char)newRowNum);
                            
                            output.Append(String.Format("{0}{1}",
                                                        moveString,
                                                        newRowText.Substring(diff.StartCol, diff.EndCol - diff.StartCol + 1)));
 
                            trackCursorColumn = diff.EndCol+1;
                            trackCursorRow = newRowNum;
                        }
                    }
                }
                    
            }
                    
            // Now set the cursor back to the right spot one more time, unless it's already there:
            if (trackCursorRow != CursorRow || trackCursorColumn != CursorColumn)
            {
                output.Append(String.Format("{0}{1}{2}",
                                            (char)UnicodeCommand.TELEPORTCURSOR,
                                            (char)CursorColumn,
                                            (char)CursorRow));
            }
            return output.ToString();
        }
        
        private class DiffChunk
        {
            public int StartCol {get;set;}
            public int EndCol {get;set;}
        }

    }
}
