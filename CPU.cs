using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace kOS
{
    public class CPU : ExecutionContext
    {
        public object Parent;
        public enum Modes { READY, STARVED, OFF };
        public Modes Mode = Modes.READY;
        public String Context;
        public static Archive archive = new Archive();
        public BindingManager bindingManager;

        private Dictionary<String, Variable> variables = new Dictionary<String, Variable>();
        private Volume selectedVolume = null;
        private List<Volume> volumes = new List<Volume>();
        private int vesselPartCount;
            
        public override Vessel Vessel { get { return ((kOSProcessor)Parent).vessel; } }
        public override Dictionary<String, Variable> Variables { get { return variables; } }
        public override List<Volume> Volumes { get  { return volumes;} }
                
        public override Volume SelectedVolume
        {
            get { return selectedVolume; }
            set { selectedVolume = value; }
        }

        public CPU(object parent, string context)
        {
            this.Parent = parent;
            this.Context = context;

            Volumes.Add(archive);

            bindingManager = new BindingManager(this, Context);
        }

        public void Boot()
        {
            Mode = Modes.READY;

            Push(new InterpreterBootup(this));

            if (Volumes.Count > 1) 
                SelectedVolume = Volumes[1];
            else
                SelectedVolume = Volumes[0];
        }

        public bool IsAlive()
        {
            var partState = ((kOSProcessor)this.Parent).part.State;

            if (partState == PartStates.DEAD)
            {
                Mode = Modes.OFF;
                return false;
            }

            return true;
        }

        public void AttachHardDisk(Harddisk hardDisk)
        {
            Volumes.Add(hardDisk);
            SelectedVolume = hardDisk;
        }

        internal void ProcessElectricity(Part part, float time)
        {
            if (Mode == Modes.OFF) return;

            var electricReq = 0.05f * time;
            var result = part.RequestResource("ElectricCharge", electricReq) / electricReq;

            var newMode = (result < 0.5f) ? Modes.STARVED : Modes.READY;

            if (newMode == Modes.READY && Mode == Modes.STARVED)
            {
                Boot();
            }

            Mode = newMode;
        }

        public override bool SwitchToVolume(int volID)
        {
            if (Volumes.Count > volID)
            {
                SelectedVolume = Volumes[volID];
                return true;
            }

            return false;
        }

        public override bool SwitchToVolume(string targetVolume)
        {
            foreach (Volume volume in Volumes)
            {
                if (volume.Name.ToUpper() == targetVolume.ToUpper())
                {
                    SelectedVolume = volume;
                    return true;
                }
            }

            return false;
        }

        public override BoundVariable CreateBoundVariable(string varName)
        {
            varName = varName.ToLower();

            if (FindVariable(varName) == null)
            {
                variables.Add(varName, new BoundVariable());
                return (BoundVariable)variables[varName];
            }
            else
            {
                throw new kOSException("Cannot bind " + varName + "; name already taken.");
            }
        }

        public override void Update(float time)
        {
            bindingManager.Update(time);

            base.Update(time);

            if (Mode == Modes.STARVED)
            {
                ChildContext = null;
            }
            else if (Mode == Modes.OFF)
            {
                ChildContext = null;
            }

            // After booting
            if (ChildContext == null)
            {
                Push(new ImmediateMode(this));
            }
        }

        public override void SendMessage(SystemMessage message)
        {
            switch (message)
            {
                case SystemMessage.SHUTDOWN:
                    ChildContext = null;
                    Mode = Modes.OFF;
                    break;

                case SystemMessage.RESTART:
                    ChildContext = null;
                    Boot();
                    break;

                default:
                    base.SendMessage(message);
                    break;
            }
        }

        internal void UpdateVolumeMounts(List<Volume> attachedVolumes)
        {
            // Remove volumes that are no longer attached
            foreach (Volume volume in volumes)
            {
                if (!attachedVolumes.Contains(volume))
                {
                    volumes.Remove(volume);
                }
            }

            // Add volumes that have become attached
            foreach (Volume volume in attachedVolumes)
            {
                if (!volumes.Contains(volume))
                {
                    volumes.Add(volume);
                }
            }
        }
    }

    /*
    public class CPU
    {
        private float cursorBlinkFloat = 0;
        public bool CursorBlinkStat = false;
        
        public Dictionary<String, Variable> variables = new Dictionary<string, Variable>();
        public enum Modes { BOOTING, READY, STARVED, OFF };
        public Modes Mode = Modes.READY;
        public Command CurrentCommand = null;
        public Queue<Command> Queue = new Queue<Command>();
        public List<Command> DeferredCommands = new List<Command>();
        public object Parent;
        public float SessionTime;
        public List<Interpreter> InterpreterStack = new List<Interpreter>();
        public Interpreter ActiveInterpreter { get { return InterpreterStack.Last(); } }
        public int MemCapacity;
        public Volume SelectedVolume = null;
        public List<Volume> Volumes = new List<Volume>();
        public BindingManager bindingManager;
        public ExecutionContext executionContext;
        public Dictionary<String, Command> LockedCommands = new Dictionary<string, Command>();
        public List<Command> AnonymousLockedCommands = new List<Command>();
        public float PoweredState = 1.0f;
        public bool DebugMode = false;

        public static Archive archive = new Archive();

        private int vesselPartCount = 0;

        public CPU(object parent, params string[] contexts)
        {
            this.Parent = parent;

            bindingManager = new BindingManager(this, contexts);

            Boot();

            if (contexts.Contains("testTerm")) DebugMode = true;
        }

        public void AttachHardDisk(Harddisk hardDisk)
        {
            SelectedVolume = hardDisk;
            Volumes.Add(hardDisk);
            this.MemCapacity = SelectedVolume.Capacity;
        }

        public Vessel GetVessel()
        {
            if (Parent is kOSProcessor)
            {
                return ((kOSProcessor)Parent).vessel;
            }

            return null;
        }
            
        public void Update()
        {
            Update(Time.deltaTime);
        }

        public void Update(float time)
        {
            if (!DebugMode && (Mode == Modes.OFF || Parent == null || GetVessel() == null)) return;

            if (Mode == Modes.BOOTING)
            {
                ActiveInterpreter.Update(time);
                return;
            }

            if (Mode != Modes.STARVED && PoweredState < 0.25f)
            {
                Mode = Modes.STARVED;
                Queue.Clear();
                InterpreterStack.Clear();
                LockedCommands.Clear();
                return;
            }
            else if (Mode == Modes.STARVED)
            {
                if (PoweredState >= 0.25f) { Boot(); }
                else { return; }
            }

            UpdateParts();

            UpdateCursor(time);

            bindingManager.Update(time);

            SessionTime += time;

            ActiveInterpreter.Update(time);

            foreach (var item in new Dictionary<String, Command>(LockedCommands))
            {
                item.Value.Update(time);
            }

            foreach (var item in new List<Command>(AnonymousLockedCommands))
            {
                item.Update(time);
            }
        }

        public void Boot()
        {
            InterpreterStack.Clear();
            InterpreterStack.Add(new InterpreterBootup(this));
            Mode = Modes.BOOTING;
        }

        internal void EndBootMode()
        {
            InterpreterStack.Clear();
            InterpreterStack.Add(new InterpreterImmediate(this));
            Mode = Modes.READY;
        }

        public void UpdateParts()
        {
            if (Parent is kOSProcessor)
            {
                if (GetVessel().parts.Count != vesselPartCount)
                {
                    kOSProcessor parentProc = ((kOSProcessor)this.Parent);
                    kOSProcessor sisterProc;

                    // Look for sister units that have newly been added to the vessel
                    foreach (Part part in GetVessel().parts)
                    {
                        if (part != parentProc.part && PartIsKosProc(part, out sisterProc))
                        {
                            RegisterSisterProc(sisterProc);
                        }
                    }
                    
                    // Look for units that are no longer part of the vessel
                    foreach (var volume in new List<Volume>(Volumes))
                    {
                        if (volume is Harddisk && ((Harddisk)volume).Parent.vessel != parentProc.vessel)
                        {
                            Volumes.Remove(volume);
                        }
                    }

                    vesselPartCount = GetVessel().parts.Count;
                }
            }
        }

        private void RegisterSisterProc(kOSProcessor sisterProc)
        {
            this.Volumes.Add(sisterProc.hardDisk);
        }

        public bool PartIsKosProc(Part input, out kOSProcessor proc)
        {
            foreach (PartModule module in input.Modules)
            {
                if (module is kOSProcessor)
                { 
                    proc = (kOSProcessor)module; 
                    return true; 
                }
            }

            proc = null;
            return false;
        }

        private void UpdateCursor(float time)
        {
            cursorBlinkFloat += time;
            while (cursorBlinkFloat > 0.5f)
            {
                cursorBlinkFloat -= 0.5f;
                CursorBlinkStat = !CursorBlinkStat;
            }
        }
        
        public char[,] GetBuffer()
        {
            char[,] bufferRef = ActiveInterpreter.Buffer;
            char[,] retBuffer = new char[bufferRef.GetLength(0), bufferRef.GetLength(1)];

            for (int y = 0; y < bufferRef.GetLength(1); y++)
                for (int x = 0; x < bufferRef.GetLength(0); x++)
                {
                    char o = bufferRef[x, y];
                    if (o == 0) o = (char)32;
                    
                    retBuffer[x, y] = o;
                }

            return retBuffer;
        }

        public void Execute(String cmdString)
        {
            executionContext.Add(cmdString);
        }
        
        public Variable FindOrCreateVariable(string p)
        {
            Variable retVar = FindVariable(p);

            if (retVar == null)
            {
                retVar = new Variable(this, p.ToUpper());
                variables.Add(p.ToUpper(), retVar);
            }

            return retVar;
        }

        public Variable FindVariable(string p)
        {
            if (variables.ContainsKey(p.ToUpper()))
            {
                return variables[p.ToUpper()];
            }

            return null;
        }

        public Variable CreateVariable(string p)
        {
            if (!variables.ContainsKey(p.ToUpper()))
            {
                Variable retVar = new Variable(this, p.ToUpper());
                variables.Add(p.ToUpper(), retVar);

                return retVar;
            }

            return null;
        }

        public bool IsLocked(string lockName)
        {
            return LockedCommands.ContainsKey(lockName.ToUpper());
        }

        internal void Lock(string InstanceName, Command cmd)
        {
            Unlock(InstanceName);
            LockedCommands.Add(InstanceName.ToUpper(), cmd);
        }

        internal void Lock(Command cmd)
        {
            Unlock(cmd);
            AnonymousLockedCommands.Add(cmd);
        }

        internal void Unlock(string InstanceName)
        {
            if (LockedCommands.ContainsKey(InstanceName.ToUpper()))
            {
                LockedCommands.Remove(InstanceName.ToUpper());
            }
        }

        internal void Unlock(Command cmd)
        {
            if (AnonymousLockedCommands.Contains(cmd))
            {
                AnonymousLockedCommands.Remove(cmd);
            }
        }

        internal void UnlockAll()
        {
            LockedCommands.Clear();
            AnonymousLockedCommands.Clear();
        }

        public void PrintLine(string line)
        {
        }
        
        public void Backspace()
        {
            ActiveInterpreter.Type((char)8);
        }

        public void DeleteKey()
        {
            ActiveInterpreter.Delete();
        }

        public void Type(char ch)
        {
            ActiveInterpreter.Type(ch);
        }

        public void Enter()
        {
            ActiveInterpreter.Type((char)13);
        }

        public void ClearScreen()
        {
            ActiveInterpreter.ClearScreen();
        }

        public void ArrowKey(Arrows value)
        {
            ActiveInterpreter.ArrowKey(value);
        }

        public void HomeKey()
        {
            ActiveInterpreter.HomeKey();
        }

        public void EndKey()
        {
            ActiveInterpreter.EndKey();
        }

        public void PushInterpreter(Interpreter newInt)
        {
            InterpreterStack.Add(newInt);
        }

        public void FunctionKey(int fkeyNumber)
        {
            ActiveInterpreter.FunctionKey(fkeyNumber);
        }

        public bool SaveProgram(File file)
        {
            if (SelectedVolume.IsRoomFor(file))
            {
                SelectedVolume.SaveFile(file);
                return true;
            }

            return false;
        }

        public File TryGetFile(string fileName)
        {
            File programOnDisk = SelectedVolume.GetByName(fileName);
            if (programOnDisk != null)
            {
                return programOnDisk.Copy();
            }

            return null;
        }

        public List<File> GetFiles()
        {
            return SelectedVolume.Files;
        }

        public void Exit(Interpreter iToExit)
        {
            foreach (Interpreter i in InterpreterStack)
            {
                if (i == iToExit)
                {
                    InterpreterStack.Remove(i);
                    return;
                }
            }
        }

        public void RunFile(string fileName)
        {
            File file = SelectedVolume.GetByName(fileName);

            if (file != null)
            {
                foreach (String s in file)
                {
                    Execute(s);
                }
            }
        }

        internal bool SwitchToVolume(int volID)
        {
            if (Volumes.Count > volID)
            {
                SelectedVolume = Volumes[volID];
                return true;
            }

            return false;
        }

        internal Volume GetVolumeByName(String targetVolume)
        {
            foreach (Volume volume in Volumes)
            {
                if (volume.Name.ToUpper() == targetVolume.ToUpper())
                {
                    return volume;
                }
            }

            return null;
        }

        internal bool SwitchToVolume(string targetVolume)
        {
            foreach (Volume volume in Volumes)
            {
                if (volume.Name.ToUpper() == targetVolume.ToUpper())
                {
                    SelectedVolume = volume;
                    return true;
                }
            }

            return false;
        }

        public Volume GetVolume(object identifier)
        {
            if (identifier is int)
            {
                return Volumes[(int)identifier];
            }
            else if (identifier is string)
            {
                int idNum;
                if (int.TryParse((string)identifier, out idNum))
                {
                    try
                    {
                        return Volumes[idNum];
                    }
                    catch (ArgumentOutOfRangeException e)
                    {
                        return null;
                    }
                }

                return GetVolumeByName((string)identifier);   
            }

            return null;
        }

        internal void ProcessElectricity(Part part, float time)
        {
            if (Mode == Modes.OFF) return;

            var electricReq = 0.05f * time;
            PoweredState = part.RequestResource("ElectricCharge", electricReq) / electricReq;
        }

        internal void TurnOff()
        {
            Mode = Modes.OFF;
            Queue.Clear();
            LockedCommands.Clear();
            InterpreterStack.Clear();
        }

        internal void TurnOn()
        {
            Boot();
        }
    }*/
}
