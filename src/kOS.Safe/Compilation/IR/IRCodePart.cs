using System;
using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Compilation.KS;

namespace kOS.Safe.Compilation.IR
{
    public class IRCodePart
    {
        public List<BasicBlock> MainCode { get; set; }
        public List<IRFunction> Functions { get; set; }
        public List<BasicBlock> RootBlocks { get; } = new List<BasicBlock>();
        public List<BasicBlock> Blocks { get; } = new List<BasicBlock>();

        public IRCodePart(CodePart codePart, List<UserFunction> userFunctions)
        {
            if (codePart.InitializationCode.Count > 0)
                throw new ArgumentException($"{nameof(codePart)} has initialization code and is structured unexpectedly.");
            if (codePart.FunctionsCode.Count > 0)
                throw new ArgumentException($"{nameof(codePart)} has function code and is structured unexpectedly.");
            
            IRBuilder builder = new IRBuilder();
            MainCode = builder.Lower(codePart.MainCode);
            Functions = userFunctions.Select(f => new IRFunction(builder, f)).ToList();

            Blocks.AddRange(MainCode);
            if (MainCode.Count > 0)
                RootBlocks.Add(MainCode[0]);
            foreach (IRFunction function in Functions)
            {
                Blocks.AddRange(function.InitializationCode);
                if (function.InitializationCode.Count > 0)
                    RootBlocks.Add(function.InitializationCode[0]);
                foreach (IRFunction.IRFunctionFragment fragment in function.Fragments)
                {
                    Blocks.AddRange(fragment.FunctionCode);
                    if (fragment.FunctionCode.Count > 0)
                        RootBlocks.Add(fragment.FunctionCode[0]);
                }
            }
        }

        public void EmitCode(CodePart codePart)
        {
            IREmitter emitter = new IREmitter();
            foreach (IRFunction function in Functions)
            {
                function.EmitCode(emitter);
            }
            codePart.MainCode = emitter.Emit(MainCode);
        }

        public class IRFunction
        {
            private readonly UserFunction function;
            private readonly List<UserFunctionCodeFragment> userFunctionFragments;

            public string Identifier => function.Identifier;
            public List<BasicBlock> InitializationCode { get; set; }
            private readonly Dictionary<UserFunctionCodeFragment, IRFunctionFragment> fragments = new Dictionary<UserFunctionCodeFragment, IRFunctionFragment>();
            public IReadOnlyCollection<IRFunctionFragment> Fragments => fragments.Values;

            public IRFunction(IRBuilder builder, UserFunction function)
            {
                this.function = function;
                InitializationCode = builder.Lower(function.InitializationCode);
                userFunctionFragments = function.PeekNewCodeFragments().ToList();
                foreach (UserFunctionCodeFragment fragment in userFunctionFragments)
                {
                    fragments.Add(fragment, new IRFunctionFragment(builder, fragment));
                }
                userFunctionFragments.Reverse();
            }

            public void EmitCode(IREmitter emitter)
            {
                function.InitializationCode.Clear();
                function.InitializationCode.AddRange(emitter.Emit(InitializationCode));
                foreach (UserFunctionCodeFragment fragment in userFunctionFragments)
                {
                    fragments[fragment].EmitCode(emitter);
                }
            }

            public class IRFunctionFragment
            {
                private readonly UserFunctionCodeFragment fragment;
                public List<BasicBlock> FunctionCode { get; set; }
                public IRFunctionFragment(IRBuilder builder, UserFunctionCodeFragment codeFragment)
                {
                    fragment = codeFragment;
                    FunctionCode = builder.Lower(codeFragment.Code);
                }
                public void EmitCode(IREmitter emitter)
                {
                    fragment.Code.Clear();
                    fragment.Code.AddRange(emitter.Emit(FunctionCode));
                }
            }
        }
    }
}
