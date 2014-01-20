using System.Text.RegularExpressions;

namespace kOS.Command.BasicIO
{
    [Command("TEST *")]
    public class CommandTestKegex : Command
    {
        public CommandTestKegex(Match regexMatch, ExecutionContext context) : base(regexMatch, context) { }

        public override void Evaluate()
        {
            var e = new Expression(RegexMatch.Groups[1].Value, ParentContext);

            StdOut(e.GetValue().ToString());

            State = ExecutionState.DONE;
        }
    }
}