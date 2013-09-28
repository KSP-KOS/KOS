using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace kOS
{
    public struct kOSExternalFunction
    {
        public String Name;
        public object Parent;
        public String MethodName;
        public int ParameterCount;
        public String regex;

        public kOSExternalFunction(String name, object parent, String methodName, int parameterCount)
        {
            this.Name = name;
            this.Parent = parent;
            this.ParameterCount = parameterCount;
            this.MethodName = methodName;

            this.regex = Utils.BuildRegex(name + "_(" + parameterCount + ")");
        }
    }
}
