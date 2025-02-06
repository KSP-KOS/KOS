using kOS.Safe.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace kOS.Safe.Function
{
    [AssemblyWalk(AttributeType = typeof(FunctionAttribute), InherritedType = typeof(SafeFunctionBase), StaticRegisterMethod = "RegisterMethod")]
    public class FunctionManager : IFunctionManager
    {
        private readonly SafeSharedObjects shared;
        private Dictionary<string, SafeFunctionBase> functions;
        private static readonly Dictionary<FunctionAttribute, Type> rawAttributes = new Dictionary<FunctionAttribute, Type>();

        public FunctionManager(SafeSharedObjects shared)
        {
            this.shared = shared;
            Load();
        }

        public void Load()
        {
            functions = new Dictionary<string, SafeFunctionBase>(StringComparer.OrdinalIgnoreCase);
            foreach (FunctionAttribute attr in rawAttributes.Keys)
            {
                var type = rawAttributes[attr];
                if (attr == null || type == null) continue;
                object functionObject = Activator.CreateInstance(type);
                foreach (string functionName in attr.Names)
                {
                    if (functionName != string.Empty)
                    {
                        functions.Add(functionName, (SafeFunctionBase)functionObject);
                    }
                }
            }
        }

        public static void RegisterMethod(Attribute attr, Type type)
        {
            var funcAttr = attr as FunctionAttribute;
            if (funcAttr != null && !rawAttributes.ContainsKey((funcAttr)))
            {
                rawAttributes.Add(funcAttr, type);
            }
        }

        public void CallFunction(string functionName)
        {
            if (!functions.ContainsKey(functionName))
            {
                throw new Exception("Call to non-existent function " + functionName);
            }

            SafeFunctionBase function = functions[functionName];
            function.Execute(shared);
            if (function.UsesAutoReturn)
                shared.Cpu.PushArgumentStack(function.ReturnValue);
            function.ReturnValue = null;
        }

        /// <summary>
        /// Find out if the function with the name given exists already in the built-in hardcoded functions set
        /// (as opposed to the user functions).
        /// </summary>
        /// <param name="functionName">check if this function exists</param>
        /// <returns>true if it does exist (as a built-in, not as a user function)</returns>
        public bool Exists(string functionName)
        {
            return functions.ContainsKey(functionName);
        }
    }
}