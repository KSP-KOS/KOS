using System.Collections.Generic;

namespace kOS.Safe.Screen
{
    public interface IScreenBuffer
    {
        int CharacterPixelWidth { get; set; }
        int CharacterPixelHeight { get; set; }
        double Brightness { get; set; } // double is overkill, but floats don't work in KSP config.xml files.
        int CursorRowShow { get; }
        int CursorColumnShow { get; }
        int RowCount { get; }
        int ColumnCount { get; }
        int AbsoluteCursorRow { get; set; }
        int BeepsPending {get; set;}
        bool ReverseScreen {get; set;}
        bool VisualBeep {get; set;}
        Queue<char> CharInputQueue { get; }
        int TopRow {get;}
        void SetSize(int rowCount, int columnCount);
        int ScrollVertical(int deltaRows);
        void MoveCursor(int row, int column);
        void PrintAt(string textToPrint, int row, int column);
        void Print(string textToPrint);
        void Print(string textToPrint, bool addNewLine);
        void ClearScreen();
        void AddSubBuffer(SubBuffer subBuffer);
        void RemoveSubBuffer(SubBuffer subBuffer);
        List<IScreenBufferLine> GetBuffer();
        void AddResizeNotifier(ScreenBuffer.ResizeNotifier notifier);
        void RemoveResizeNotifier(ScreenBuffer.ResizeNotifier notifier);
        void RemoveAllResizeNotifiers();
        string DebugDump();
    }
}