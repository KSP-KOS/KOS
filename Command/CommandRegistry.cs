using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using kOS.Utilities;

namespace kOS.Command
{
    public static class CommandRegistry
    {
        public static List<KeyValuePair<string, Type>> Bindings = new List<KeyValuePair<string, Type>>();

        static CommandRegistry()
        {
            foreach (var t in Assembly.GetExecutingAssembly().GetTypes())
            {
                var attr = (CommandAttribute) t.GetCustomAttributes(typeof (CommandAttribute), true).FirstOrDefault();
                if (attr == null) continue;
                foreach (var s in attr.Values)
                {
                    Bindings.Add(new KeyValuePair<string, Type>(Utils.BuildRegex(s), t));
                }
            }
            //Sorting commands longest to shortest to assure that the more specific commands match first.
            Bindings.Sort((p1, p2) => p2.Key.Length.CompareTo(p1.Key.Length));
        }
    }
}