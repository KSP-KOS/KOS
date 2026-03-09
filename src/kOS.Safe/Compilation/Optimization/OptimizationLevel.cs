namespace kOS.Safe.Compilation
{
    /*
     * O1:
     *  Replace CONSTANT: values with the constant
     *  Replace ship fields with their alias
     *  Constant folding
     *  Dead code elimination
     *  
     *  Constant propagation
     *  Replace lex indexing with string constant with suffixing where possible
     * O2:
     *  Common expression elimination
     *      Particularly: Any expression of 3 opcodes used more than twice,
     *      or any expression of >3 opcodes used more than once
     *  Replace parameterless suffix method calls with get member (this feels like cheating...)
     *  Boolean logical simplification
     *  Code motion - moving expressions outside loops if they do not depend on loop variables
     *  Replace X * X * ... * X with X^N
     *  Carry's - moving the N-1 D lookup of >1D lists outside the innermost loop
     *  Loop jamming? (Combining adjacent loops into one)
     *  Unswitching (moving conditional evaluation outside the loop) - low priority
     *  Linear function test replacement - low priority
     * O3:
     *  Local function inlining
     *  Constant propagation to local functions
     *  Loop stack manipulation (delayed setting of either index or aggregator)
     * O4:
     *  Constant loop unrolling
     */
    public enum OptimizationLevel : int
    {
        None = 0,  // No changes whatsoever
        Minimal = 1,    // Only changes that trim opcodes without changing any flow
        Balanced = 2,   // Only changes that trim opcodes without increasing file size
        Aggressive = 3, // Favor trimming opcodes over file size.
        Extreme = 4     // Include constant loop unrolling
    }
}
