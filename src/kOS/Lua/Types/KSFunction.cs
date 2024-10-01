using System;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Execution;
using kOS.Safe.Function;
using Debug = UnityEngine.Debug;

namespace kOS.Lua.Types
{
    public class KSFunction : LuaTypeBase
    {
        private static readonly Type[] bindingTypes = { typeof(SafeFunctionBase), typeof(DelegateSuffixResult) };
        private static readonly string metatableName = "KerboscriptFunction";
        public override string MetatableName => metatableName;
        public override Type[] BindingTypes => bindingTypes;

        public KSFunction(KeraLua.Lua state)
        {
            state.NewMetaTable(MetatableName);
            state.PushString("__type");
            state.PushString(MetatableName);
            state.RawSet(-3);
            state.PushString("__call");
            state.PushCFunction(KSFunctionCall);
            state.RawSet(-3);
            state.PushString("__gc");
            state.PushCFunction(Binding.CollectObject);
            state.RawSet(-3);
            state.PushString("__tostring");
            state.PushCFunction(Binding.ObjectToString);
            state.RawSet(-3);
        }

        private static int KSFunctionCall(IntPtr L)
        {
            var state = KeraLua.Lua.FromIntPtr(L);
            var binding = Binding.bindings[state.MainThread.Handle];
            state.CheckUserData(1, metatableName);
            var ksFunction = binding.Objects[state.ToUserData(1)];
            
            var stack = (binding.Shared.Cpu as LuaCPU).Stack;
            stack.Clear();
            stack.PushArgument(new KOSArgMarkerType());
            for (int i = 2; i <= state.GetTop(); i++)
            {
                var arg = Binding.ToCSharpObject(state, i, binding);
                if (arg == null) break;
                stack.PushArgument(arg);
            }
            
            if (ksFunction is SafeFunctionBase function)
            {
                Binding.LuaExceptionCatch(() => function.Execute(binding.Shared), state);
                return Binding.PushLuaType(state, Structure.ToPrimitive(function.ReturnValue), binding);
            }
            if (ksFunction is DelegateSuffixResult delegateResult)
            {
                Binding.LuaExceptionCatch(() => delegateResult.Invoke(binding.Shared.Cpu), state);
                return Binding.PushLuaType(state, Structure.ToPrimitive(delegateResult.Value), binding);
            }
            return state.Error(string.Format("attempt to call a non function {0} value", ksFunction.GetType().Name));
        }
    }
}
