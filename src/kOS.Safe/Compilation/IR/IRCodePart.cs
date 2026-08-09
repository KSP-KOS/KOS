using System;
using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Compilation.KS;
using kOS.Safe.Compilation.Optimization;

namespace kOS.Safe.Compilation.IR
{
    /// <summary>
    /// This class is the interim representation of a program, including
    /// the functions, triggers, and mainline code defined therein.
    /// </summary>
    public class IRCodePart : ICodeComponent
    {
        private readonly Dictionary<string, string> functionRefs = new Dictionary<string, string>();
        private readonly Dictionary<string, (IRScope Scope, bool IsGlobal)> closureScopes =
            new Dictionary<string, (IRScope, bool)>();

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
        public IEnumerable<ICodeComponent> Components =>
            new ICodeComponent[] { this }.
            Union(Triggers).
            Union(Functions.SelectMany(GetFunctionFragments));
        public IEnumerable<BasicBlock> RootBlocks =>
            new BasicBlock[] { RootBlock }.
            Union(Triggers.Select(GetRootBlock)).
            Union(Functions.SelectMany(GetFunctionFragments).Select(GetRootBlock));
        private IEnumerable<IRFunction.IRFunctionFragment> GetFunctionFragments(IRFunction function)
            => function.Fragments;
        private BasicBlock GetRootBlock(ICodeComponent codeComponent)
            => codeComponent.RootBlock;
        /// <summary>
        /// Gets the collection of blocks, across all program elements.
        /// </summary>
        public IEnumerable<BasicBlock> Blocks =>
            MainCode.
            Union(Triggers.SelectMany(GetBlocks)).
            Union(Functions.SelectMany(GetFunctionFragments).SelectMany(GetBlocks));
        private IEnumerable<BasicBlock> GetBlocks(ICodeComponent codeComponent)
            => codeComponent.Blocks;
        List<BasicBlock> ICodeComponent.Blocks
        {
            get => MainCode;
            set => MainCode = value;
        }
        IRCodePart ICodeComponent.CodePart => this;
        public BasicBlock RootBlock { get; set; }

        BasicBlock ICodeComponent.TerminalBlock { get; set; }

        /// <summary>
        /// Gets the reachable variables for a given call site.
        /// </summary>
        /// <remarks>
        /// This is populated in <see cref="SingleStaticAssignment.ApplyUses"/>
        /// <para/>
        /// Note that the data contained here may become outdated by other optimization passes.
        /// Use <see cref="SingleStaticAssignment.ApplyUses(BasicBlock)"/> to update it.
        /// </remarks>
        public Dictionary<IRInstruction, HashSet<IInterimVariableReference>> ReachableVariables { get; } =
            new Dictionary<IRInstruction, HashSet<IInterimVariableReference>>(IRInstruction.ReferenceEqualityComparer);

        /// <summary>
        /// Gets or sets the variable uses.
        /// </summary>
        /// <remarks>
        /// The set accessor is used to populate this in <see cref="Optimization.Passes.SCCPWithTypePropagation.ApplyPass"/>.
        /// <para/>
        /// Note that the data contained here may become outdated by other optimization passes.
        /// Use <see cref="Optimization.Passes.SCCPWithTypePropagation.MapUsesAndPropagateTypes(IRCodePart)"/> to get an updated collection.
        /// </remarks>
        public Dictionary<SSADefinition, HashSet<IOperandInstructionBase>> VariableUses { get; set; } =
            new Dictionary<SSADefinition, HashSet<IOperandInstructionBase>>(SSADefinition.ReferenceEqualityComparer);

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
            
            MainCode = IRBuilder.Lower(codePart.MainCode, this);

            Functions = new List<IRFunction>();
            Queue<UserFunction> functionsToLower = new Queue<UserFunction>(userFunctions.Where(f => closureScopes.ContainsKey(f.Identifier)));
            HashSet<UserFunction> completedFunctions = new HashSet<UserFunction>();
            while (functionsToLower.Count > 0)
            {
                UserFunction function = functionsToLower.Dequeue();
                Functions.Add(new IRFunction(function, this));
                completedFunctions.Add(function);
                foreach (UserFunction func in userFunctions.Where(
                    f => closureScopes.ContainsKey(f.Identifier) &&
                    !completedFunctions.Contains(f) &&
                    !functionsToLower.Contains(f)))
                    functionsToLower.Enqueue(func);
            }
            Triggers = triggers.Select(t => new IRTrigger(t, this)).ToList();
            foreach (UserFunction func in userFunctions.Except(completedFunctions))
                Functions.Add(new IRFunction(func, this));

