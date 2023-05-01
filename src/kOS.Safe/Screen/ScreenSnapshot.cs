using System;
using System.Collections.Generic;
using System.Text;
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
        public int CursorRow {get; private set; }
        public bool CursorVisible { get { var row = CursorRow; return row >= 0 && row < RowCount && CursorColumn < Buffer[row].Length; } }
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
        private const int JOIN_DIFF_DIST = 6;

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
        /// A factory that constructs an empty screen buffer of a correct size for the fromScreen
        /// </summary>
        /// <param name="fromScreen">The screen - only used to determine the needed width/height</param>
        /// <returns>An empty snapshot buffer</returns>
        public static ScreenSnapShot EmptyScreen(IScreenBuffer fromScreen)
        {
            ScreenSnapShot newThing = new ScreenSnapShot();
            newThing.TopRow = fromScreen.TopRow;
            newThing.CursorColumn = fromScreen.CursorColumnShow;
            newThing.CursorRow = fromScreen.CursorRowShow;
            newThing.RowCount = fromScreen.RowCount;
            newThing.Buffer = new List<IScreenBufferLine>();
            for (int i = 0; i < newThing.RowCount ; ++i)
                newThing.Buffer.Add(new ScreenBufferLine(fromScreen.ColumnCount));
            return newThing;
        }
        
        /// <summary>
        /// Make a copy of me for later diffing against.
        /// Almost a deep copy.
        /// </summary>
        /// <returns>The new copy</returns>
        public IScreenSnapShot DeepCopy()
        {
            ScreenSnapShot newCopy = new ScreenSnapShot
            {
                TopRow = TopRow,
                CursorColumn = CursorColumn,
                CursorRow = CursorRow,
                RowCount = RowCount,
                Buffer = new List<IScreenBufferLine>()
            };

            // This will probably reset the timestamps on the rows, but for our purposes that's actually fine - we call this
            // when we want to get a fully sync'ed copy:
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
        /// <param name="older">the older snapshot of a screen to diff from</param>
        /// <returns>the string that if output in order, will give you the desired changes</returns>
        public string DiffFrom(IScreenSnapShot older)
        {
            StringBuilder output = new StringBuilder();
            
            int verticalScroll = TopRow - older.TopRow;
            int trackCursorColumn = older.CursorColumn; // track the movements that will occur as the outputs happen.
            int trackCursorRow = older.CursorRow; // track the movements that will occur as the outputs happen.

            //invalidate the cursor tracking because the last send cursor position will not actually be there (see below)
            if(!older.CursorVisible)
            {
                trackCursorColumn = -100;
                trackCursorRow = -100;
            }

            // First, output the command to make the terminal scroll to match:
            if (verticalScroll > 0) // scrolling text up (eyeballs panning down)
                output.Append(new String((char)UnicodeCommand.SCROLLSCREENUPONE, verticalScroll)); // A run of scrollup chars
            else if (verticalScroll < 0) // scrolling text down (eyeballs panning up)
                output.Append(new String((char)UnicodeCommand.SCROLLSCREENDOWNONE, -verticalScroll)); // A run of scrolldown chars

            // Check each row:
            for (int newRowNum = 0 ; newRowNum < RowCount ; ++newRowNum)
            {
                // Account for the diff due to the scrolling:
                int oldRowNum = newRowNum + verticalScroll;

                IScreenBufferLine newRow = Buffer[newRowNum];
                bool oldRowExists = (oldRowNum >= 0 && oldRowNum < older.Buffer.Count);
                IScreenBufferLine olderRow = oldRowExists ? older.Buffer[oldRowNum] : new ScreenBufferLine(0);
                
                // if new row is an empty dummy, then pad it out so it gets properly diffed against the old row:
                if (newRow.Length == 0)
                    newRow = new ScreenBufferLine(olderRow.Length);
                // If the old row is a dummy pad or if the new row is newer than the old row, then it needs checking for diffs:
                if (olderRow.Length == 0 || newRow.LastChangeTick > olderRow.LastChangeTick)
                {
                    List<DiffChunk> diffs = new List<DiffChunk>();
                    
                    for (int newCol = 0 ; newCol < newRow.Length ; ++newCol)
                    {
                        // Check if they differ, but in a way that treats ' ' and 0x00 as identical, and treats a shorter
                        // old row as if it has been padded with spaces:
                        bool oldRowTooShort = newCol >= olderRow.Length;
                        char newChar = (newRow[newCol] == '\0' ? ' ' : newRow[newCol]);
                        char oldChar = (oldRowTooShort ? ' ' : (olderRow[newCol] == '\0' ? ' ' : olderRow[newCol]));
                        if (newChar != oldChar)
                        {
                            // Start a new diff chunk if there isn't one yet, or the diff is a long enough distance from the existing one:
                            if (diffs.Count == 0 || diffs[diffs.Count-1].EndCol < newCol - JOIN_DIFF_DIST)
                            {
                                DiffChunk newChunk = new DiffChunk
                                {
                                    StartCol = newCol, 
                                    EndCol = newCol
                                };
                                diffs.Add(newChunk);
                            }
                            else // stretch the existing diff chunk to here.
                            {
                                diffs[diffs.Count-1].EndCol = newCol;
                            }
                        }
                    }
                    
                    string newRowText = newRow.ToString();

                    foreach (DiffChunk diff in diffs)
                    {
                        /* comment-out -----------------
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
                        --------------------------- */
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
                            
                            // content = the bit of string to print at this location, with nulls made into spaces so the 
                            // telnet terminal will print correctly.
                            string content = newRowText.Substring(diff.StartCol, diff.EndCol - diff.StartCol + 1).Replace('\0',' ');

                            output.Append(String.Format("{0}{1}", moveString, content));
 
                            trackCursorColumn = diff.EndCol+1;
                            trackCursorRow = newRowNum;
                        /* comment-out -----------------
                        }
                        --------------------------- */
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

            //Do cursor (un)hiding
            if (CursorVisible != older.CursorVisible)
            {
                if(CursorVisible)
                {
                    output.Append((char)UnicodeCommand.SHOWCURSOR);
                }
                else
                {
                    output.Append((char)UnicodeCommand.HIDECURSOR);
                }
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
