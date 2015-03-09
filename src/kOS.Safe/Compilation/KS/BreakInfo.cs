using System;
using System.Collections.Generic;
using kOS.Safe.Compilation;

namespace kOS.Safe.Compilation.KS
{
    /// <summary>
    /// The information needed to know how to break out of a brace section
    /// back to some outer nesting level, either because it's a BREAK in
    /// a loop, or a RETURN in a function.
    /// (Basically, any time you might be making a forward goto that exits
    /// from one or more nesting levels of {..} sections, you need to know
    /// how far to pop back down to on the local variable context stack, as
    /// well as what instruction number you're jumping to.)
    /// </summary>
    public class BreakInfo
    {
        /// <summary>
        /// What is the nesting level that should be broken back to.
        /// </summary>
        public Int16 NestLevel {get; private set;} // 32767 max recursion supported?  Seems fine.
        
        /// <summary>
        /// Which opcodes are in need of adjusting.
        /// There are two kinds of adjustment:
        /// 1  - if it's a breakout jump of some sort, set it to the proper Instruction Pointer
        /// once the location of the bottom of the block is known.
        /// 2 - if it's an OpcodePopScope, then work out the diff between its
        /// nest level and this break's nest level, and make it pop that many scopes.
        /// </summary>
        public List<Opcode> Opcodes {get; private set;}

        /// <summary>
        /// Make a new break info block, with empty opcode list, given the current brace nest level.
        /// </summary>
        /// <param name="nest">current brace nest level when the break info was set up</param>
        public BreakInfo(int nest)
        {
            NestLevel = (Int16)nest;
            Opcodes = new List<Opcode>();
        }
    }
}