            if (MainCode.Count > 0)
                RootBlock = MainCode[0];
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IRCodePart"/> class for DEBUG purposes.
        /// </summary>
        /// <remarks>
        /// This constructor is only intended for unit testing.
        /// </remarks>
        public IRCodePart(List<Opcode> mainCode, List<UserFunction> userFunctions, List<Trigger> triggers)
        {
            MainCode = IRBuilder.Lower(mainCode, this);

            Functions = new List<IRFunction>();
            Queue<UserFunction> functionsToLower = new Queue<UserFunction>(userFunctions.Where(f => closureScopes.ContainsKey(f.Identifier)));
            HashSet<UserFunction> completedFunctions = new HashSet<UserFunction>();
            while (functionsToLower.Count > 0)
            {
                UserFunction function = functionsToLower.Dequeue();
                Functions.Add(new IRFunction(function, this));
                completedFunctions.Add(function);
                foreach (UserFunction func in userFunctions.Where(
                    f => closureScopes.ContainsKey(f.Identifier) &&
                    !completedFunctions.Contains(f) &&
                    !functionsToLower.Contains(f)))
                    functionsToLower.Enqueue(func);
            }
            Triggers = triggers.Select(t => new IRTrigger(t, this)).ToList();
            foreach (UserFunction func in userFunctions.Except(completedFunctions))
                Functions.Add(new IRFunction(func, this));

            if (MainCode.Count > 0)
                RootBlock = MainCode[0];
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

        public static void FlattenCallTree(IClosureVariableUser funcOrTrigger)
        {
            funcOrTrigger.FunctionCalls.UnionWith(GetFunctionsCalled(funcOrTrigger));
            funcOrTrigger.TriggersCreated.UnionWith(GetTriggersCreated(funcOrTrigger));
            funcOrTrigger.ExternalWrites.UnionWith(funcOrTrigger.FunctionCalls.SelectMany(f => f.ExternalWrites));
            funcOrTrigger.ExternalUnsets.UnionWith(funcOrTrigger.FunctionCalls.SelectMany(f => f.ExternalUnsets));
        }
        public void FlattenCallTrees()
        {
            foreach (IRFunction function in Functions)
                FlattenCallTree(function);
            foreach (IRTrigger trigger in Triggers)
                FlattenCallTree(trigger);
        }
        private static HashSet<IRFunction> GetFunctionsCalled(IClosureVariableUser funcOrTrigger)
        {
            HashSet<IRFunction> result = new HashSet<IRFunction>();
            GetFunctionsCalled_Recursive(funcOrTrigger, result);
            return result;
        }
        private static void GetFunctionsCalled_Recursive(IClosureVariableUser funcOrTrigger, HashSet<IRFunction> result)
        {
            foreach (IRFunction func in funcOrTrigger.FunctionCalls)
            {
                if (result.Add(func))
                    GetFunctionsCalled_Recursive(func, result);
            }
        }
        public static HashSet<IRTrigger> GetTriggersCreated(IClosureVariableUser funcOrTrigger)
            => new HashSet<IRTrigger>(GetFunctionsCalled(funcOrTrigger).SelectMany(f => f.TriggersCreated));

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
            foreach (IRFunction function in Functions.Where(f => f.IsGlobal || f.CallSites.Count > 0))
            {
                function.EmitCode(emitter);
            }
            codePart.MainCode = emitter.Emit(MainCode);
        }

        public void EnrollClosure(string pointer, IRScope closureScope, bool isGlobal = false)
        {
            closureScopes[pointer] = (closureScope, isGlobal);
        }
        public void EnrollFunction(string variable, string functionRef, IRScope closureScope, bool isGlobal)
        {
            string functionID = functionRef.Split('-').First();
            functionRefs[variable] = functionID;
            if (Functions == null)
            {
                EnrollClosure(functionID, closureScope, isGlobal);
            }
            else
            {
                IRFunction function = GetFunction(functionID);
                if (function == null)
                    EnrollClosure(functionID, closureScope, isGlobal);
                else
                    function.IsGlobal = isGlobal;
            }
        }
        public IRFunction GetFunction(IRCall call)
        {
            if (!functionRefs.TryGetValue(call.Function, out string functionName))
                return null;
            if (functionName == null)
                return null;
            return GetFunction(functionName);
        }

        /// <summary>
        /// This class represents a trigger definition.
        /// </summary>
        /// <seealso cref="IClosureVariableUser" />
        /// <seealso cref="ICodeComponent" />
        public class IRTrigger : IClosureVariableUser, ICodeComponent
        {
            private readonly Trigger trigger;
            /// <summary>
            /// Gets the identifier string for this trigger.
            /// </summary>
            public string Identifier { get; }
            /// <summary>
            /// Gets or sets the code for this trigger, in BasicBlock representation.
            /// </summary>
            public List<BasicBlock> Blocks { get; set; }
            public HashSet<string> ExternalReads { get; set; } = new HashSet<string>();
            public HashSet<string> ExternalWrites { get; } = new HashSet<string>();
            public HashSet<(string Name, IRUnset Instruction)> ExternalUnsets { get; } = new HashSet<(string, IRUnset)>();
            public HashSet<IRTrigger> TriggersCreated { get; } = new HashSet<IRTrigger>();
            public HashSet<IRFunction> FunctionCalls { get; } = new HashSet<IRFunction>();
            public IRScope ClosureScope { get; }

