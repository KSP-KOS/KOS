using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
        public Dictionary<String, Expression> Locks = new Dictionary<string, Expression>();
        public List<Command> CommandLocks = new List<Command>();

        public ExecutionContext()
        {

        }
        
        public ExecutionContext(ExecutionContext parent)
        {
            this.ParentContext = parent;
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

        public virtual void Break()
        {
            ChildContext = null;
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
                String volName = (String)volID;

                foreach (Volume targetVolume in Volumes)
                {
                    if (targetVolume.Name == volName)
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
            if (Locks.ContainsKey(name))
            {
                Locks.Remove(name);
            }
            else if (ParentContext != null)
            {
                ParentContext.Unlock(name);
            }
        }

        public bool parseNext(ref string buffer, out string cmd)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                string c = buffer.Substring(i, 1);

                if (c == "\"")
                {
                    i = Utils.FindEndOfString(buffer, i + 1);
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

        /*
        internal void Add(string cmdString)
        {
            buffer += cmdString;
            string nextCmd;

            while (parseNext(out nextCmd))
            {
                try
                {
                    Command cmd = Command.Get(nextCmd, Cpu, this);
                    Add(cmd);
                }
                catch (kOSException e)
                {
                    StdOut(e.ToString());
                    Queue.Clear(); // HALT!!
                }
            }
        }

        internal void Add(Command cmd)
        {
            if (cmd != null)
            {
                Queue.Enqueue(cmd);
            }
            else
            {
                StdOut("Syntax error.");
                Queue.Clear();
            }
        }

        public bool parseNext(out string cmd)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                string c = buffer.Substring(i, 1);

                if (c == "\"")
                {
                    i = Utils.FindEndOfString(buffer, i + 1);
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
            }

            cmd = ""; 
            return false;
        }

        public void PushContext(ExecutionContext newContext)
        {
            if (newContext is Interpreter)
            {
                Cpu.PushInterpreter((Interpreter)newContext);
            }
            else
            {
                child = newContext;
            }
        }

        internal Volume GetVolume(string identifier)
        {
            return Cpu.GetVolume(identifier);
        }

        public virtual void Update(float time)
        {
            if (child != null)
            {
                child.Update(time);
                if (child.state == CommandState.OK)
                {
                    child = null;
                }
            }

            while (Queue.Count > 0)
            {
                var currentCmd = Queue.Peek();

                if (currentCmd != null)
                {
                    try
                    {
                        if (currentCmd.State == CommandState.NEW)
                        {
                            currentCmd.Evaluate();
                        }
                        else if (currentCmd.State == CommandState.WAIT)
                        {
                            currentCmd.Update(time);
                            break;
                        }
                        else if (currentCmd.State == CommandState.OK)
                        {
                            Queue.Dequeue();
                        }
                    }
                    catch (kOSException e)
                    {
                        StdOut(e.Message);
                        Queue.Clear(); // Halt!
                    }
                }
            }
        }

        public virtual void StdOut(string value)
        {
            if (parent != null)
            {
                parent.StdOut(value);
            }
        }

        public virtual void ClearScreen()
        {
            parent.ClearScreen();
        }

        public virtual BindingManager getBindingManager()
        {
            return parent.getBindingManager();
        }

        public virtual Variable CreateVariable(string varName)
        {
            Variable variable = FindVariable(varName);
            if (variable != null) throw new kOSException("Variable '" + varName + "' already exists.");

            variables[varName.ToUpper()] = new Variable(Cpu, varName.ToUpper());
            return variables[varName.ToUpper()];
        }

        public virtual Variable FindOrCreateVariable(string varName)
        {
            Variable variable = FindVariable(varName);
            if (variable != null)
            {
                return variable;
            }
            else
            {
                variables[varName.ToUpper()] = new Variable(Cpu, varName.ToUpper());
                return variables[varName.ToUpper()];
            }
        }

        public virtual Variable FindVariable(string varName)
        {
            if (variables.ContainsKey(varName.ToUpper()))
            {
                return variables[varName.ToUpper()];
            }
            else if (parent != null)
            {
                return parent.FindVariable(varName);
            }

            return null;
        }

        internal bool SwitchToVolume(int volID)
        {
            return Cpu.SwitchToVolume(volID);

        }

        internal bool SwitchToVolume(string targetVolume)
        {
            return Cpu.SwitchToVolume(targetVolume);
        }

        internal void Lock(string InstanceName, Command cmd)
        {
            Cpu.Lock(InstanceName, cmd);
        }

        internal void Lock(Command cmd)
        {
            Cpu.Lock(cmd);
        }
        
        internal void Unlock(string InstanceName)
        {
            Cpu.Unlock(InstanceName);
        }

        internal void Unlock(Command cmd)
        {
            Cpu.Unlock(cmd);
        }

        internal void UnlockAll()
        {
            Cpu.UnlockAll();
        }

        internal CPU GetCpu()
        {
            if (this.Cpu != null) return Cpu;

            return parent.GetCpu();
        }
         * */




    }
}
