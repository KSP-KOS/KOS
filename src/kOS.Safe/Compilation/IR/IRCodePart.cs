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
        public List<IRTrigger> Triggers { get; set; }
        public List<BasicBlock> RootBlocks { get; } = new List<BasicBlock>();
        public List<BasicBlock> Blocks { get; } = new List<BasicBlock>();

        public IRCodePart(CodePart codePart, List<UserFunction> userFunctions, List<Trigger> triggers)
        {
            if (codePart.InitializationCode.Count > 0)
                throw new ArgumentException($"{nameof(codePart)} has initialization code and is structured unexpectedly.");
            if (codePart.FunctionsCode.Count > 0)
                throw new ArgumentException($"{nameof(codePart)} has function code and is structured unexpectedly.");
            
            IRBuilder builder = new IRBuilder();
            MainCode = builder.Lower(codePart.MainCode);
            Functions = userFunctions.Select(f => new IRFunction(builder, f)).ToList();
            Triggers = triggers.Select(t => new IRTrigger(builder, t)).ToList();

            Blocks.AddRange(MainCode);
            if (MainCode.Count > 0)
                RootBlocks.Add(MainCode[0]);
            foreach (IRTrigger trigger in Triggers)
            {
                Blocks.AddRange(trigger.Code);
                if (trigger.Code.Count > 0)
                    RootBlocks.Add(trigger.Code[0]);
            }
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

            SingleStaticAssignment.FinalizeSSA(this);
        }

        public void EmitCode(CodePart codePart)
        {
            IREmitter emitter = new IREmitter();
            foreach (IRTrigger trigger in Triggers)
            {
                trigger.EmitCode(emitter);
            }
            foreach (IRFunction function in Functions)
            {
                function.EmitCode(emitter);
            }
            codePart.MainCode = emitter.Emit(MainCode);
        }

        public class IRTrigger : IClosureVariableUser
        {
            private readonly Trigger trigger;
            public string Identifier { get; }
            public List<BasicBlock> Code { get; set; }

            public HashSet<IRVariable> ExternalReads { get; } = new HashSet<IRVariable>();
            public HashSet<SSAVariable> ExternalWrites { get; } = new HashSet<SSAVariable>();

            public IRTrigger(IRBuilder builder, Trigger trigger)
            {
                this.trigger = trigger;
                Identifier = trigger.Code.FirstOrDefault()?.Label ?? "";
                Code = builder.Lower(trigger.Code);
                if (Code.Count > 0)
                {
                    ExternalReads.UnionWith(Code[0].Scope.GetGlobalScope().Variables.Cast<IRVariable>());
                    foreach (BasicBlock block in Code)
                    {
                        ExternalWrites.UnionWith(block.VariablesWritten.Where(v => v.Scope.IsGlobalScope));
                        /*foreach (IRInstruction instruction in block.Instructions)
                        {
                            if (instruction is IRAssign assignment &&
                                assignment.Target.Scope.IsGlobalScope)
                                writes.Add(assignment.Target);
                        }*/
                    }
                }
            }
            public void EmitCode(IREmitter emitter)
            {
                trigger.Code.Clear();
                trigger.Code.AddRange(emitter.Emit(Code));
            }
        }

        public class IRFunction : IClosureVariableUser
        {
            private readonly UserFunction function;
            private readonly List<UserFunctionCodeFragment> userFunctionFragments;
            private readonly Dictionary<UserFunctionCodeFragment, IRFunctionFragment> fragments = new Dictionary<UserFunctionCodeFragment, IRFunctionFragment>();

            public string Identifier => function.Identifier;
            public List<BasicBlock> InitializationCode { get; set; }
            public IReadOnlyCollection<IRFunctionFragment> Fragments => fragments.Values;
            public HashSet<IRVariable> ExternalReads { get; } = new HashSet<IRVariable>();
            public HashSet<SSAVariable> ExternalWrites { get; } = new HashSet<SSAVariable>();

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

                foreach (IRFunctionFragment fragment in Fragments)
                {
                    if (fragment.FunctionCode.Count == 0)
                        continue;
                    ExternalReads.UnionWith(fragment.FunctionCode[0].Scope.GetGlobalScope().Variables.Cast<IRVariable>());
                    foreach (BasicBlock block in fragment.FunctionCode)
                    {
                        ExternalWrites.UnionWith(block.VariablesWritten.Where(v => v.Scope.IsGlobalScope));
                        /*foreach (IRInstruction instruction in block.Instructions)
                        {
                            if (instruction is IRAssign assignment &&
                                assignment.Target.Scope.IsGlobalScope)
                                writes.Add(assignment.Target);
                        }*/
                    }
                }
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

        public static void SetDefiningScope(IClosureVariableUser function, IRScope scope)
        {
            HashSet<SSAVariable> tempWrites = new HashSet<SSAVariable>(function.ExternalWrites);
            foreach (SSAVariable variable in tempWrites)
            {
                if (scope.IsVariableInScope(variable.Name))
                {
                    variable.RedefineScope(scope.GetVariableNamed(variable.Name).Scope);
                }
            }

            HashSet<IRVariable> tempReads = new HashSet<IRVariable>(function.ExternalReads);
            foreach (IRVariable variable in tempReads)
            {
                if (scope.IsVariableInScope(variable.Name))
                {
                    function.ExternalReads.Remove(variable);
                    function.ExternalReads.Add((IRVariable)scope.GetVariableNamed(variable.Name));
                }
            }
        }

        public interface IClosureVariableUser
        {
            HashSet<IRVariable> ExternalReads { get; }
            HashSet<SSAVariable> ExternalWrites { get; }
        }
    }
}
