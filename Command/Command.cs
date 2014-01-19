using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Reflection;

namespace kOS.Command
{
    public class CommandAttribute : Attribute
    {
        public string[] Values { get; set; }
        public CommandAttribute(params string[] values) { Values = values; }

        public override String ToString()
        {
            return String.Join(",", Values);
        }
    }

    public static class CommandRegistry
    {
        public static Dictionary<String, Type> Bindings = new Dictionary<string, Type>();

        static CommandRegistry()
        {
            foreach (var t in Assembly.GetExecutingAssembly().GetTypes())
            {
                var attr = (CommandAttribute)t.GetCustomAttributes(typeof(CommandAttribute), true).FirstOrDefault();
                if (attr == null) continue;
                foreach (var s in attr.Values)
                {
                    Bindings.Add(Utils.BuildRegex(s), t);
                }
            }
        }
    }

    public abstract class Command : ExecutionContext
    {
        public float Time;
        public float WaitTime = 0;
        public String Input;
        public Match RegexMatch;
        public String InstanceName;

        protected Command(Match regexMatch, ExecutionContext context) : base(context)
        {
            Input = regexMatch.ToString();
            RegexMatch = regexMatch;
        }

        protected Command(String input, ExecutionContext context) : base(context)
        {
            Input = input;
        }

        public abstract void Evaluate();

        public static Command Get(String input, ExecutionContext context, int line)
        {
            try
            {
                var retCommand = Get(input, context);
                retCommand.Line = line;

                return retCommand;
            }
            catch (kOSException e)
            {
                e.LineNumber = line;
                throw;
            }
        }

        public static Command Get(String input, ExecutionContext context)
        {
            input = input.Trim();//.Replace("\n", " ");

            foreach (var kvp in CommandRegistry.Bindings)
            {
                var match = Regex.Match(input, kvp.Key, RegexOptions.IgnoreCase);
                if (!match.Success) continue;
                var command = (Command)Activator.CreateInstance(kvp.Value, match, context);
                return command;
            }

            throw new kOSException("Syntax Error.", context);
        }

        public virtual void Refresh()
        {
            State = ExecutionState.NEW;
        }

        public override void Lock(Command command)
        {
            if (ParentContext != null) ParentContext.Lock(command);
        }

        public override void Lock(string name, Expression expression)
        {
            if (ParentContext != null) ParentContext.Lock(name, expression);
        }

        public override void Unlock(Command command)
        {
            if (ParentContext != null) ParentContext.Unlock(command);
        }

        public override void Unlock(string name)
        {
            if (ParentContext != null) ParentContext.Unlock(name);
        }
    }
}
