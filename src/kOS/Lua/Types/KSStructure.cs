using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using KeraLua;
using kOS.Safe.Compilation;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using kOS.Safe.Execution;
using kOS.Safe.Function;
using Debug = UnityEngine.Debug;

namespace kOS.Lua.Types
{
    public class KSStructure : LuaTypeBase
    {
        private static readonly CalculatorStructure structureCalculator = new CalculatorStructure();
        private static readonly Type[] bindingTypes = { typeof(Structure) };
        public override string MetatableName => "Structure";
        public override Type[] BindingTypes => bindingTypes;

        public KSStructure(KeraLua.Lua state)
        {
            state.NewMetaTable(MetatableName);
            state.PushString(MetatableName);
            state.SetField(-2, "__type");
            AddMethod(state, "__index", StructureIndex);
            AddMethod(state, "__newindex", StructureNewIndex);
            AddMethod(state, "__pairs", StructurePairs);
            AddMethod(state, "__gc", Binding.CollectObject);
            AddMethod(state, "__len", StructureLength);
            AddMethod(state, "__tostring", StructureToString);
            AddMethod(state, "__add", StructureAdd);
            AddMethod(state, "__sub", StructureSubtract);
            AddMethod(state, "__mul", StructureMultiply);
            AddMethod(state, "__div", StructureDivide);
            AddMethod(state, "__pow", StructurePower);
            AddMethod(state, "__unm", StructureUnary);
            AddMethod(state, "__eq", StructureEqual);
            AddMethod(state, "__lt", StructureLessThan);
            AddMethod(state, "__le", StructureLessEqualThan);
            state.Pop(1);
        }

        private static int StructureAdd(IntPtr L) => StructureOperator(L, structureCalculator.Add);
        private static int StructureSubtract(IntPtr L) => StructureOperator(L, structureCalculator.Subtract);
        private static int StructureMultiply(IntPtr L) => StructureOperator(L, structureCalculator.Multiply);
        private static int StructureDivide(IntPtr L) => StructureOperator(L, structureCalculator.Divide);
        private static int StructurePower(IntPtr L) => StructureOperator(L, structureCalculator.Power);
        private static int StructureEqual(IntPtr L) => StructureOperator(L, structureCalculator.Equal);
        private static int StructureLessThan(IntPtr L) => StructureOperator(L, structureCalculator.LessThan);
        private static int StructureLessEqualThan(IntPtr L) => StructureOperator(L, structureCalculator.LessThanEqual);
        
        private static int StructureOperator(IntPtr L, Func<OperandPair, object> operatorMethod)
        {
            var state = KeraLua.Lua.FromIntPtr(L);
            var binding = Binding.Bindings[state.MainThread.Handle];
            var pair = new OperandPair(Binding.ToCSharpObject(state, 1, binding), Binding.ToCSharpObject(state, 2, binding));
            return (int)Binding.LuaExceptionCatch(() => Binding.PushLuaType(state, operatorMethod(pair), binding), state);
        }
        
        private static int StructureUnary(IntPtr L)
        {
            var state = KeraLua.Lua.FromIntPtr(L);
            var binding = Binding.Bindings[state.MainThread.Handle];
            var obj = Binding.ToCSharpObject(state, 1, binding);
            if (obj == null) return 0;
            MethodInfo unaryMethod = obj.GetType().GetMethod("op_UnaryNegation", BindingFlags.FlattenHierarchy |BindingFlags.Static | BindingFlags.Public);
            if (unaryMethod != null)
                return (int)Binding.LuaExceptionCatch(() => Binding.PushLuaType(state, unaryMethod.Invoke(null, new[]{obj}), binding), state);
            Binding.LuaExceptionCatch(() => throw new KOSUnaryOperandTypeException("negate", obj), state);
            return 0;
        }

        private static int StructureLength(IntPtr L)
        {
            var state = KeraLua.Lua.FromIntPtr(L);
            var binding = Binding.Bindings[state.MainThread.Handle];
            var structure = binding.Objects[state.ToUserData(1)] as Structure;
            if (!structure.HasSuffix("LENGTH"))
                return state.Error("attempt to get length of a Structure with no length suffix");
            state.PushString("LENGTH");
            return (int)Binding.LuaExceptionCatch(() => PushSuffixResult(state, binding, structure, -1), state);
        }

        private static int StructureToString(IntPtr L)
        {
            var state = KeraLua.Lua.FromIntPtr(L);
            var structure = Binding.Bindings[state.MainThread.Handle].Objects[state.ToUserData(1)];
            var structureString = (string)Binding.LuaExceptionCatch(() => structure.ToString(), state);
            if (structure is IEnumerable<Structure>)
            {   // make enum structures ToString() method show 1 base indexed values in lua
                // replaces "\n  [*number*]" with "\n  [*number+1*]"
                state.PushString(Regex.Replace(structureString, @"\n\s*\[([0-9]+)\]", (match) =>
                    Regex.Replace(match.Groups[0].Value, match.Groups[1].Value, (int.Parse(match.Groups[1].Value) + 1).ToString())
                ));
            }
            else
                state.PushString(structureString);
            return 1;
        }
        
        private static int StructureIndex(IntPtr L)
        {
            var state = KeraLua.Lua.FromIntPtr(L);
            var binding = Binding.Bindings[state.MainThread.Handle];
            object obj = binding.Objects[state.ToUserData(1)];
            var structure = obj as Structure;
            if (structure == null)
                return state.Error(string.Format("attempt to index a {0} value", obj.GetType().Name));

            return (int)Binding.LuaExceptionCatch(() => PushSuffixResult(state, binding, structure, 2), state);
        }

