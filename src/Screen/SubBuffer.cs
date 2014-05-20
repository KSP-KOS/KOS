using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace kOS.Screen
{
    public class SubBuffer
    {
        public readonly List<char[]> Buffer = new List<char[]>();

        public int RowCount { get; private set; }
        public int ColumnCount { get; private set; }
        public bool Fixed { get; set; }
        public int PositionRow { get; set; }
        public int PositionColumn { get; set; }
        public bool Enabled { get; set; }

        public void SetSize(int rowCount, int columnCount)
        {
            if (rowCount <= ScreenBuffer.MAX_ROWS && columnCount <= ScreenBuffer.MAX_COLUMNS)
            {
                RowCount = rowCount;
                ColumnCount = columnCount;
                InitializeBuffer();
            }
        }

        private void InitializeBuffer()
        {
            Buffer.Clear();

            for (int row = 0; row < RowCount; row++)
            {
                Buffer.Add(new char[ColumnCount]);
            }
        }
    }
}
