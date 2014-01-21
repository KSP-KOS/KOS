using System;
using System.Collections.Generic;
using kOS.Binding;
using kOS.Command;
using kOS.Expression;
using kOS.Persistance;
using kOS.Utilities;

namespace kOS.Context
{
    public interface IExecutionContext
    {
        IVolume SelectedVolume { get; set; }
        Vessel Vessel { get; }
        List<IVolume> Volumes { get; }
        Dictionary<String, Variable> Variables { get; }
        List<KOSExternalFunction> ExternalFunctions { get; }
        IExecutionContext ParentContext { get; set; }
        IExecutionContext ChildContext { get; set; }
        ExecutionState State { get; set; }
        int Line { get; set; }
        void VerifyMount();
        bool KeyInput(char c);
        bool Type(char c);
        bool SpecialKey(kOSKeys key);
        char[,] GetBuffer();
        void StdOut(String text);
        void Put(String text, int x, int y);
        void Update(float time);
        void Push(IExecutionContext newChild);
        bool Break();
        Variable FindVariable(string varName);
        Variable CreateVariable(string varName);
        Variable FindOrCreateVariable(string varName);
        BoundVariable CreateBoundVariable(string varName);
        bool SwitchToVolume(int volID);
        bool SwitchToVolume(String volName);
        IVolume GetVolume(object volID);
        IExecutionContext GetDeepestChildContext();
        T FindClosestParentOfType<T>() where T : class, IExecutionContext;
        void UpdateLock(String name);
        Expression.Expression GetLock(String name);
        void Lock(ICommand command);
        void Lock(String name, Expression.Expression expression);
        void Unlock(ICommand command);
        void Unlock(String name);
        void UnlockAll();
        void Unset(String name);
        void UnsetAll();
        bool ParseNext(ref string buffer, out string cmd, ref int lineCount, out int lineStart);
        void SendMessage(SystemMessage message);
        int GetCursorX();
        int GetCursorY();
        object CallExternalFunction(String name, string[] parameters);
        bool FindExternalFunction(String name);
        void OnSave(ConfigNode node);
        void OnLoad(ConfigNode node);
        string GetVolumeBestIdentifier(IVolume selectedVolume);
    }
}