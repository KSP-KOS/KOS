using System.Collections.Generic;

namespace kOS.Safe.Binding
{
    public interface IBindingManager
    {
        void Load();
        void AddBoundVariable(string name, BindingGetDlg getDelegate, BindingSetDlg setDelegate);
        void AddGetter(string name, BindingGetDlg dlg);
        void AddGetter(IEnumerable<string> names, BindingGetDlg dlg);
        void AddSetter(string name, BindingSetDlg dlg);
        void AddSetter(IEnumerable<string> names, BindingSetDlg dlg);
        void PreUpdate();
        void PostUpdate();
        void ToggleFlyByWire(string paramName, bool enabled);
        void UnBindAll();
        void Dispose();
        void SelectAutopilotMode(string autopilotMode);
    }
}