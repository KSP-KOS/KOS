using kOS.Safe.Encapsulation;
using System.Linq;

namespace kOS.Safe.Function.Collections
{
    [Function("lex", "lexicon")]
    public class FunctionLexicon : SafeFunctionBase
    {
        public override void Execute(SharedObjects shared)
        {
            Structure[] argArray = new Structure[CountRemainingArgs(shared)];
            for (int i = argArray.Length - 1; i >= 0; --i)
                argArray[i] = PopStructureAssertEncapsulated(shared); // fill array in reverse order because .. stack args.
            AssertArgBottomAndConsume(shared);
            var lexicon = new Lexicon(argArray.ToList());
            ReturnValue = lexicon;
        }
    }
}