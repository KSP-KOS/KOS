using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using UnityEngine;

namespace kOS
{
    public enum ExecutionState { NEW, DONE, WAIT };
    public enum SpecialKey { HOME, END, DELETE };
    public enum SystemMessage { CLEARSCREEN, SHUTDOWN, RESTART };

    public class ExecutionContext
    {
        public static int COLUMNS = 50;
        public static int ROWS = 36;

        public CPU Cpu;
        public Queue<Command> Queue = new Queue<Command>();
        public String buffer;
        public ExecutionContext ParentContext = null;
        public ExecutionContext ChildContext = null;
        public ExecutionState State = ExecutionState.NEW;
        
        public virtual Volume SelectedVolume
        { 
            get { return ParentContext != null ? ParentContext.SelectedVolume : null; }
            set { if (ParentContext != null) ParentContext.SelectedVolume = value; } 
        }

        public virtual Vessel Vessel { get { return ParentContext != null ? ParentContext.Vessel : null; } }
        public virtual List<Volume> Volumes { get { return ParentContext != null ? ParentContext.Volumes : null; } }
        public virtual Dictionary<String, Variable> Variables { get { return ParentContext != null ? ParentContext.Variables : null; } }
        public virtual List<kOSExternalFunction> ExternalFunctions { get { return ParentContext != null ? ParentContext.ExternalFunctions : null; } }
        public Dictionary<String, Expression> Locks = new Dictionary<string, Expression>();
        public List<Command> CommandLocks = new List<Command>();

        public ExecutionContext()
        {

        }
        
        public ExecutionContext(ExecutionContext parent)
        {
            this.ParentContext = parent;
        }

        public virtual void VerifyMount()
        {
            if (ParentContext != null) ParentContext.VerifyMount();
        }

        public bool KeyInput(char c)
        {
            if (ChildContext != null) return ChildContext.Type(c);

            return Type(c);
        }

        

        public virtual bool Type(char c)
        {
            if (ChildContext != null) return ChildContext.Type(c);

            return false;
        }

        public virtual bool SpecialKey(kOSKeys key)
        {
            if (ChildContext != null) return ChildContext.SpecialKey(key);

            return false;
        }

        public virtual char[,] GetBuffer()
        {
            return (ChildContext != null) ? ChildContext.GetBuffer() : null;
        }

        public virtual void StdOut(String text)
        {
            if (ParentContext != null) ParentContext.StdOut(text);
        }

        public virtual void Put(String text, int x, int y)
        {
            if (ParentContext != null) ParentContext.Put(text, x, y);
        }

        public virtual void Update(float time)
        {
            // Process Command locks
            foreach (Command command in new List<Command>(CommandLocks))
            {
                command.Update(time);
            }

            if (ChildContext != null)
            {
                if (ChildContext.State == ExecutionState.DONE)
                {
                    ChildContext = null;
                }
                else
                {
                    ChildContext.Update(time);
                }
            }
        }

        public virtual void Push(ExecutionContext newChild)
        {
            ChildContext = newChild;
        }

        public virtual bool Break()
        {
            if (ParentContext != null) return ParentContext.Break();

            return false;
        }

        public Variable FindVariable(string varName)
        {
            varName = varName.ToLower();

            Variable v = Variables.ContainsKey(varName) ? Variables[varName] : null;

            if (v == null && ParentContext != null)
            {
                v = ParentContext.FindVariable(varName);
            }

            if (v != null && Locks.ContainsKey(varName))
            {
                v.Value = Locks[varName].GetValue();
            }

            return v;
        }

        public Variable CreateVariable(string varName)
        {
            varName = varName.ToLower();

            var v = new Variable();
            Variables.Add(varName, v);
            return v;
        }

        public Variable FindOrCreateVariable(string varName)
        {
            varName = varName.ToLower();

            Variable v = FindVariable(varName);

            if (v == null)
            {
                v = CreateVariable(varName);
            }

            return v;
        }

        public virtual BoundVariable CreateBoundVariable(string varName)
        {
            return ParentContext.CreateBoundVariable(varName);
        }

        public virtual bool SwitchToVolume(int volID)
        {
            if (ParentContext != null) return ParentContext.SwitchToVolume(volID);

            return false;
        }

        public virtual bool SwitchToVolume(String volName)
        {
            if (ParentContext != null) return ParentContext.SwitchToVolume(volName);

            return false;
        }

