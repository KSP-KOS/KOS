namespace kOS.Safe.Encapsulation
{
    public delegate TR GlobalSuffixGetDlg<out TR>();
    public delegate bool GlobalSuffixSetDlg<in T>(T value);
    public delegate TR SuffixGetDlg<in T, out TR>(T model);
    public delegate void SuffixSetDlg<in T, in TV>(T model, TV value);
}