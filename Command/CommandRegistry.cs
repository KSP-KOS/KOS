using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using kOS.Utilities;

namespace kOS.Command
{
    public static class CommandRegistry
    {
        public static Dictionary<string, Type> Bindings = new Dictionary<string, Type>();

        static CommandRegistry()
        {
            foreach (var t in Assembly.GetExecutingAssembly().GetTypes())
            {
                var attr = (CommandAttribute) t.GetCustomAttributes(typeof (CommandAttribute), true).FirstOrDefault();
                if (attr == null) continue;
                foreach (var s in attr.Values)
                {
                    Bindings.Add(Utils.BuildRegex(s), t);
                }
            }
        }
    }
}