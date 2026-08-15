using System;
using System.Collections.Generic;

namespace kOS.Safe.Compilation.IR
{
    public interface ICodeComponent
    {
        IRCodePart CodePart { get; }
        List<BasicBlock> Blocks { get; set; }
        BasicBlock RootBlock { get; set; }
        BasicBlock TerminalBlock { get; set; }
    }
}
