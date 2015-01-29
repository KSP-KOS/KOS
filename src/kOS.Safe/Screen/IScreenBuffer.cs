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
        void Print(string textToPrint, bool addNewLine);
        void ClearScreen();
        void AddSubBuffer(SubBuffer subBuffer);
        void RemoveSubBuffer(SubBuffer subBuffer);
        List<char[]> GetBuffer();
        void AddResizeNotifier(ScreenBuffer.ResizeNotifier notifier);
        void RemoveResizeNotifier(ScreenBuffer.ResizeNotifier notifier);
        string DebugDump();
    }
}