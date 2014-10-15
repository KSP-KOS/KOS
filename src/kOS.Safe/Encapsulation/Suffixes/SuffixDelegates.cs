namespace kOS.Safe.Encapsulation.Suffixes
{
    public delegate TReturn StaticSuffixGetDlg<out TReturn>();
    public delegate bool StaticSuffixSetDlg<in TParam>(TParam value);
    public delegate TReturn SuffixGetDlg<out TReturn>();
    public delegate void SuffixSetDlg<in TValue>(TValue value);
}