            public BasicBlock RootBlock { get; set; }
            public BasicBlock TerminalBlock { get; set; }
            public IRCodePart CodePart { get; }

            /// <summary>
            /// Initializes a new instance of the <see cref="IRTrigger"/> class.
            /// </summary>
            /// <param name="builder">The IRBuilder object in use.</param>
            /// <param name="trigger">The trigger object to convert.</param>
            public IRTrigger(Trigger trigger, IRCodePart codePart)
            {
                this.trigger = trigger;
                CodePart = codePart;
                Identifier = trigger.Code.FirstOrDefault()?.Label ?? "";
                ClosureScope = codePart.closureScopes[Identifier].Scope;
                Blocks = IRBuilder.Lower(trigger.Code, this, ClosureScope);
                if (Blocks.Count > 0)
                {
                    RootBlock = Blocks[0];
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
                trigger.Code.AddRange(emitter.Emit(Blocks));
            }
            public override string ToString()
                => $"IRTrigger: {Identifier}";
        }

        /// <summary>
        /// This class represents a user-defined function.
        /// </summary>
        /// <seealso cref="IClosureVariableUser" />
        public class IRFunction : IClosureVariableUser
        {
            private readonly UserFunction function;
            private readonly List<UserFunctionCodeFragment> userFunctionFragments;
            private readonly Dictionary<UserFunctionCodeFragment, IRFunctionFragment> fragments = new Dictionary<UserFunctionCodeFragment, IRFunctionFragment>();

            /// <summary>
            /// Gets the code part to which this function belongs.
            /// </summary>
            public IRCodePart CodePart { get; }
            /// <summary>
            /// Gets the identifier string for this function.
            /// </summary>
            public string Identifier => function.Identifier;
            /// <summary>
            /// Gets or sets a value indicating whether this instance
            /// is stored at the global scope.
            /// </summary>
            /// <value>
            ///   <c>true</c> if this instance is global; otherwise, <c>false</c>.
            /// </value>
            public bool IsGlobal { get; internal set; } = false;
            /// <summary>
            /// Gets or sets the initialization code, in BasicBlock format.
            /// </summary>
            public List<BasicBlock> InitializationCode { get; set; }
            /// <summary>
            /// Gets the collection of function fragments.
            /// </summary>
            public IReadOnlyCollection<IRFunctionFragment> Fragments => fragments.Values;
            public HashSet<string> ExternalReads { get; set; } = new HashSet<string>();
            public HashSet<string> ExternalWrites { get; } = new HashSet<string>();
            public HashSet<(string Name, IRUnset Instruction)> ExternalUnsets { get; } = new HashSet<(string, IRUnset)>();
            public HashSet<IRTrigger> TriggersCreated { get; } = new HashSet<IRTrigger>();
            public HashSet<IRFunction> FunctionCalls { get; } = new HashSet<IRFunction>();
            public IRScope ClosureScope { get; }
            /// <summary>
            /// Gets a value indicating whether this instance may be recursive.
            /// </summary>
            /// <value>
            ///   <c>true</c> if this instance may be recursive; otherwise, <c>false</c>.
            /// </value>
            public bool IsRecursive => FunctionCalls.Contains(this);
            /// <summary>
            /// Gets the return value of this function.
            /// </summary>
            public PhiOperand<IRReturn> Returns { get; } = new PhiOperand<IRReturn>();
            /// <summary>
            /// Gets a value indicating whether this instance is invariant.
            /// A user function must also be inert to be considered invariant.
            /// </summary>
            /// <value>
            ///   <c>true</c> if this instance is invariant; otherwise, <c>false</c>.
            /// </value>
            public bool IsInvariant
                => Returns.IsInvariant &&
                IsInert;
            /// <summary>
            /// Gets a value indicating whether this instance is inert.
            /// A user function that contains any calls to non-inert functions is,
            /// itself, not inert. Any assignments or unsets to the enclosing scope or
            /// setting any suffixes or indexes also makes a function non-inert.
            /// </summary>
            /// <value>
            ///   <c>true</c> if this instance is inert; otherwise, <c>false</c>.
            /// </value>
            public bool IsInert
                => IsSelfInert &&
                FunctionCalls.Where(f => f != this).All(function => function.IsSelfInert);
            private bool IsSelfInert
            {
                get
                {
                    if (ExternalWrites.Count > 0 || ExternalUnsets.Count > 0)
                        return false;

                    return Fragments.All(fragment =>
                        fragment.Blocks.Where(block => block.IsExecutable).All(block =>
                            block.Instructions.All(instruction =>
                            {
                                foreach (IRInstruction operation in instruction.DepthFirstInstructions())
                                    if (operation is IActionInstruction actionInstruction &&
                                        !actionInstruction.IsInert)
                                        return false;
                                return true;
                            })
                        )
                    );
                }
            }