        public virtual Volume GetVolume(object volID)
        {
            if (volID is int)
            {
                if (Volumes.Count > (int)volID) return Volumes[(int)volID];
            }
            else if (volID is String)
            {
                String volName = volID.ToString().ToUpper();

                foreach (Volume targetVolume in Volumes)
                {
                    if (targetVolume.Name.ToUpper() == volName)
                    {
                        return targetVolume;
                    }
                }

                int outVal;
                if (int.TryParse((String)volID, out outVal))
                {
                    if (Volumes.Count > outVal) return Volumes[outVal];
                }
            }

            throw new kOSException("Volume '" + volID.ToString() + "' not found");
        }

        public ExecutionContext GetDeepestChildContext()
        {
            return ChildContext == null ? this : ChildContext.GetDeepestChildContext();
        }

        public void UpdateLock(String name)
        {
            Expression e = GetLock(name);
            if (e != null)
            {
                var v = FindVariable(name);
                v.Value = e.GetValue();
            }
        }

        public virtual Expression GetLock(String name)
        {
            if (Locks.ContainsKey(name))
            {
                return Locks[name];
            }
            else
            {
                return ParentContext == null ? null : ParentContext.GetLock(name);
            }
        }

        public virtual void Lock(Command command)
        {
            CommandLocks.Add(command);
        }

        public virtual void Lock(String name, Expression expression)
        {
            name = name.ToLower();

            FindOrCreateVariable(name);

            if (!Locks.ContainsKey(name))
            {
                Locks.Add(name, expression);
            }
        }

        public virtual void Unlock(Command command)
        {
            CommandLocks.Remove(command);
            if (ParentContext != null) ParentContext.Unlock(command);
        }

        public virtual void Unlock(String name)
        {
            name = name.ToLower();

            if (Locks.ContainsKey(name))
            {
                Locks.Remove(name);
            }
            else if (ParentContext != null)
            {
                ParentContext.Unlock(name);
            }
        }

        public virtual void UnlockAll()
        {
            Locks.Clear();
            if (ParentContext != null) ParentContext.UnlockAll();
        }

        public bool parseNext(ref string buffer, out string cmd)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                string c = buffer.Substring(i, 1);

                if (c == "\"")
                {
                    i = Utils.FindEndOfString(buffer, i + 1);

                    if (i == -1)
                    {
                        cmd = "";
                        return false;
                    }
                }
                else if (c == ".")
                {
                    int pTest;
                    if (i == buffer.Length - 1 || int.TryParse(buffer.Substring(i + 1, 1), out pTest) == false)
                    {
                        cmd = buffer.Substring(0, i);
                        buffer = buffer.Substring(i + 1).Trim();
                        return true;
                    }
                }
                else if (c == "{")
                {
                    i = Utils.BraceMatch(buffer, i);

                    if (i == -1)
                    {
                        cmd = "";
                        return false;
                    }
                    else
                    {
                        // Do you see a period after this right brace? If not, let's just pretend there is one ok?
                        if (!buffer.Substring(i + 1).StartsWith("."))
                        {
                            cmd = buffer.Substring(0, i + 1);
                            buffer = buffer.Substring(i + 1).Trim();
                            return true;
                        }
                    }
                }
            }

            cmd = "";
            return false;
        }

        public virtual void SendMessage(SystemMessage message)
        {
            if (ParentContext != null) ParentContext.SendMessage(message);
        }

        public virtual int GetCursorX()
        {
            return ChildContext != null ? ChildContext.GetCursorX() : -1;
        }

        public virtual int GetCursorY()
        {
            return ChildContext != null ? ChildContext.GetCursorY() : -1;
        }

        public virtual object CallExternalFunction(String name, string[] parameters)
        {
            if (ParentContext != null) return ParentContext.CallExternalFunction(name, parameters);

            return null;
        }

        public virtual bool FindExternalFunction(String name)
        {
            if (ParentContext != null) return ParentContext.FindExternalFunction(name);

            return false;
        }

        public virtual void OnSave(ConfigNode node)
        {
            ConfigNode contextNode = new ConfigNode("context");

            contextNode.AddValue("context-type", this.GetType().ToString());

            if (ChildContext != null)
            {
                ChildContext.OnSave(contextNode);
            }

            node.AddNode(contextNode);
        }

        public virtual void OnLoad(ConfigNode node)
        {
        }

        public virtual string GetVolumeBestIdentifier(Volume SelectedVolume)
        {
            if (ParentContext != null) return ParentContext.GetVolumeBestIdentifier(SelectedVolume);

            return "";
        }
    }
}
