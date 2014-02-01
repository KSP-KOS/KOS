using kOS.Context;

namespace kOS.Binding
{
    public class BoundVariable : Variable
    {
        public BindingGetDlg Get;
        public BindingSetDlg Set;

        public override object Value
        {
            get { return Get(Cpu); }
            set { Set(Cpu, value); }
        }

        public ICPU Cpu { get; set; }
    }
}