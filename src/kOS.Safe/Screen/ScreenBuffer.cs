using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using kOS.Safe.UserIO;

namespace kOS.Safe.Screen
{
    public class ScreenBuffer : IScreenBuffer
    {
        private const int DEFAULT_ROWS = 36;
        private const int DEFAULT_COLUMNS = 50;
        private int topRow;
        private readonly List<SubBuffer> subBuffers;

        /// <summary> The main output buffer </summary>
        private readonly PrintingBuffer ScrollingOutput;

        /// <summary> Used to print independently of the main cursor. Always merged into ScrollingOutput, not visible itself. </summary>
        private readonly PrintingBuffer PositionalOutput;

        public Queue<char> CharInputQueue { get; private set; }
        
        public int BeepsPending {get; set;}
        
        // These next 6 things really belong in TermWindow, but then they can't be reached from
        // TerminalStruct over in kOS.Safe because TermWindow is just too full of
        // Unity stuff to move it to kOS.Safe, or even provide all its methods
        // in an ITermWindow interface:
        // ---------------------------------------------------------------------------        
        /// <summary>True means the terminal screen should be shown in reversed colors.</summary>
        public bool ReverseScreen {get; set;}
        /// <summary>True means a beep should make the terminal screen flash silently in lieu of an audio beep.</summary>
        public bool VisualBeep {get; set;}
        public int CharacterPixelWidth { get; set; }
        public int CharacterPixelHeight { get; set; }
        public double Brightness { get; set; }

        
        public int ColumnCount { get; private set; }

        public virtual int CursorRowShow { get { return ScrollingOutput.CursorRow - topRow; } }

        public virtual int CursorColumnShow { get { return ScrollingOutput.CursorColumn; } }

        public int RowCount { get; private set; }

        public int AbsoluteCursorRow
        {
            get { return ScrollingOutput.CursorRow; }
            set { ScrollingOutput.MoveCursor(value, ScrollingOutput.CursorColumn); }
        }

        // Needed so the terminal knows when it's been scrolled, for its diffing purposes.
        public int TopRow { get { return topRow; } }

        protected List<ResizeNotifier> Notifyees { get; set; }

        /// <summary>Delegate prototype expected by AddResizeNotifier</summary>
        /// <param name="sb">This screenbuffer telling the callback who it is</param>
        /// <returns>telling this screenbuffer how many vertical rows to scroll as a result of the resize.</returns>
        public delegate int ResizeNotifier(IScreenBuffer sb);

        public ScreenBuffer()
        {
            Notifyees = new List<ResizeNotifier>();

            subBuffers = new List<SubBuffer>();

            ScrollingOutput = new PrintingBuffer()
            {
                AutoExtend = true,
                WillTruncate = false,
                Enabled = true,
                KeepShortenedLines = true
            };
            AddSubBuffer(ScrollingOutput);

            PositionalOutput = new PrintingBuffer()
            {
                AutoExtend = true
            };
            AddSubBuffer(PositionalOutput);
            
            CharInputQueue = new Queue<char>();

            RowCount = DEFAULT_ROWS;
            ColumnCount = DEFAULT_COLUMNS;
            InitializeBuffer();
        }

        public void AddResizeNotifier(ResizeNotifier notifier)
        {
            if (Notifyees.IndexOf(notifier) < 0)
                Notifyees.Add(notifier);
        }

        public void RemoveResizeNotifier(ResizeNotifier notifier)
        {
            Notifyees.Remove(notifier);
        }

        public void RemoveAllResizeNotifiers()
        {
            Notifyees.Clear();
        }

        public void SetSize(int rows, int columns)
        {
            RowCount = rows;
            ColumnCount = columns;
            int scrollDiff = Notifyees
                .Where(notifier => notifier != null)
                .Sum(notifier => notifier(this));
            ScrollVertical(scrollDiff);
            MoveCursor(ScrollingOutput.CursorRow, ScrollingOutput.CursorColumn); //synchronize cursors
        }

        public virtual int ScrollVertical(int deltaRows)
        {
            return ScrollVerticalInternal(deltaRows);
        }

        /// <summary>
        /// Marks the given section of rows in the buffer as dirty and
        /// in need of a diff check.
        /// </summary>
        /// <param name="startRow">Starting with this row number</param>
        /// <param name="numRows">for this many rows, or up to the max row the buffer has if this number is too large</param>
        public void MarkRowsDirty(int startRow, int numRows)
        {
            ScrollingOutput.MarkRowsDirty(startRow, numRows);
        }

        /// <summary> Move the cursor to the given absolute position, scrolling the screen to make it visible </summary>
        public virtual void MoveCursor(int row, int column)
        {
            ScrollingOutput.MoveCursor(row, column);
            ScrollCursorVisible();
        }

        /// <summary> Print text at the given position, not auto-scrolling the view </summary>
        public virtual void PrintAt(string textToPrint, int row, int column)
        {
            PositionalOutput.MoveCursor(0, column);
            PositionalOutput.PositionRow = row;
            PositionalOutput.Print(StripUnprintables(textToPrint), false);
            PositionalOutput.MergeTo(ScrollingOutput, topRow);
            PositionalOutput.Wipe();
        }

