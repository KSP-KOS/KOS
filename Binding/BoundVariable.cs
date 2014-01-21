using kOS.Context;

namespace kOS.Binding
{
    public class BoundVariable : Variable
    {
        public BindingSetDlg Set;
        public BindingGetDlg Get;

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

        public ICPU Cpu { get; set; }
    }
}