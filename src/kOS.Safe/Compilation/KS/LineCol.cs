namespace kOS.Safe.Compilation.KS
{
    /// <summary>
    /// Just a dumb simple tuple of line and column, to be returned in
    /// places where the compiler needs to treat the pair of them like
    /// a single return value from a method.
    /// </summary>
    public class LineCol
    {
        public short Line { get; set; }
        public short Col { get; set; }
        public LineCol(short line, short col)
        {
            Line = line;
            Col = col;
        }
    }
}
