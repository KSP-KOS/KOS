namespace kOS.Safe.Compilation.KS
{
    /// <summary>
    /// Just a dumb simple tuple of line and column, to be returned in
    /// places where the compiler needs to treat the pair of them like
    /// a single return value from a method.
    /// </summary>
    public class LineCol
    {
        public short Line { get; private set; }
        public short Column { get; private set; }

        public LineCol(short line, short column)
        {
            Line = line;
            Column = column;
        }

        public LineCol(int line, int column)
        {
            Line = (short)line;
            Column = (short)column;
        }

        public static LineCol Unknown()
        {
            return new LineCol(-1,-1);
        }
    }
}
