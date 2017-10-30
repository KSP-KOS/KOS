using kOS.Safe.Persistence;

namespace kOS.Safe.Screen
{
    public interface ITermWindow
    {
        void OpenPopupEditor(Volume v, GlobalPath path);
        void Open();
        void Close();
        void Toggle();
        bool IsOpen { get; }
        bool ShowCursor { get; set; }
        bool IsPowered { get; set; }
    }
}