        /// <summary> 
        /// Print text with a trailing newline at the cursor.
        /// Scrolls the view to keep the cursor visible.
        /// </summary>
        public void Print(string textToPrint)
        {
            Print(textToPrint, true);
        }

        /// <summary> Print text at the cursor, scrolling to keep the screen visible </summary>
        public void Print(string textToPrint, bool trailingNewLine)
        {
            ScrollingOutput.Print(StripUnprintables(textToPrint), trailingNewLine);
            ScrollCursorVisible();
        }
        
        public void ClearScreen()
        {
            MoveCursor(0, 0);
            ScrollingOutput.Wipe();
            ScrollingOutput.SetSize(1, ColumnCount);
            PositionalOutput.PositionRow = 0;
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

        /// <summary> Merge all enabled SubBuffers, considering topRow, and return the resulting view </summary>
        public List<IScreenBufferLine> GetBuffer()
        {
            SubBuffer View = new SubBuffer()
            {
                Fixed = true
            };
            View.SetSize(RowCount, ColumnCount);

            // merge sub buffers
            UpdateSubBuffers();
            foreach (SubBuffer subBuffer in subBuffers)
            {
                if (subBuffer.Enabled)
                {
                    subBuffer.MergeTo(View, topRow);
                }
            }

            return View.GetBuffer();
        }

        // This was handy when trying to figure out what was going on.
        public string DebugDump()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("DebugDump ScreenBuffer: RowCount=" + RowCount + ", ColumnCount=" + ColumnCount + ", topRow=" + topRow + "\n");
            foreach (SubBuffer sub in subBuffers)
                sb.Append(sub.DebugDump());
            return sb.ToString();
        }

        protected virtual void UpdateSubBuffers()
        {
            // so subclasses can do something with their subbuffers before they are merged
        }

        private void InitializeBuffer()
        {
            topRow = 0;

            ScrollingOutput.SetSize(1, ColumnCount);
            PositionalOutput.SetSize(1, ColumnCount);
        }

        /// <summary> Scroll so the cursor is visible, if neccessary </summary>
        protected void ScrollCursorVisible()
        {
            if (CursorRowShow < 0)
            {
                ScrollVerticalInternal(CursorRowShow);
            }
            else if (CursorRowShow >= RowCount)
            {
                ScrollVerticalInternal(CursorRowShow - RowCount +1);
            }
        }

        /// <summary>
        /// Scroll the view. This does not scroll past 0 or the end of the last non-fixed buffer.
        /// </summary>
        /// <param name="deltaRows">Rows we want to scroll, positive for down</param>
        /// <returns>Rows actually scrolled, positive for down</returns>
        private int ScrollVerticalInternal(int deltaRows = 1)
        {
            int maxTopRow = 0;
            foreach(SubBuffer buf in subBuffers)
            {
                if(!buf.Fixed && buf.Enabled)
                {
                    int bufMaxTopRow = buf.PositionRow + buf.RowCount - RowCount;
                    if(bufMaxTopRow > maxTopRow)
                    {
                        maxTopRow = bufMaxTopRow;
                    }
                }
            }

            // boundary checks
            if (topRow + deltaRows < 0)
                deltaRows = -topRow;
            else if (topRow + deltaRows > maxTopRow)
                deltaRows = maxTopRow - topRow;
            
            topRow += deltaRows;

            return deltaRows;
        }
        
        /// <summary>
        /// Strip beeps from the text and add them to pending.
        /// Everything else is handled by SubBuffer, which needs at least the newlines.
        /// </summary>
        private string StripUnprintables(string textToPrint)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char ch in textToPrint)
            {
                switch (ch)
                {
                    case (char)0x0007:
                    case (char)UnicodeCommand.BEEP:
                        ++BeepsPending;
                        break;
                    default:
                        sb.Append(ch);
                        break;
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Split a text into lines, considering both newlines in the text, and wrapover.
        /// </summary>
        /// <param name="textToPrint">Text to split</param>
        /// <param name="columnCount">Number of columns in a full line</param>
        /// <param name="cursorColumn">Starting column, i.e. how many columns are already used in the first line</param>
        public static List<string> SplitIntoLines(string textToPrint, int columnCount, int cursorColumn)
        {
            var lineList = new List<string>();
            int availableColumns = columnCount - cursorColumn;

            string[] lines = textToPrint.Trim(new[] { '\r' }).Split('\n');

            foreach (string line in lines)
            {
                string lineToPrint = line.TrimEnd('\r');
                int startIndex = 0;

                while ((lineToPrint.Length - startIndex) > availableColumns)
                {
                    lineList.Add(lineToPrint.Substring(startIndex, availableColumns));
                    startIndex += availableColumns;
                    availableColumns = columnCount;
                }

                lineList.Add(lineToPrint.Substring(startIndex));
                availableColumns = columnCount;
            }

            return lineList;
        }

        /// <summary>
        /// Create debug output for a character
        /// </summary>
        /// <param name="c">Character to debug</param>
        /// <returns>Character if it's definitly printable, the escaped code point otherwise</returns>
        public static string DebugCharacter(char c)
        {
            if ((int)c >= 0x20 && (int)c < 0x80)
            {
                return c.ToString();
            }
            else
            {
                return "\\" + (int)c;
            }
        }
    }
}
