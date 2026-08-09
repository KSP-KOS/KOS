using System;
using System.Collections.Generic;
using System.Linq;

namespace kOS.Safe.Compilation.IR
{
    public interface IInterimVariableReference : IInterimOperand
    {
        string Name { get; }
        IInterimVariableReference CloneReferenceTo(short sourceLine, short sourceColumn);
    }
    public readonly struct InterimVariableReference : IInterimVariableReference
    {
        public short SourceLine { get; }
        public short SourceColumn { get; }
        public string Name { get; }
        public bool IsInvariant => false;

        public Type Type => typeof(Encapsulation.Structure);

        public InterimVariableReference(string name, Opcode opcode) :
            this(name, opcode.SourceLine, opcode.SourceColumn) { }
        public InterimVariableReference(string name, IRInstruction instruction) :
            this(name, instruction.SourceLine, instruction.SourceColumn) { }
        public InterimVariableReference(string name, short sourceLine, short sourceColumn)
        {
            Name = name;
            SourceLine = sourceLine;
            SourceColumn = sourceColumn;
        }

        public IInterimOperand Clone(BasicBlock _, bool __ = false)
            => new InterimVariableReference(Name, SourceLine, SourceColumn);
        public IInterimVariableReference CloneReferenceTo(short sourceLine, short sourceColumn)
            => new InterimVariableReference(Name, sourceLine, sourceColumn);

        public IEnumerable<Opcode> EmitOpcodes()
        {
            yield return new OpcodePush(Name)
            {
                SourceLine = SourceLine,
                SourceColumn = SourceColumn
            };
        }

        public override string ToString()
            => $"{Name}";

        // Assumes that variable reference on the same line are intended
        // to be the same variable. This is only not true if a trigger
        // modifies a variable between opcodes of the same line.
        // Users are likely to not expect that to occur, and certainly
        // should not count on it occurring, so it should be a valid
        // assumption to make.
        public bool Equals(IInterimOperand other)
            => other is IInterimVariableReference variableRef &&
            string.Equals(Name, variableRef.Name, StringComparison.OrdinalIgnoreCase) &&
            SourceLine == variableRef.SourceLine;
        public override bool Equals(object obj)
            => obj is IInterimVariableReference variable &&
            string.Equals(Name, variable.Name, StringComparison.OrdinalIgnoreCase) &&
            SourceLine == variable.SourceLine;
        public override int GetHashCode()
            => (Name.ToLower(), SourceLine).GetHashCode();
    }

    public readonly struct InterimResolvedReference : IInterimVariableReference, IEvaluatableToConstant
    {
        public short SourceLine { get; }
        public short SourceColumn { get; }
        public SSADefinition Reference { get; }
        public string Name => Reference.Name;
        public bool IsInvariant => Reference.IsInvariant;
        public Type Type => Reference.Type;

        public InterimResolvedReference(SSADefinition reference, IRInstruction instruction) :
            this(reference, instruction.SourceLine, instruction.SourceColumn) { }
        public InterimResolvedReference(SSADefinition reference, short sourceLine, short sourceColumn)
        {
            Reference = reference;
            SourceLine = sourceLine;
            SourceColumn = sourceColumn;
        }

        public IInterimOperand Clone(BasicBlock _, bool maintainSSAReferences = false)
            => maintainSSAReferences ? CloneReferenceTo(SourceLine, SourceColumn) :
            new InterimVariableReference(Name, SourceLine, SourceColumn);
        public IInterimVariableReference CloneReferenceTo(short sourceLine, short sourceColumn)
            => new InterimResolvedReference(Reference, sourceLine, sourceColumn);

        public IEnumerable<Opcode> EmitOpcodes()
        {
            yield return new OpcodePush(Name)
            {
                SourceLine = SourceLine,
                SourceColumn = SourceColumn
            };
        }

        public InterimConstantValue Evaluate()
        {
            if (!IsInvariant)
                throw new InvalidOperationException();
            return Reference.Evaluate();
        }

        public override string ToString()
            => Reference.ToString();
        public bool Equals(IInterimOperand other)
            => (other is InterimResolvedReference variable &&
            Reference.Equals(variable.Reference)) ||
            (IsInvariant &&
            other is IEvaluatableToConstant evaluatableToConstant &&
            evaluatableToConstant.IsInvariant &&
            Evaluate().Equals(evaluatableToConstant.Evaluate()));
        public override bool Equals(object obj)
            => obj is InterimResolvedReference variable &&
            Reference.Equals(variable.Reference);
        public override int GetHashCode()
            => Reference.GetHashCode();
    }

