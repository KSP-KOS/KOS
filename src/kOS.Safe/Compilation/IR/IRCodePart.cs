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
                foreach (IRFunction.IRFunctionFragment fragment in function.Fragments.Values)
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
            public IRFunction(IRBuilder builder, UserFunction function)
            {
                this.function = function;
                InitializationCode = builder.Lower(function.InitializationCode);
                fragments = function.PeekNewCodeFragments().ToList();
                foreach (UserFunctionCodeFragment fragment in fragments)
                {
                    Fragments.Add(fragment, new IRFunctionFragment(builder, fragment));
                }
                fragments.Reverse();
            }

            public void EmitCode(IREmitter emitter)
            {
                function.InitializationCode.Clear();
                function.InitializationCode.AddRange(emitter.Emit(InitializationCode));
                foreach (UserFunctionCodeFragment fragment in fragments)
                {
                    Fragments[fragment].EmitCode(emitter);
                }
            }

            public List<BasicBlock> InitializationCode { get; set; }
            public Dictionary<UserFunctionCodeFragment, IRFunctionFragment> Fragments { get; } = new Dictionary<UserFunctionCodeFragment, IRFunctionFragment>();
            private readonly List<UserFunctionCodeFragment> fragments;
            public class IRFunctionFragment
            {
                public List<BasicBlock> FunctionCode { get; set; }
                private readonly UserFunctionCodeFragment fragment;
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
