namespace kOS.Safe.Persistence
{
    /// <summary>
    /// Identifies the type of file it is,
    /// (By scanning over the file's first few bytes).
    /// (NOTE: This was called "FileType", but I didn't like the
    /// overloaded meaning of "Type" which also meas a C# Type.)
    /// </summary>
    public enum FileCategory
    {
        /// <summary>
        /// either can't be identified, or file couldn't be opened to try to identify it.
        /// </summary>
        UNKNOWN = 0,

        /// <summary>
        /// The default the type identifier will always assume as long<br/>
        /// as the first few characters are printable ascii.
        /// </summary>
        ASCII, 

        /// <summary>
        /// At the moment we won't be able to detect this<br/>
        /// and it will call scripts just ASCII, but this<br/>
        /// may change later and be used.
        /// </summary>
        KERBOSCRIPT,
                      
        /// <summary>
        /// The ML compiled and packed file that came from a KerboScript.
        /// </summary>
        KSM
    }
}