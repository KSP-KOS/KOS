using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace kOS.Safe.Compilation.IR
{
    public abstract class PhiBase<T>: IEnumerable<T>, IEnumerable<KeyValuePair<BasicBlock, T>>
    {
        protected readonly IEqualityComparer<T> objComparer = EqualityComparer<T>.Default;

        public virtual bool RequireExecutable { get; set; } = true;
        public Dictionary<BasicBlock, T> PossibleValues { get; } = new Dictionary<BasicBlock, T>();

        protected PhiBase()
        {
            objComparer = EqualityComparer<T>.Default;
        }
        protected PhiBase(IEqualityComparer<T> comparer)
        {
            objComparer = comparer;
        }

        public virtual bool IsInvariant
        {
            get
            {
                HashSet<T> reachableValues = GetPossibleValues();
                // Return true if there is exactly one reachable value and it is invariant.
                return reachableValues.Count == 1 && ObjIsInvariant(reachableValues.First());
            }
        }

        protected abstract bool ObjIsInvariant(T obj);

        public virtual Type Type
        {
            get
            {
                IEnumerable<T> reachableValues = GetPossibleValues();
                if (!reachableValues.Any())
                    return null;
                Type proposedType = ObjType(reachableValues.FirstOrDefault());
                foreach (T value in reachableValues.Skip(1))
                {
                    if (proposedType == null)
                        return null;
                    Type objType = ObjType(value);
                    if (objType == null)
                        return null;
                    if (proposedType != typeof(Encapsulation.Structure))
                        proposedType = GetFirstCommonBaseType(proposedType, ObjType(value));
                }

                return proposedType;
            }
        }

        protected abstract Type ObjType(T obj);

        protected virtual HashSet<T> GetPossibleValues()
        {
            HashSet<T> result = new HashSet<T>(objComparer);
            HashSet<T> visited = new HashSet<T>(objComparer);
            Queue<T> queue = new Queue<T>(this);

            while (queue.Count > 0)
            {
                T value = queue.Dequeue();
                if (value is IEnumerable<T> enumerable)
                {
                    if (visited.Add(value))
                    {
                        foreach (T nested in enumerable)
                            queue.Enqueue(nested);
                    }
                }
                else
                    result.Add(value);
            }
            return result;
        }

        protected bool AnyValueIs(Predicate<T> predicate)
        {
            HashSet<T> visited = new HashSet<T>(objComparer);
            Queue<T> queue = new Queue<T>(this);

            while (queue.Count > 0)
            {
                T value = queue.Dequeue();
                if (value is IEnumerable<T> enumerable)
                {
                    if (visited.Add(value))
                    {
                        foreach (T nested in enumerable)
                            queue.Enqueue(nested);
                    }
                }
                else if (predicate(value))
                    return true;
            }
            return false;
        }

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

        public virtual IEnumerator<KeyValuePair<BasicBlock, T>> GetEnumerator()
        {
            if (RequireExecutable)
                return PossibleValues.Where(kvp => kvp.Key.IsExecutable).GetEnumerator();
            else
                return PossibleValues.GetEnumerator();
        }
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            if (RequireExecutable)
                return PossibleValues.Where(kvp => kvp.Key.IsExecutable).Select(kvp => kvp.Value).GetEnumerator();
            else
                return PossibleValues.Values.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }

    public abstract class PhiOperand<T> : PhiBase<T>, IMultipleOperandInstruction
    {
        IEnumerable<IInterimOperand> IMultipleOperandInstruction.Operands => Operands;
        int IMultipleOperandInstruction.OperandCount => PossibleValues.Count;

        protected virtual IEnumerable<IInterimOperand> Operands => PossibleValues.Values.Select(ValueAsOperand);

        protected PhiOperand() : base() { }
        protected PhiOperand(IEqualityComparer<T> comparer) : base(comparer) { }

        protected abstract IInterimOperand ValueAsOperand(T item);
        protected abstract InterimConstantValue ObjAsConstant(T obj);

        public virtual InterimConstantValue Evaluate()
        {
            if (!IsInvariant)
                throw new InvalidOperationException();
            return ObjAsConstant(GetPossibleValues().First());
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
            => GetPossibleValues().Select(ValueAsOperand).Any(predicate);
        bool IOperandInstructionBase.AllOperands(Func<IInterimOperand, bool> predicate)
            => GetPossibleValues().Select(ValueAsOperand).All(predicate);
    }
}
