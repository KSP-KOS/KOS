using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;


namespace kOS
{
    [CommandAttribute(@"^CALL ([a-zA-Z0-9]+) ?\((.*?)\)$")]
    public class CommandCallExternal : Command
    {
        public CommandCallExternal(Match regexMatch, ExecutionContext context) : base(regexMatch, context) { }

        public override void Evaluate()
        {
            String name = RegexMatch.Groups[1].Value;
            String paramString = RegexMatch.Groups[2].Value;

            var parameters = new List<String>();

            foreach (String param in Utils.ProcessParams(paramString))
            {
                Expression subEx = new Expression(param, this);
                parameters.Add(subEx.GetValue().ToString());
            }

            CallExternalFunction(name, parameters.ToArray());
            
            State = ExecutionState.DONE;
        }
    }
}
