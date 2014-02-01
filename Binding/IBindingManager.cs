using kOS.Context;

namespace kOS.Binding
{
    public interface IBindingManager : IUpdatable
    {
        ICPU Cpu { get; set; }
        void AddGetter(string name, BindingGetDlg dlg);
        void AddSetter(string name, BindingSetDlg dlg);
    }

    public interface IUpdatable
    {
        void Update(float time);
    }
}