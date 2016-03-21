using System.Text;

namespace kOS.Safe.Function.Misc
{
    [Function("profileresult")]
    public class ProfileResult : SafeFunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            AssertArgBottomAndConsume(shared);
            if (shared.Cpu.ProfileResult == null || shared.Cpu.ProfileResult.Count == 0)
            {
                ReturnValue = "<no profile data available>";
                return;
            }
            StringBuilder sb = new StringBuilder();
            foreach (string textLine in shared.Cpu.ProfileResult)
            {
                if (sb.Length > 0)
                    sb.Append("\n");
                sb.Append(textLine);
            }
            ReturnValue = sb.ToString();
        }
    }
}