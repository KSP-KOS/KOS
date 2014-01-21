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
        Dictionary<string, Variable> Variables { get; }
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
        void StdOut(string text);
        void Put(string text, int x, int y);
        void Update(float time);
        void Push(IExecutionContext newChild);
        bool Break();
        Variable FindVariable(string varName);
        Variable CreateVariable(string varName);
        Variable FindOrCreateVariable(string varName);
        BoundVariable CreateBoundVariable(string varName);
        bool SwitchToVolume(int volID);
        bool SwitchToVolume(string volName);
        IVolume GetVolume(object volID);
        IExecutionContext GetDeepestChildContext();
        T FindClosestParentOfType<T>() where T : class, IExecutionContext;
        void UpdateLock(string name);
        Expression.Expression GetLock(string name);
        void Lock(ICommand command);
        void Lock(string name, Expression.Expression expression);
        void Unlock(ICommand command);
        void Unlock(string name);
        void UnlockAll();
        void Unset(string name);
        void UnsetAll();
        bool ParseNext(ref string buffer, out string cmd, ref int lineCount, out int lineStart);
        void SendMessage(SystemMessage message);
        int GetCursorX();
        int GetCursorY();
        object CallExternalFunction(string name, string[] parameters);
        bool FindExternalFunction(string name);
        void OnSave(ConfigNode node);
        void OnLoad(ConfigNode node);
        string GetVolumeBestIdentifier(IVolume selectedVolume);
    }
}