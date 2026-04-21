using System;
using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Compilation.KS;

namespace kOS.Safe.Compilation.IR
{
    /// <summary>
    /// This class is the interim representation of a program, including
    /// the functions, triggers, and mainline code defined therein.
    /// </summary>
    public class IRCodePart
    {
        /// <summary>
        /// Gets or sets the mainline code, in BasicBlock format.
        /// </summary>
        public List<BasicBlock> MainCode { get; set; }
        /// <summary>
        /// Gets or sets the function definitions.
        /// </summary>
        public List<IRFunction> Functions { get; set; }
        /// <summary>
        /// Gets or sets the trigger definitions.
        /// </summary>
        public List<IRTrigger> Triggers { get; set; }
        /// <summary>
        /// Gets the collection of root blocks, across all mainline
        /// code, functions, and triggers.
        /// </summary>
        public List<BasicBlock> RootBlocks { get; } = new List<BasicBlock>();
        /// <summary>
        /// Gets the collection of blocks, across all program elements.
        /// </summary>
        public List<BasicBlock> Blocks { get; } = new List<BasicBlock>();

        /// <summary>
        /// Initializes a new instance of the <see cref="IRCodePart"/> class.
        /// </summary>
        /// <param name="codePart">The code part containing main code.</param>
        /// <param name="userFunctions">The user functions defined in this program.</param>
        /// <param name="triggers">The triggers defined in this program.</param>
        /// <exception cref="System.ArgumentException"></exception>
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

