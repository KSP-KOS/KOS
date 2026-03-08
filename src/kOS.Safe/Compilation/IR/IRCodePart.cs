using System.Collections.Generic;

namespace kOS.Safe.Compilation.IR
{
    public class IRCodePart
    {
        public IRCodePart(CodePart codePart)
        {
            IRBuilder irBuilder = new IRBuilder();
            InitializationCode = irBuilder.Lower(codePart.InitializationCode);
            FunctionsCode = irBuilder.Lower(codePart.FunctionsCode);
            MainCode = irBuilder.Lower(codePart.MainCode);
        }

        public List<BasicBlock> FunctionsCode { get; set; }
        public List<BasicBlock> InitializationCode { get; set; }
        public List<BasicBlock> MainCode { get; set; }
        public void EmitToCodePart(CodePart codePart)
        {
            codePart.FunctionsCode = IREmitter.Emit(FunctionsCode);
            codePart.InitializationCode = IREmitter.Emit(InitializationCode);
            codePart.MainCode = IREmitter.Emit(MainCode);
        }
    }
}
