namespace kOS.UserIO
{
    /// <summary>
    /// Subclass of TerminalUnicodeMapper designed to handle the specifics of
    /// the ANSI terminal control codes.
    /// </summary>
    public class TerminalAnsiMapper : TerminalUnicodeMapper
    {
        public TerminalAnsiMapper(string typeString) : base(typeString)
        {
            // enable this line later after ANSI mapping is filled in.
            // TerminalTypeID = TerminalType.ANSI;
        }

        // TODO - this isn't implemented properly yet.  For now it's just a copy of the default unknown terminal base class.        
    }
}