        /// <summary>
        /// Gets a function by string reference.
        /// </summary>
        /// <param name="identifier">The function identifier string.</param>
        public IRFunction GetFunction(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
                return null;
            return Functions.FirstOrDefault(f => string.Equals(f.Identifier, identifier, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets a trigger by string reference.
        /// </summary>
        /// <param name="identifier">The trigger identifier string.</param>
        public IRTrigger GetTrigger(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
                return null;
            return Triggers.FirstOrDefault(t => string.Equals(t.Identifier, identifier, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Emits the code into Opcode representation, and back into
        /// its source objects.
        /// </summary>
        /// <param name="codePart">The code part into which to emit mainline code.</param>
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

        /// <summary>
        /// This class represents a trigger definition.
        /// </summary>
        /// <seealso cref="kOS.Safe.Compilation.IR.IRCodePart.IClosureVariableUser" />
        public class IRTrigger : IClosureVariableUser
        {
            private readonly Trigger trigger;
            /// <summary>
            /// Gets the identifier string for this trigger.
            /// </summary>
            public string Identifier { get; }
            /// <summary>
            /// Gets or sets the code for this trigger, in BasicBlock representation.
            /// </summary>
            public List<BasicBlock> Code { get; set; }

            /// <summary>
            /// Gets the collection of external variables that are read
            /// within the trigger.
            /// </summary>
            public HashSet<IRVariable> ExternalReads { get; } = new HashSet<IRVariable>();
            /// <summary>
            /// Gets the collection of external variables that are written
            /// to within the trigger.
            /// </summary>
            public HashSet<SSAVariable> ExternalWrites { get; } = new HashSet<SSAVariable>();

            /// <summary>
            /// Initializes a new instance of the <see cref="IRTrigger"/> class.
            /// </summary>
            /// <param name="builder">The IRBuilder object in use.</param>
            /// <param name="trigger">The trigger object to convert.</param>
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
            /// <summary>
            /// Emits the code into Opcode representation back into the
            /// trigger's source object.
            /// </summary>
            /// <param name="emitter">The IREmitter object in use.</param>
            public void EmitCode(IREmitter emitter)
            {
                trigger.Code.Clear();
                trigger.Code.AddRange(emitter.Emit(Code));
            }
        }

        /// <summary>
        /// This class represents a user-defined function.
        /// </summary>
        /// <seealso cref="kOS.Safe.Compilation.IR.IRCodePart.IClosureVariableUser" />
        public class IRFunction : IClosureVariableUser
        {
            private readonly UserFunction function;
            private readonly List<UserFunctionCodeFragment> userFunctionFragments;
            private readonly Dictionary<UserFunctionCodeFragment, IRFunctionFragment> fragments = new Dictionary<UserFunctionCodeFragment, IRFunctionFragment>();

            /// <summary>
            /// Gets the identifier string for this function.
            /// </summary>
            public string Identifier => function.Identifier;
            /// <summary>
            /// Gets or sets the initialization code, in BasicBlock format.
            /// </summary>
            public List<BasicBlock> InitializationCode { get; set; }
            /// <summary>
            /// Gets the collection of function fragments.
            /// </summary>
            public IReadOnlyCollection<IRFunctionFragment> Fragments => fragments.Values;
            /// <summary>
            /// Gets the collection of external variables that are read
            /// within the function.
            /// </summary>
            public HashSet<IRVariable> ExternalReads { get; } = new HashSet<IRVariable>();
            /// <summary>
            /// Gets the collection of external variables that are written
            /// to within the function.
            /// </summary>
            public HashSet<SSAVariable> ExternalWrites { get; } = new HashSet<SSAVariable>();

            /// <summary>
            /// Initializes a new instance of the <see cref="IRFunction"/> class.
            /// </summary>
            /// <param name="builder">The IRBuilder object in use.</param>
            /// <param name="function">The user function object to convert.</param>
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

            /// <summary>
            /// Emits the code into Opcode representation back into the
            /// trigger's source object.
            /// </summary>
            /// <param name="emitter">The IREmitter object in use.</param>
            public void EmitCode(IREmitter emitter)
            {
                function.InitializationCode.Clear();
                function.InitializationCode.AddRange(emitter.Emit(InitializationCode));
                foreach (UserFunctionCodeFragment fragment in userFunctionFragments)
                {
                    fragments[fragment].EmitCode(emitter);
                }
            }

            /// <summary>
            /// This class represents a function fragment. See <seealso cref="UserFunctionCodeFragment"/>.
            /// </summary>
            public class IRFunctionFragment
            {
                private readonly UserFunctionCodeFragment fragment;
                /// <summary>
                /// Gets or sets the function code, in BasicBlock representation.
                /// </summary>
                public List<BasicBlock> FunctionCode { get; set; }
                /// <summary>
                /// Initializes a new instance of the <see cref="IRFunctionFragment"/> class.
                /// </summary>
                /// <param name="builder">The IRBuilder object in use.</param>
                /// <param name="codeFragment">The function code fragment to convert.</param>
                public IRFunctionFragment(IRBuilder builder, UserFunctionCodeFragment codeFragment)
                {
                    fragment = codeFragment;
                    FunctionCode = builder.Lower(codeFragment.Code);
                }
                /// <summary>
                /// Emits the code into Opcode representation back into the
                /// trigger's source object.
                /// </summary>
                /// <param name="emitter">The IREmitter object in use.</param>
                public void EmitCode(IREmitter emitter)
                {
                    fragment.Code.Clear();
                    fragment.Code.AddRange(emitter.Emit(FunctionCode));
                }
            }
        }

        /// <summary>
        /// Sets the scope in which a function or trigger is defined.
        /// </summary>
        /// <param name="function">The function or trigger to target.</param>
        /// <param name="scope">The scope to set as parent.</param>
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

        /// <summary>
        /// Represents a user of a closure and its contained variables.
        /// </summary>
        public interface IClosureVariableUser
        {
            /// <summary>
            /// Gets the collection of external variables that are read
            /// within the closure.
            /// </summary>
            HashSet<IRVariable> ExternalReads { get; }
            /// <summary>
            /// Gets the collection of external variables that are written
            /// to within the closure.
            /// </summary>
            HashSet<SSAVariable> ExternalWrites { get; }
        }
    }
}
