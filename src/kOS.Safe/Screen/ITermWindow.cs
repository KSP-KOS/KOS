using kOS.Safe.Persistence;

namespace kOS.Safe.Screen
{
    public interface ITermWindow
    {
        void OpenPopupEditor( Volume v, string fName );
        void Open();
        void Close();
        void Toggle();
        bool IsOpen();
        bool ShowCursor { get; set; }
        bool IsPowered { get; set; }
    }
}