    public class InterimUnresolvedReference : IInterimVariableReference
    {
        private readonly List<SSADefinition> references;

        public short SourceLine { get; }
        public short SourceColumn { get; }
        public IReadOnlyCollection<SSADefinition> References => references;
        public string Name { get; }
        public bool IsInvariant => false;
        public Type Type
        {
            get
            {
                Type proposedType = References.First().Type;
                foreach (SSADefinition variable in References.Skip(1))
                    proposedType = PhiNode.GetFirstCommonBaseType(proposedType, variable.Type);

                return proposedType;
            }
        }

        private InterimUnresolvedReference(string name, short sourceLine, short sourceColumn)
        {
            Name = name;
            SourceLine = sourceLine;
            SourceColumn = sourceColumn;
        }
        public InterimUnresolvedReference(SSADefinition reference, short sourceLine, short sourceColumn) :
            this(reference.Name, sourceLine, sourceColumn)
        {
            references = new List<SSADefinition>
            {
                reference
            };
        }
        public InterimUnresolvedReference(IEnumerable<SSADefinition> references, short sourceLine, short sourceColumn) :
            this(references.First().Name, sourceLine, sourceColumn)
        {
            this.references = new List<SSADefinition>();
            foreach (SSADefinition reference in references)
                AddReference(reference);
        }

