namespace kOS.Safe.Encapsulation.Suffixes
{
    public delegate TReturn GlobalSuffixGetDlg<out TReturn>();
    public delegate bool GlobalSuffixSetDlg<in T>(T value);
    public delegate TReturn SuffixGetDlg<in T, out TReturn>(T model);
    public delegate void SuffixSetDlg<in T, in TV>(T model, TV value);
}