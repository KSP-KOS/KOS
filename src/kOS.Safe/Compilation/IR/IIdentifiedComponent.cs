namespace kOS.Safe.Compilation.IR
{
    public interface IIdentifiedComponent
    {
        /// <summary>
        /// Gets the identifier string for this component.
        /// </summary>
        string Identifier { get; }
    }
    public interface ILabeledComponent : IIdentifiedComponent
    {
        /// <summary>
        /// Gets the starting label, which functions as the
        /// true identifier in opcode form.
        /// </summary>
        string Label { get; }
    }
}
