using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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

            foreach (String param in processParams(paramString))
            {
                Expression subEx = new Expression(param, this);
                parameters.Add(subEx.GetValue().ToString());
            }

            CallExternalFunction(name, parameters.ToArray());
            
            State = ExecutionState.DONE;
        }

        public string[] processParams(string input)
        {
            String buffer = "";
            List<String> output = new List<string>();

            for (var i = 0; i < input.Length; i++)
            {
                char c = input[i];

                if (c == '\"')
                {
                    var prevI = i;
                    i = Expression.FindEndOfString(input, i + 1);
                    buffer += input.Substring(prevI, i - prevI + 1);
                }
                else
                {
                    if (c == ',')
                    {
                        output.Add(buffer.Trim());
                        buffer = "";
                    }
                    else
                    {
                        buffer += c;
                    }
                }
            }

            if (buffer.Trim().Length > 0) output.Add(buffer.Trim());

            return output.ToArray();
        }
    }
}
