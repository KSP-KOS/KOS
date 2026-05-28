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
        public List<BasicBlock> RootBlocks { get; } = new List<BasicBlock>();
        /// <summary>
        /// Gets the collection of blocks, across all program elements.
        /// </summary>
        public List<BasicBlock> Blocks { get; } = new List<BasicBlock>();

        /// <summary>
        /// Gets the reachable variables for a given call site.
        /// </summary>
        public Dictionary<IRInstruction, HashSet<IInterimVariableReference>> ReachableVariables { get; } =
            new Dictionary<IRInstruction, HashSet<IInterimVariableReference>>();

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
            MainCode = builder.Lower(codePart.MainCode, this);

            Functions = new List<IRFunction>();
            Queue<UserFunction> functionsToLower = new Queue<UserFunction>(userFunctions.Where(f => closureScopes.ContainsKey(f.Identifier)));
            HashSet<UserFunction> completedFunctions = new HashSet<UserFunction>();
            while (functionsToLower.Count > 0)
            {
                UserFunction function = functionsToLower.Dequeue();
                Functions.Add(new IRFunction(builder, function, this));
                completedFunctions.Add(function);
                foreach (UserFunction func in userFunctions.Where(
                    f => closureScopes.ContainsKey(f.Identifier) &&
                    !completedFunctions.Contains(f) &&
                    !functionsToLower.Contains(f)))
                    functionsToLower.Enqueue(func);
            }
            Triggers = triggers.Select(t => new IRTrigger(builder, t, this)).ToList();
            foreach (UserFunction func in userFunctions.Except(completedFunctions))
                Functions.Add(new IRFunction(builder, func, this));

            Blocks.AddRange(MainCode);
            if (MainCode.Count > 0)
                RootBlocks.Add(MainCode[0]);
            RootBlocks.AddRange(Triggers.Select(t => t.RootBlock));
            RootBlocks.AddRange(Functions.SelectMany(f => f.RootBlocks));

            SingleStaticAssignment.FinalizeSSA(this);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IRCodePart"/> class for DEBUG purposes.
        /// </summary>
        /// <remarks>
        /// This constructor is only intended for unit testing.
        /// </remarks>
        public IRCodePart(List<Opcode> mainCode, List<UserFunction> userFunctions, List<Trigger> triggers)
        {
            IRBuilder builder = new IRBuilder();
            MainCode = builder.Lower(mainCode, this);

            Functions = new List<IRFunction>();
            Queue<UserFunction> functionsToLower = new Queue<UserFunction>(userFunctions.Where(f => closureScopes.ContainsKey(f.Identifier)));
            HashSet<UserFunction> completedFunctions = new HashSet<UserFunction>();
            while (functionsToLower.Count > 0)
            {
                UserFunction function = functionsToLower.Dequeue();
                Functions.Add(new IRFunction(builder, function, this));
                completedFunctions.Add(function);
                foreach (UserFunction func in userFunctions.Where(
                    f => closureScopes.ContainsKey(f.Identifier) &&
                    !completedFunctions.Contains(f) &&
                    !functionsToLower.Contains(f)))
                    functionsToLower.Enqueue(func);
            }
            Triggers = triggers.Select(t => new IRTrigger(builder, t, this)).ToList();
            foreach (UserFunction func in userFunctions.Except(completedFunctions))
                Functions.Add(new IRFunction(builder, func, this));

            Blocks.AddRange(MainCode);
            if (MainCode.Count > 0)
                RootBlocks.Add(MainCode[0]);
            RootBlocks.AddRange(Triggers.Select(t => t.RootBlock));
            RootBlocks.AddRange(Functions.SelectMany(f => f.RootBlocks));
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
            foreach (IRFunction function in Functions)
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
            public HashSet<string> ExternalReads { get; set; } = new HashSet<string>();
            public HashSet<string> ExternalWrites { get; } = new HashSet<string>();
            public HashSet<(string Name, IRUnset Instruction)> ExternalUnsets { get; } = new HashSet<(string, IRUnset)>();
            public HashSet<IRTrigger> TriggersCreated { get; } = new HashSet<IRTrigger>();
            public HashSet<IRFunction> FunctionCalls { get; } = new HashSet<IRFunction>();
            public IRScope ClosureScope { get; }

            public BasicBlock RootBlock { get; }

            /// <summary>
            /// Initializes a new instance of the <see cref="IRTrigger"/> class.
            /// </summary>
            /// <param name="builder">The IRBuilder object in use.</param>
            /// <param name="trigger">The trigger object to convert.</param>
            public IRTrigger(IRBuilder builder, Trigger trigger, IRCodePart codePart)
            {
                this.trigger = trigger;
                Identifier = trigger.Code.FirstOrDefault()?.Label ?? "";
                ClosureScope = codePart.closureScopes[Identifier].Scope;
                Code = builder.Lower(trigger.Code, codePart, ClosureScope);
                if (Code.Count > 0)
                {
                    RootBlock = Code[0];
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
        /// <seealso cref="IClosureVariableUser" />
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

            public List<BasicBlock> RootBlocks { get; } = new List<BasicBlock>();

            /// <summary>
            /// Initializes a new instance of the <see cref="IRFunction"/> class.
            /// </summary>
            /// <param name="builder">The IRBuilder object in use.</param>
            /// <param name="function">The user function object to convert.</param>
            public IRFunction(IRBuilder builder, UserFunction function, IRCodePart codePart)
            {
                this.function = function;
                (ClosureScope, IsGlobal) = codePart.closureScopes[Identifier];
                InitializationCode = builder.Lower(function.InitializationCode, codePart, ClosureScope);
                userFunctionFragments = function.PeekNewCodeFragments().ToList();
                foreach (UserFunctionCodeFragment fragment in userFunctionFragments)
                {
                    fragments.Add(fragment, new IRFunctionFragment(builder, fragment, codePart, ClosureScope));
                }
                userFunctionFragments.Reverse();

                if (function.InitializationCode.Count > 0)
                    RootBlocks.Add(InitializationCode[0]);
                foreach (IRFunctionFragment fragment in Fragments)
                {
                    if (fragment.FunctionCode.Count > 0)
                        RootBlocks.Add(fragment.FunctionCode[0]);
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
                public IRFunctionFragment(IRBuilder builder, UserFunctionCodeFragment codeFragment, IRCodePart codePart, IRScope ClosureScope)
                {
                    fragment = codeFragment;
                    FunctionCode = builder.Lower(codeFragment.Code, codePart, ClosureScope);
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