        public void AddReference(SSADefinition reference)
        {
            if (!string.Equals(reference.Name, Name, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException($"Names must match: {reference.Name} vs. {Name}");
            references.Add(reference);
        }

        public IInterimOperand Clone(BasicBlock _, bool maintainSSAReferences = false)
            => maintainSSAReferences ? CloneReferenceTo(SourceLine, SourceColumn) :
            new InterimVariableReference(Name, SourceLine, SourceColumn);
        public IInterimVariableReference CloneReferenceTo(short sourceLine, short sourceColumn)
            => new InterimUnresolvedReference(References, sourceLine, sourceColumn);

        public IEnumerable<Opcode> EmitOpcodes()
        {
            yield return new OpcodePush(Name)
            {
                SourceLine = SourceLine,
                SourceColumn = SourceColumn
            };
        }

        public override string ToString()
            => $"{Name} #?";
        public bool Equals(IInterimOperand other)
            => other is InterimUnresolvedReference unresolvedRef &&
            references.SequenceEqual(unresolvedRef.references);
        public override bool Equals(object obj)
            => obj is InterimUnresolvedReference unresolvedRef &&
            references.SequenceEqual(unresolvedRef.references);
        public override int GetHashCode()
            => References.GetHashCode();
    }

    public static class SSAIndexIssuer
    {
        private static readonly Dictionary<string, uint> indices = new Dictionary<string, uint>();
        public static uint GetIndex(string name)
        {
            if (indices.ContainsKey(name))
                unchecked
                {
                    return ++indices[name];
                }
            indices[name] = 0;
            return 0;
        }
    }

    public abstract class SSADefinition : IEquatable<SSADefinition>
    {
        protected readonly uint ssaIndex;
        protected Dictionary<IRUnset, SSADefinition> potentialUnsetSites;
        protected readonly Dictionary<SSASetDefinition, SSAPotentialDefinition> potentialClobberDefinitions =
            new Dictionary<SSASetDefinition, SSAPotentialDefinition>();

        public enum SetState
        {
            Set = 1,
            PotentiallyUnset = 0,
            Unset = -1
        }

        public static IEqualityComparer<SSADefinition> ReferenceEqualityComparer => SSAReferenceEqualityComparer.Instance;

        public string Name { get; }
        public abstract bool IsInvariant { get; }
        public abstract Type Type { get; }
        public virtual SetState State { get; }
        public IRInstruction AssignedAt { get; }
        public HashSet<SSADefinition> ReplacedBy { get; } = new HashSet<SSADefinition>(SSAReferenceEqualityComparer.Instance);
        public HashSet<SSADefinition> Replaces { get; } = new HashSet<SSADefinition>(SSAReferenceEqualityComparer.Instance);

        protected SSADefinition(string name)
        {
            Name = name;
            ssaIndex = SSAIndexIssuer.GetIndex(name);
        }
        protected SSADefinition(string name, IRInstruction assignedAt) : this(name)
        {
            AssignedAt = assignedAt;
        }
        protected SSADefinition(string name, SetState state, IRInstruction assignedAt) : this(name, assignedAt)
        {
            State = state;
        }

        public abstract SSADefinition PotentiallyUnset(IRUnset potentiallyUnsetAt, bool writeReplaceChain);
        public abstract InterimConstantValue Evaluate();

        public SSADefinition PotentiallyOverwrite(SSASetDefinition newDefinition, bool writeReplaceChain)
        {
            if (State == SetState.Unset)
                return this;
            if (newDefinition.State == SetState.Unset)
                throw new ArgumentException($"{nameof(newDefinition)} must not be definitively unset.");
            if (newDefinition.Equals(this))
                return this;
            if (!potentialClobberDefinitions.TryGetValue(newDefinition, out SSAPotentialDefinition result))
            {
                result = new SSAPotentialDefinition(this, newDefinition);
                potentialClobberDefinitions[newDefinition] = result;
            }
            if (writeReplaceChain)
            {
                newDefinition.Replaces.Add(this);
                ReplacedBy.Add(newDefinition);
            }
            return result;
        }

        public SSASetDefinition GetSetDefinition()
        {
            switch (this)
            {
                case SSASetDefinition setDefinition:
                    return setDefinition;
                case SSAPotentialDefinition potentialDefinition:
                    if (potentialDefinition.Conditional.IsExecutable)
                        throw new InvalidCastException();
                    return potentialDefinition.Preceding.GetSetDefinition();
                case PhiVariable phi:
                    if (phi.Node.PossibleValues.Where(kvp => kvp.Key.IsExecutable).Select(kvp => kvp.Value).Distinct().Count() > 1)
                        throw new InvalidCastException();
                    return phi.Node.PossibleValues.FirstOrDefault(kvp => kvp.Key.IsExecutable).Value.GetSetDefinition();
                default:
                    throw new NotImplementedException();
            }
        }

        protected static string SetState_ToString(SetState state)
        {
            switch (state)
            {
                default:
                case SetState.Set:
                    return "#";
                case SetState.PotentiallyUnset:
                    return "?";
                case SetState.Unset:
                    return "⊥";
            }
        }
        public override string ToString()
            => $"{Name} {SetState_ToString(State)}{ssaIndex}";
        public abstract bool ValuesEqual(SSADefinition other);
        public bool Equals(SSADefinition other)
        {
            if (State != other.State)
                return false;
            if (!string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase))
                return false;
            return ValuesEqual(other);
        }

        public override bool Equals(object obj)
            => obj is SSADefinition ssaDef && Equals(ssaDef);
        public override int GetHashCode()
            => Name.ToLower().GetHashCode();

        private class SSAReferenceEqualityComparer : IEqualityComparer<SSADefinition>
        {
            public static SSAReferenceEqualityComparer Instance { get; } =
                new SSAReferenceEqualityComparer();
            public bool Equals(SSADefinition x, SSADefinition y)
                => x == y;
            public int GetHashCode(SSADefinition obj)
                => obj.GetHashCode();
        }
    }

    public class SSASetDefinition : SSADefinition
    {
        private static readonly Dictionary<(string, IRCall), SSASetDefinition> postCallDefinitions =
            new Dictionary<(string, IRCall), SSASetDefinition>();

        public IRAssign DefinedAt { get; }
        public override bool IsInvariant => State != SetState.PotentiallyUnset && (AssignedAt?.IsInvariant ?? false);
        public override Type Type => AssignedType;
        public Type AssignedType { get; set; }

