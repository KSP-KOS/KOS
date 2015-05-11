namespace kOS.Safe.Encapsulation
{
    /// <summary>
    /// A Dumper Is a class that is part of a suffix graph and wants to control how many levels of the graph we want to travel on a dump
    /// </summary>
    public interface IDumper : ISuffixed
    {
        string[] Dump(int limit, int depth = 0);
    }
}