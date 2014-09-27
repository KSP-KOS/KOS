namespace kOS.Safe.Encapsulation
{

    public class GlobalSuffix<TR> : ISuffix
    {
        private readonly GlobalSuffixGetDlg<TR> getter;

        public GlobalSuffix(GlobalSuffixGetDlg<TR> getter)
        {
            this.getter = getter;
        }

        public object Get()
        {
            return getter.Invoke();
        }

        public string Description { get; private set; }
    }
}