        public SSASetDefinition(string name, IRAssign assignedAt) : base(name, SetState.Set, assignedAt)
        {
            DefinedAt = assignedAt;
            potentialUnsetSites = new Dictionary<IRUnset, SSADefinition>();
            AssignedType = DefinedAt?.Value?.Type ?? typeof(Encapsulation.Structure);
        }
        public SSASetDefinition(string name, IRUnset unsetAt) : base(name, SetState.Unset, unsetAt)
        {
            AssignedType = null;
        }
        private SSASetDefinition(string name, IRCall assignedIn) : base(name, SetState.Set, assignedIn)
        {
            AssignedType = typeof(Encapsulation.Structure);
        }
        public static SSASetDefinition FromCallSite(string name, IRCall assignedIn)
        {
            if (postCallDefinitions.TryGetValue((name, assignedIn), out SSASetDefinition result))
                return result;
            result = new SSASetDefinition(name, assignedIn);
            postCallDefinitions[(name, assignedIn)] = result;
            return result;
        }
        private SSASetDefinition(SSASetDefinition definition, IRUnset potentiallyUnsetAt) :
            base(definition.Name, SetState.PotentiallyUnset, potentiallyUnsetAt)
        {
            DefinedAt = definition.DefinedAt;
            potentialUnsetSites = definition.potentialUnsetSites;
        }
        public override SSADefinition PotentiallyUnset(IRUnset potentiallyUnsetAt, bool writeReplaceChain)
        {
            if (State == SetState.Unset)
                return this;

            if (!potentialUnsetSites.TryGetValue(potentiallyUnsetAt, out SSADefinition result))
            {
                result = new SSASetDefinition(this, potentiallyUnsetAt);
                potentialUnsetSites[potentiallyUnsetAt] = result;
            }
            if (writeReplaceChain)
            {
                result.Replaces.Add(this);
                ReplacedBy.Add(result);
            }
            return result;
        }
        public override InterimConstantValue Evaluate()
            => (DefinedAt.Value as IEvaluatableToConstant).Evaluate();
        
        public override bool ValuesEqual(SSADefinition other)
        {
            if (other is SSASetDefinition setDefinition)
            {
                if (setDefinition.DefinedAt == null)
                    return false;
                if (other == this)
                    return true;
                return DefinedAt?.Value.Equals(setDefinition.DefinedAt.Value) ?? false;
            }
            return other.Equals(this);
        }
    }
    public class SSAPotentialDefinition : SSADefinition, IMultipleOperandInstruction
    {
        private static readonly Dictionary<(IRUnset, SSASetDefinition), SSAPotentialDefinition> potentialSets =
            new Dictionary<(IRUnset, SSASetDefinition), SSAPotentialDefinition>();

        public SSADefinition Preceding { get; private set; }
        public SSADefinition Succeeding { get; private set; }
        public IRUnset Conditional { get; }
        public override Type Type => State == SetState.Set ?
            PhiNode<SSADefinition>.GetFirstCommonBaseType(Preceding.Type, Succeeding.Type) : null;
        public override bool IsInvariant => (Conditional?.IsInvariant ?? false) &&
            !Conditional.IsExecutable && Preceding.IsInvariant;

        public IEnumerable<IInterimOperand> Operands
        {
            get
            {
                short sourceLine = Conditional?.SourceLine ?? -1;
                short sourceColumn = Conditional?.SourceColumn ?? -1;
                yield return new InterimResolvedReference(Preceding, sourceLine, sourceColumn);
                yield return new InterimResolvedReference(Succeeding, sourceLine, sourceColumn);
            }
        }
        public int OperandCount => 2;

