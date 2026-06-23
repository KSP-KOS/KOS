#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace kOS.Safe.Compilation
#pragma warning restore IDE0130 // Namespace does not match folder structure
{
    /*
     * O0:
     * -2000. Conversion to SSA (analysis pass)
     * -1000. Type Propagation
     *  
     * O1:
     * -1000. SCCP with Type Propagation
     *  10. Suffix replacement:
     *          Replace CONSTANT: values with the constant
     *          Replace ship fields with their alias
     *  20. Constant propagation
     *  30. Constant folding
     *  1050. Peephole optimizations:
     *          Replace lex indexing with string constant with suffixing where possible
     *          Replace parameterless suffix method calls with get member (this feels like cheating...)
     *          Replace calls to VectorDotProduct with multiplication
     *          Replace --X with X
     *          Replace !!X with X
     *          Branch logical simplification (e.g. !X branch = X branch!)
     *          Algebraic simplification (e.g. A*B+A*C = A*(B+C), A+-B=A-B, A--B=A+B, -A+B=B-A, X*X*...*X=N^X)
     *  32000. Remove unnecessary scope pushes/pops
     *  32767. Block ordering to minimize unconditional jumps. Also removes blocks that are not executable.
     *  
     * O2:
     * -10000. Loop condition reorganization
     *  32100. Common expression elimination
     *          Particularly: Any expression of 3 opcodes used more than twice,
     *          or any expression of >3 opcodes used more than once
     *  2100. Code motion - moving expressions outside loops if they do not depend on loop variables
     *  
     *  Loop jamming? (Combining adjacent loops into one)
     *  Unswitching (moving conditional evaluation outside the loop) - low priority
     *  Linear function test replacement - low priority
     *  
     * O3:
     *  2000. Local function inlining (also calls constant folding again on the affected blocks)
     *  21.   Local variable propagation to local functions (mostly useful for constants)
     *  
     *  3500. Loop stack manipulation (delayed setting of either index or aggregator)
     *  
     * O4:
     *  4000. Constant length loop unrolling
     */
    public enum OptimizationLevel : int
    {
        None = 0,       // No changes whatsoever
        Minimal = 1,    // Only changes that trim opcodes without changing any flow
        Balanced = 2,   // Only changes that trim opcodes without increasing file size
        Aggressive = 3, // Favor trimming opcodes over file size.
        Extreme = 4     // Include constant loop unrolling
    }
}
