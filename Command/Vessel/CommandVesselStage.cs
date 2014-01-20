using System.Text.RegularExpressions;
using kOS.Context;

namespace kOS.Command.Vessel
{
    [Command("STAGE")]
    class CommandVesselStage : Command
    {
        public CommandVesselStage(Match regexMatch, ExecutionContext context) : base(regexMatch, context) { }

        public override void Evaluate()
        {
            Staging.ActivateNextStage();

            State = ExecutionState.DONE;
        }
    }
}