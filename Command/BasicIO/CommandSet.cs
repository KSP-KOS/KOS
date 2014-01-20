using System.Text.RegularExpressions;
using kOS.Debug;

namespace kOS.Command.BasicIO
{
    [Command("SET ~ TO *")]
    public class CommandSet : Command
    { 
        public CommandSet(Match regexMatch, ExecutionContext context) : base(regexMatch, context) { }

        public override void Evaluate()
        {
            var targetTerm = new Term(RegexMatch.Groups[1].Value);
            var e = new Expression(RegexMatch.Groups[2].Value, ParentContext);

            if (targetTerm.Type == Term.TermTypes.STRUCTURE)
            {
                var baseObj = new Expression(targetTerm.SubTerms[0], ParentContext).GetValue();

                var obj = baseObj as SpecialValue;
                if (obj != null)
                {
                    if (obj.SetSuffix(targetTerm.SubTerms[1].Text.ToUpper(), e.GetValue()))
                    {
                        State = ExecutionState.DONE;
                        return;
                    }
                    throw new KOSException("Suffix '" + targetTerm.SubTerms[1].Text + "' doesn't exist or is read only", this);
                }
                throw new KOSException("Can't set subvalues on a " + Expression.GetFriendlyNameOfItem(baseObj), this);
            }
            var v = FindOrCreateVariable(targetTerm.Text);

            if (v == null) return;
            v.Value = e.GetValue();
            State = ExecutionState.DONE;
        }
    }
}