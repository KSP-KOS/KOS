namespace kOS.Safe.Encapsulation.Suffixes
{
    public delegate TReturn StaticSuffixGetDlg<out TReturn>();
    public delegate bool StaticSuffixSetDlg<in TParam>(TParam value);
    public delegate TReturn SuffixGetDlg<in TParam, out TReturn>(TParam model);
    public delegate void SuffixSetDlg<in TParam, in TValue>(TParam model, TValue value);
}