        private static int PushSuffixResult(KeraLua.Lua state, Binding.BindingData binding, Structure structure, int index)
        {
            object pushValue = null;
            if (state.Type(index) == LuaType.Number && structure is IIndexable indexable)
            {
                pushValue = Structure.ToPrimitive(indexable.GetIndex((int)state.ToInteger(index)-(structure is Lexicon? 0 : 1), true));
                return Binding.PushLuaType(state, pushValue, binding);
            }

            var suffixName = state.ToString(index).ToLower();
            
            if (structure is TerminalInput && suffixName == "getchar")
            {
                state.PushCFunction(GetChar);
                return 1;
            }
            
            var result = structure.GetSuffix(suffixName, true);
            if (result == null)
                return Binding.PushLuaType(state, null, binding);
            
            if (result.HasValue)
            {
                pushValue = Structure.ToPrimitive(result.Value);
            }
            else if (result is DelegateSuffixResult delegateResult && delegateResult.RawDelInfo.ReturnType != typeof(void)
                                                                   && delegateResult.RawDelInfo.Parameters.Length == 0
                                                                   && suffixName != "pop" && suffixName != "peek")
            {
                var callResult = delegateResult.RawCall(null);
                delegateResult.RawSetValue(Structure.FromPrimitiveWithAssert(callResult));
                pushValue = Structure.ToPrimitive(delegateResult.Value);
            } else
            {
                pushValue = result as DelegateSuffixResult; // if its somehow not DelegateSuffixResult push null
            }
            return Binding.PushLuaType(state, pushValue, binding);
        }

        private static int StructureNewIndex(IntPtr L)
        {
            var state = KeraLua.Lua.FromIntPtr(L);
            var binding = Binding.Bindings[state.MainThread.Handle];
            var obj = binding.Objects[state.ToUserData(1)];
            var structure = obj as Structure;
            if (structure == null)
                return state.Error(string.Format("attempt to index a {0} value", obj.GetType().Name));
            var newValue = Binding.ToCSharpObject(state, 3, binding);
            if (newValue == null) return 0;
            if (structure is IIndexable && state.Type(2) == LuaType.Number)
            {
                var index = (int)state.ToInteger(2) - (structure is Lexicon? 0 : 1);
                Binding.LuaExceptionCatch(() =>
                    (structure as IIndexable).SetIndex(index, Structure.FromPrimitive(newValue) as Structure), state);
            }
            else
            {
                var index = state.ToString(2);
                Binding.LuaExceptionCatch(() =>
                {
                    if (!structure.SetSuffix(index, Structure.FromPrimitive(newValue)))
                        throw new Exception($"Suffix \"{index}\" not found on Structure {structure.KOSName}");
                }, state);
            }
            return 0;
        }
        
        private static int StructurePairs(IntPtr L)
        {
            var state = KeraLua.Lua.FromIntPtr(L);
            var binding = Binding.Bindings[state.MainThread.Handle];
            var structure = binding.Objects[state.ToUserData(1)] as Structure;
            if (structure == null)
                return state.Error("pairs metamethod can only be called with a Structure type");

            state.PushInteger(1);
            var enumCount = (structure is IIndexable && structure is IEnumerable<Structure> enumerable)
                ? enumerable.Count()
                : 0;
            state.NewTable();
            var index = 1;
            for (; index <= enumCount; index++)
            {
                state.PushInteger(index);
                state.PushInteger(index);
                state.SetTable(-3);
            }
            foreach (var name in structure.GetSuffixNames())
            {
                state.PushInteger(index++);
                state.PushString(name);
                state.SetTable(-3);
            }

            state.PushCClosure(StructureNext, 2); // pass the starting index and table with index-suffix pairs
            state.PushCopy(1);
            return 2;
        }

        private static int StructureNext(IntPtr L)
        {
            var state = KeraLua.Lua.FromIntPtr(L);
            var binding = Binding.Bindings[state.MainThread.Handle];
            var structure = binding.Objects[state.ToUserData(1)] as Structure;
            if (structure == null)
                return state.Error("iterator can only be called with a Structure type");
            // ignore the second argument
            var currentIndex = state.ToInteger(KeraLua.Lua.UpValueIndex(1));
            state.PushCopy(KeraLua.Lua.UpValueIndex(2));
            state.PushInteger(currentIndex);
            state.GetTable(-2);

            Binding.LuaExceptionCatch(() => PushSuffixResult(state, binding, structure, -1), state);
            
            state.PushInteger(currentIndex+1);
            state.Copy(-1, KeraLua.Lua.UpValueIndex(1));
            state.Remove(-1);
            return 2;
        }
        
        private static int GetChar(IntPtr L)
        {
            return GetCharContinuation(L, 0, IntPtr.Zero);
        }

        private static int GetCharContinuation(IntPtr L, int status, IntPtr ctx)
        {
            var state = KeraLua.Lua.FromIntPtr(L);
            var shared = Binding.Bindings[state.MainThread.Handle].Shared;
            var q = shared.Screen.CharInputQueue;
            if (q.Count == 0)
                state.YieldK(0, 0, GetCharContinuation);
            state.PushString(q.Dequeue().ToString());
            return 1;
        }
    }
}
