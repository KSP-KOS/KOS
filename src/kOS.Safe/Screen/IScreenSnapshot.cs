using System.Collections.Generic;

namespace kOS.Safe.Screen
{
    public interface IScreenSnapShot
    {
        List<IScreenBufferLine> Buffer {get;}
        int TopRow {get;}
        int CursorColumn {get;}
        int CursorRow {get;}
        int RowCount {get;}
        bool CursorVisible { get; }

        string DiffFrom(IScreenSnapShot older);
        
        IScreenSnapShot DeepCopy();
    }
}