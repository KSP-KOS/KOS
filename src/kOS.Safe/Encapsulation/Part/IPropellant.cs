namespace kOS.Safe.Encapsulation.Part
{
    public interface IPropellant : IResource
    {
        float Density { get; }
        float Ratio { get; }
        string FlowMode { get; }
    }

    /// <summary>
    /// Information common to any resource
    /// </summary>
    public interface IResource
    {
        string Name { get; }
        double Amount { get; }
        double Capacity { get; }
        ListValue Parts { get; }
    }
}