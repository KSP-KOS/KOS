﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using kOS.Safe.Function;

namespace kOS.Function
{
    public class FunctionManager : IFunctionManager
    {
        private readonly SharedObjects shared;
        private Dictionary<string, FunctionBase> functions;

        public FunctionManager(SharedObjects shared)
        {
            this.shared = shared;
        }

        public void Load()
        {
            functions = new Dictionary<string, FunctionBase>(StringComparer.OrdinalIgnoreCase);
            foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
            {
                var attr = (FunctionAttribute)type.GetCustomAttributes(typeof(FunctionAttribute), true).FirstOrDefault();
                if (attr == null) continue;

                object functionObject = Activator.CreateInstance(type);
                foreach (string functionName in attr.Names)
                {
                    if (functionName != string.Empty)
                    {
                        functions.Add(functionName, (FunctionBase)functionObject);
                    }
                }
            }
        }

        public void CallFunction(string functionName)
        {
            if (!functions.ContainsKey(functionName))
            {
                throw new Exception("Call to non-existent function " + functionName);
            }

            FunctionBase function = functions[functionName];
            function.Execute(shared);
        }

    }
}
