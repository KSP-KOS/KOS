using kOS.Context;

namespace kOS.Binding
{
    public class BoundVariable : Variable
    {
        public BindingManager.BindingSetDlg Set;
        public BindingManager.BindingGetDlg Get;

        public override object Value
        {
            get
            {
                return Get(Cpu);
            }
            set
            {
                Set(Cpu, value);
            }
        }

        public CPU Cpu { get; set; }
    }
}