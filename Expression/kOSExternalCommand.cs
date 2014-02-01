using kOS.Utilities;

namespace kOS.Expression
{
    public struct KOSExternalFunction
    {
        public KOSExternalFunction(string name, object parent, string methodName, int parameterCount) : this()
        {
            Name = name;
            Parent = parent;
            ParameterCount = parameterCount;
            MethodName = methodName;

            Regex = Utils.BuildRegex(name + "_(" + parameterCount + ")");
        }

        public string Name { get; private set; }
        public object Parent { get; private set; }
        public string MethodName { get; private set; }
        public int ParameterCount { get; private set; }
        public string Regex { get; private set; }
    }
}