        internal SSAPotentialDefinition(SSADefinition preceding, SSASetDefinition succeeding) :
            base(preceding.Name, (SetState)Math.Min((int)preceding.State, (int)succeeding.State), succeeding.AssignedAt)
        {
            if (!preceding.Name.Equals(succeeding.Name, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Preceding and suceeding definitions must share a name.");
            if (succeeding.State != SetState.Set)
                throw new ArgumentException("The succeeding definition must be definitively set.");
            Preceding = preceding;
            Succeeding = succeeding;
            potentialUnsetSites = new Dictionary<IRUnset, SSADefinition>();
            Conditional = preceding.AssignedAt as IRUnset;
        }
        private SSAPotentialDefinition(SSAPotentialDefinition definition, IRUnset potentiallyUnsetAt) :
            base(definition.Name, SetState.PotentiallyUnset, potentiallyUnsetAt)
        {
            Preceding = definition.Preceding;
            Succeeding = definition.Succeeding;
            potentialUnsetSites = definition.potentialUnsetSites;
            Conditional = definition.Conditional;
        }
        private SSAPotentialDefinition(SSASetDefinition potentialDefinition, IRUnset condition) :
            base(potentialDefinition.Name, SetState.PotentiallyUnset, potentialDefinition.AssignedAt)
        {
            Preceding = null;
            Succeeding = potentialDefinition;
            potentialUnsetSites = new Dictionary<IRUnset, SSADefinition>();
            Conditional = condition;
        }
        public static SSAPotentialDefinition PotentiallySet(SSASetDefinition potentialDefinition, IRUnset condition, bool writeReplaceChain)
        {
            if (!potentialSets.TryGetValue((condition, potentialDefinition), out SSAPotentialDefinition result))
            {
                result = new SSAPotentialDefinition(potentialDefinition, condition);
                potentialSets[(condition, potentialDefinition)] = result;
            }
            if (writeReplaceChain)
            {
                result.Replaces.Add(potentialDefinition);
                potentialDefinition.ReplacedBy.Add(result);
            }
            return result;
        }

        public override SSADefinition PotentiallyUnset(IRUnset potentiallyUnsetAt, bool writeReplaceChain)
        {
            if (State == SetState.Unset)
                return this;

            if (!potentialUnsetSites.TryGetValue(potentiallyUnsetAt, out SSADefinition result))
            {
                result = new SSAPotentialDefinition(this, potentiallyUnsetAt);
                potentialUnsetSites[potentiallyUnsetAt] = result;
            }
            if (writeReplaceChain)
            {
                result.Replaces.Add(this);
                ReplacedBy.Add(result);
            }
            return result;
        }

        public void ForEachOperand(Action<IInterimOperand> action)
        {
            action(new InterimResolvedReference(Preceding, -1, -1));
            action(new InterimResolvedReference(Succeeding, -1, -1));
        }
        public void MutateEachOperand(Func<IInterimOperand, IInterimOperand> mutateFunc)
        {
            Preceding = Mutate(mutateFunc, Preceding);
            Succeeding = Mutate(mutateFunc, Succeeding);
        }
        public bool AnyOperand(Func<IInterimOperand, bool> predicate)
            => EvaluatePredicate(predicate, Preceding) || EvaluatePredicate(predicate, Succeeding);
        public bool AllOperands(Func<IInterimOperand, bool> predicate)
            => EvaluatePredicate(predicate, Preceding) && EvaluatePredicate(predicate, Succeeding);
        private static SSADefinition Mutate(Func<IInterimOperand, IInterimOperand> func, SSADefinition definition)
        {
            IInterimOperand result = func(new InterimResolvedReference(definition, -1, -1));
            return ((InterimResolvedReference)result).Reference;
        }
        private static bool EvaluatePredicate(Func<IInterimOperand, bool> predicate, SSADefinition definition)
            => predicate(new InterimResolvedReference(definition, -1, -1));
        public override InterimConstantValue Evaluate()
        {
            if (!IsInvariant)
                throw new InvalidOperationException();
            return Preceding.Evaluate();
        }

        public override bool ValuesEqual(SSADefinition other)
        {
            if (!Conditional?.IsExecutable ?? false)
                return Preceding.Equals(other);
            return other is SSAPotentialDefinition potentialDefinition &&
                potentialDefinition.Conditional == Conditional &&
                potentialDefinition.Succeeding.Equals(Succeeding) &&
                potentialDefinition.Preceding.Equals(Preceding);
        }
    }
    public class PhiVariable : SSADefinition
    {
        private readonly SetState internalSetState = SetState.Set;

        public PhiNode<SSADefinition> Node { get; }
        public override SetState State
        {
            get
            {
                IEnumerable<SSADefinition> possibleValues = Node.PossibleValues.Values;
                if (possibleValues.Any(v => v.State < SetState.Set))
                {
                    if (possibleValues.All(v => v.State == SetState.Unset))
                        return SetState.Unset;
                    else
                        return SetState.PotentiallyUnset;
                }
                return internalSetState;
            }
        }
        public override bool IsInvariant => Node.IsInvariant;
        public override Type Type => Node.Type;

        public PhiVariable(string name, PhiNode<SSADefinition> node) : base(name)
        {
            Node = node;
        }

        public PhiVariable(PhiVariable phiVariable, IRUnset potentiallyUnsetAt) : base(phiVariable.Name, potentiallyUnsetAt)
        {
            Node = phiVariable.Node;
            internalSetState = SetState.PotentiallyUnset;
        }

        public override SSADefinition PotentiallyUnset(IRUnset potentiallyUnsetAt, bool writeReplaceChain)
        {
            if (!potentialUnsetSites.TryGetValue(potentiallyUnsetAt, out SSADefinition result))
            {
                result = new PhiVariable(this, potentiallyUnsetAt);
                potentialUnsetSites[potentiallyUnsetAt] = result;
            }
            if (writeReplaceChain)
            {
                result.Replaces.Add(this);
                ReplacedBy.Add(result);
            }
            return result;
        }

        public override InterimConstantValue Evaluate()
            => Node.Evaluate();
        
        public override bool ValuesEqual(SSADefinition other)
        {
            if (IsInvariant)
            {
                return Node.PossibleValues.FirstOrDefault(kvp => kvp.Key.IsExecutable).Value?.Equals(other) ?? false;
            }
            if (other is PhiVariable otherPhi)
            {
                IEnumerable<BasicBlock> possibleValues = Node.PossibleValues.Keys;
                Dictionary<BasicBlock, SSADefinition> otherPossibleValues = otherPhi.Node.PossibleValues;
                possibleValues = possibleValues.Where(key => key.IsExecutable);
                if (possibleValues.Count() != otherPossibleValues.Where(kvp => kvp.Key.IsExecutable).Count())
                    return false;
                
                return possibleValues.All(
                        key =>
                        otherPossibleValues.ContainsKey(key) &&
                        (otherPossibleValues[key] == Node.PossibleValues[key] ||
                        otherPossibleValues[key].Equals(Node.PossibleValues[key])));
            }
            return false;
        }

        public override string ToString()
            => $"{Name} #{ssaIndex}";
    }

    public class PhiNode : PhiNode<SSADefinition>
    {
        protected override bool ObjIsInvariant(SSADefinition obj)
            => obj.IsInvariant;
        protected override Type ObjType(SSADefinition obj)
            => obj.Type;
        public PhiVariable Result { get; }

        public PhiNode(string name)
        {
            Result = new PhiVariable(name, this);
        }

        protected override InterimConstantValue EvaluateObj(SSADefinition obj)
            => obj.Evaluate();

        protected override void MutateEachOperand(Func<IInterimOperand, IInterimOperand> mutateFunc)
        {
            foreach (BasicBlock block in PossibleValues.Keys)
                PossibleValues[block] = ((InterimResolvedReference)mutateFunc(ValueAsOperand(PossibleValues[block]))).Reference;
        }
        protected override IInterimOperand ValueAsOperand(SSADefinition item)
        {
            SSASetDefinition ssaDef = item as SSASetDefinition;
            short sourceLine = ssaDef?.DefinedAt.SourceLine ?? -1;
            short sourceColumn = ssaDef?.DefinedAt.SourceColumn ?? -1;
            return new InterimResolvedReference(item, sourceLine, sourceColumn);
        }

    }
    public class PhiOperand<T> : PhiNode<T> where T : IRInstruction, ISingleOperandInstruction
    {
        protected override bool ObjIsInvariant(T obj)
            => obj == null || (obj.IsInvariant && obj.Operand is IEvaluatableToConstant);
        protected override Type ObjType(T obj)
            => obj.Operand.Type;

        public override InterimConstantValue Evaluate()
        {
            if (!PossibleValues.Any())
                return null;
            return base.Evaluate();
        }

        protected override InterimConstantValue EvaluateObj(T obj)
            => (obj.Operand as IEvaluatableToConstant)?.Evaluate();

        protected override void MutateEachOperand(Func<IInterimOperand, IInterimOperand> mutateFunc)
        {
            foreach (BasicBlock block in PossibleValues.Keys)
                PossibleValues[block].Operand = mutateFunc(PossibleValues[block].Operand);
        }
        protected override IInterimOperand ValueAsOperand(T item)
            => item.Operand;
    }
    
    public abstract class PhiNode<T> : IMultipleOperandInstruction
    {
        public virtual bool IsInvariant
        {
            get
            {
                IEnumerable<KeyValuePair<BasicBlock, T>> reachableValues =
                    PossibleValues.Where(kvp => kvp.Key.IsExecutable);
                // Return true if there is exactly one reachable value and it is invariant.
                return reachableValues.Distinct().Count() == 1 && ObjIsInvariant(reachableValues.First().Value);
            }
        }
        protected abstract bool ObjIsInvariant(T obj);
        public virtual Type Type
        {
            get
            {
                IEnumerable<T> reachableValues =
                    PossibleValues.Where(kvp => kvp.Key.IsExecutable).Select(kvp => kvp.Value);
                if (!reachableValues.Any())
                    return null;
                Type proposedType = ObjType(reachableValues.FirstOrDefault());
                foreach (T variable in reachableValues.Skip(1))
                    proposedType = GetFirstCommonBaseType(proposedType, ObjType(variable));

                return proposedType;
            }
        }
        protected abstract Type ObjType(T obj);
        public Dictionary<BasicBlock, T> PossibleValues { get; } = new Dictionary<BasicBlock, T>();

        IEnumerable<IInterimOperand> IMultipleOperandInstruction.Operands => Operands;

        protected virtual IEnumerable<IInterimOperand> Operands => PossibleValues.Values.Select(ValueAsOperand);

        int IMultipleOperandInstruction.OperandCount => PossibleValues.Count;

        public virtual InterimConstantValue Evaluate()
        {
            if (!IsInvariant)
                throw new InvalidOperationException();
            T variable = PossibleValues.First(kvp => kvp.Key.IsExecutable).Value;
            return EvaluateObj(variable);
        }
        protected abstract InterimConstantValue EvaluateObj(T obj);

        public static Type GetFirstCommonBaseType(Type typeA, Type typeB)
        {
            if (typeA == null || typeB == null) return null;

            Type current = typeA;
            while (current != null)
            {
                if (current.IsAssignableFrom(typeB))
                {
                    return current;
                }
                current = current.BaseType;
            }

#if DEBUG
            throw new Exceptions.KOSYouShouldNeverSeeThisException($"Couldn't find a base class between {typeA} and {typeB}, when all kOS types should derive from {nameof(Encapsulation.Structure)}.");
#else
            return typeof(Encapsulation.Structure);
#endif
        }

        void IOperandInstructionBase.ForEachOperand(Action<IInterimOperand> action)
        {
            foreach (T item in PossibleValues.Values)
                action(ValueAsOperand(item));
        }
        void IOperandInstructionBase.MutateEachOperand(Func<IInterimOperand, IInterimOperand> mutateFunc)
            => MutateEachOperand(mutateFunc);
        protected abstract void MutateEachOperand(Func<IInterimOperand, IInterimOperand> mutateFunc);
        bool IOperandInstructionBase.AnyOperand(Func<IInterimOperand, bool> predicate)
            => PossibleValues.Values.Select(ValueAsOperand).Any(predicate);
        bool IOperandInstructionBase.AllOperands(Func<IInterimOperand, bool> predicate)
            => PossibleValues.Values.Select(ValueAsOperand).All(predicate);
        protected abstract IInterimOperand ValueAsOperand(T item);
    }
}
