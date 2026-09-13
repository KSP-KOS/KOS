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
        /// Gets the main body code component.
        /// </summary>
        public CodeElement MainCode { get; }
        /// <summary>
        /// Gets the function definitions.
        /// </summary>
        public List<IRFunction> Functions { get; private set; }
        /// <summary>
        /// Gets the trigger definitions.
        /// </summary>
        public List<IRTrigger> Triggers { get; private set; }
        /// <summary>
        /// Gets the <see cref="CodeElement"/>s contained within this object.
        /// </summary>
        /// <seealso cref="MainCode"/>
        /// <seealso cref="Triggers"/>
        /// <seealso cref="Functions"/>
        public IEnumerable<CodeElement> Elements =>
            new CodeElement[] { MainCode }.
            Concat(Triggers).
            Concat(Functions.SelectMany(f => f.Fragments));
        /// <summary>
        /// Gets all <see cref="CodeComponent"/>s contained within
        /// all elements associated with this object.
        /// </summary>
        /// <seealso cref="Elements"/>
        public IEnumerable<CodeComponent> Components =>
            Elements.SelectMany(CodeElement.GetComponents);
        /// <summary>
        /// Gets the root blocks for all components
        /// </summary>
        /// <seealso cref="Components"/>
        public IEnumerable<BasicBlock> RootBlocks => Components.Select(CodeComponent.GetRootBlock);
        /// <summary>
        /// Gets the collection of blocks, across all program components.
        /// </summary>
        /// <seealso cref="Components"/>
        public IEnumerable<BasicBlock> Blocks => Components.SelectMany(CodeComponent.GetBlocks);

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

        public HashSet<IRInstruction> UnresolvedCallSites { get; } = new HashSet<IRInstruction>(IRInstruction.ReferenceEqualityComparer);

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

            MainCode = new CodeElement(codePart.MainCode, this, null);

            Functions = new List<IRFunction>();
            Queue<UserFunction> functionsToLower = new Queue<UserFunction>(userFunctions);
            Triggers = new List<IRTrigger>();
            Queue<Trigger> triggersToLower = new Queue<Trigger>(triggers);
            while (functionsToLower.Count > 0 ||
                triggersToLower.Count > 0)
            {
                if (functionsToLower.Where(f => closureScopes.ContainsKey(f.Identifier)).Any())
                {
                    UserFunction function = functionsToLower.Dequeue();
                    if (!closureScopes.ContainsKey(function.Identifier))
                        functionsToLower.Enqueue(function);
                    else
                        Functions.Add(new IRFunction(function, this));
                }
                else
                {
                    Trigger trigger = triggersToLower.Dequeue();
                    if (!closureScopes.ContainsKey(trigger.Code.FirstOrDefault()?.Label ?? ""))
                        triggersToLower.Enqueue(trigger);
                    else
                        Triggers.Add(new IRTrigger(trigger, this));
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IRCodePart"/> class for DEBUG purposes.
        /// </summary>
        /// <remarks>
        /// This constructor is only intended for unit testing.
        /// </remarks>
        public IRCodePart(List<Opcode> mainCode, List<UserFunction> userFunctions, List<Trigger> triggers)
        {
            MainCode = new CodeElement(mainCode, this, null);

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
        }

        /// <summary>
        /// Gets a function by string reference.
        /// </summary>
        /// <param name="identifier">The function identifier string.</param>
        public IInterimFunction GetFunction(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
                return null;

            if (identifier.StartsWith("$") && identifier.EndsWith("*"))
            {
                if (!functionRefs.TryGetValue(identifier, out string functionName))
                    return null;
                if (functionName != null)
                    return GetFunction(functionName);
            }

            IInterimFunction result;
            result = Functions.FirstOrDefault(f => string.Equals(f.Identifier, identifier, StringComparison.OrdinalIgnoreCase));
            if (result != null)
                return result;
            result = (IInterimFunction)Components.FirstOrDefault(c => c is IIdentifiedComponent identifiedComponent && string.Equals(identifiedComponent.Identifier, identifier, StringComparison.OrdinalIgnoreCase));
            if (result != null)
                return result;
            return null;
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
        
        private static HashSet<IInterimFunction> GetFunctionsCalled(IClosureVariableUser funcOrTrigger)
        {
            HashSet<IInterimFunction> result = new HashSet<IInterimFunction>();
            Queue<IInterimFunction> queue = new Queue<IInterimFunction>(funcOrTrigger.FunctionCalls);
            while (queue.Count > 0)
            {
                IInterimFunction function = queue.Dequeue();
                if (result.Add(function))
                    foreach(IInterimFunction called in function.FunctionCalls)
                        queue.Enqueue(called);
            }
            return result;
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
            Triggers.Sort((t1, t2) => StringComparer.OrdinalIgnoreCase.Compare(t1.Identifier, t2.Identifier));
            foreach (IRTrigger trigger in Triggers)
            {
                trigger.EmitCode(emitter);
            }
            foreach (IRFunction function in Functions)
            {
                function.EmitCode(emitter);
            }
            MainCode.EmitCode(emitter, codePart.MainCode);
        }

        public void EnrollClosure(string pointer, IRScope closureScope, bool isGlobal = false)
            => closureScopes[pointer] = (closureScope, isGlobal);
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
                IRFunction function = (IRFunction)GetFunction(functionID);
                if (function == null)
                    EnrollClosure(functionID, closureScope, isGlobal);
                else
                    function.IsGlobal = isGlobal;
            }
        }
        public (IRScope Scope, bool IsGlobal) GetClosureScope(string identifier)
            => closureScopes[identifier];
    }
}
