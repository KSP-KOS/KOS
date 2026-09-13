namespace kOS.Safe.Compilation.IR
{
    public interface IActionInstruction
    {
        /// <summary>
        /// Gets a value indicating whether this instruction affects
        /// the wider simulation state or changes the value of an
        /// object.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instruction is inert; otherwise, <c>false</c>.
        /// </value>
        bool IsInert { get; }
    }
}