            public HashSet<IRCall> CallSites { get; } = new HashSet<IRCall>(IRInstruction.ReferenceEqualityComparer);

            public List<BasicBlock> RootBlocks { get; } = new List<BasicBlock>();
            public BasicBlock TerminalBlock { get; set; }

            /// <summary>
            /// Initializes a new instance of the <see cref="IRFunction"/> class.
            /// </summary>
            /// <param name="builder">The IRBuilder object in use.</param>
            /// <param name="function">The user function object to convert.</param>
            public IRFunction(UserFunction function, IRCodePart codePart)
            {
                CodePart = codePart;
                this.function = function;
                (ClosureScope, IsGlobal) = codePart.closureScopes[Identifier];
                InitializationCode = IRBuilder.Lower(function.InitializationCode, codePart, ClosureScope);
                userFunctionFragments = function.PeekNewCodeFragments().ToList();
                foreach (UserFunctionCodeFragment fragment in userFunctionFragments)
                {
                    fragments.Add(fragment, new IRFunctionFragment(fragment, codePart, this));
                }
                userFunctionFragments.Reverse();

                if (function.InitializationCode.Count > 0)
                    RootBlocks.Add(InitializationCode[0]);
                foreach (IRFunctionFragment fragment in Fragments)
                {
                    if (fragment.Blocks.Count > 0)
                        RootBlocks.Add(fragment.Blocks[0]);

                    foreach (BasicBlock block in fragment.Blocks)
                    {
                        if (block.Successors.Any(b => !(b is SyntheticReturnBlock)))
                            continue;
                        if (!(block.Instructions[block.Instructions.Count - 1] is IRReturn ret))
                            Returns.PossibleValues[block] = null;
                        else
                            Returns.PossibleValues[block] = ret;
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

            public override string ToString()
                => $"IRFunction: {Identifier}";

            /// <summary>
            /// This class represents a function fragment. See <seealso cref="UserFunctionCodeFragment"/>.
            /// </summary>
            public class IRFunctionFragment : ICodeComponent
            {
                private readonly UserFunctionCodeFragment fragment;
                /// <summary>
                /// Gets or sets the function code, in BasicBlock representation.
                /// </summary>
                public List<BasicBlock> Blocks { get; set; }
                public IRFunction Function { get; set; }
                public BasicBlock RootBlock { get; set; }
                public BasicBlock TerminalBlock
                {
                    get => Function.TerminalBlock;
                    set => Function.TerminalBlock = value;
                }
                public IRCodePart CodePart { get; }
                /// <summary>
                /// Initializes a new instance of the <see cref="IRFunctionFragment"/> class.
                /// </summary>
                /// <param name="builder">The IRBuilder object in use.</param>
                /// <param name="codeFragment">The function code fragment to convert.</param>
                public IRFunctionFragment(UserFunctionCodeFragment codeFragment, IRCodePart codePart, IRFunction function)
                {
                    Function = function;
                    CodePart = codePart;
                    fragment = codeFragment;
                    Blocks = IRBuilder.Lower(codeFragment.Code, this, function.ClosureScope);
                    RootBlock = Blocks.FirstOrDefault();
                }
                /// <summary>
                /// Emits the code into Opcode representation back into the
                /// trigger's source object.
                /// </summary>
                /// <param name="emitter">The IREmitter object in use.</param>
                public void EmitCode(IREmitter emitter)
                {
                    fragment.Code.Clear();
                    fragment.Code.AddRange(emitter.Emit(Blocks));
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
            HashSet<string> ExternalReads { get; set; }
            /// <summary>
            /// Gets the collection of external variables that may be written
            /// to by this instance.
            /// </summary>
            HashSet<string> ExternalWrites { get; }
            /// <summary>
            /// Gets the collection of external variables that may be unset by this instance.
            /// </summary>
            HashSet<(string Name, IRUnset Instruction)> ExternalUnsets { get; }
            /// <summary>
            /// Gets the collection of triggers that could be created from this instance.
            /// </summary>
            HashSet<IRTrigger> TriggersCreated { get; }
            /// <summary>
            /// Gets the functions called from within this instance's body.
            /// </summary>
            HashSet<IRFunction> FunctionCalls { get; }
            /// <summary>
            /// Gets the scope of the closure this instance uses.
            /// </summary>
            IRScope ClosureScope { get; }
        }
    }
}
