using System.Text.RegularExpressions;

namespace kOS.Command
{
    [Command("ADD *")]
    public class CommandAddObjectToVessel : Command
    {
        public CommandAddObjectToVessel(Match regexMatch, ExecutionContext context) : base(regexMatch, context) { }

        public override void Evaluate()
        {
            Expression ex = new Expression(RegexMatch.Groups[1].Value, this);
            object obj = ex.GetValue();

            if (obj is kOS.Node)
            {
                ((Node)obj).AddToVessel(Vessel);
            }
            else
            {
                throw new kOSException("Supplied object ineligible for adding", this);
            }

            State = ExecutionState.DONE;
        }
    }
}