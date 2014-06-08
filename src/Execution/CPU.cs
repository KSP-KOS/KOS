using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using kOS.Suffixed;
using kOS.Function;
using kOS.Compilation;

namespace kOS.Execution
{
    public class CPU: IUpdateObserver
    {
        private enum Status
        {
            Running = 1,
            Waiting = 2
        }

        private readonly Stack _stack;
        private readonly Dictionary<string, Variable> _vars;
        private Status _currentStatus;
        private double _currentTime;
        private double _timeWaitUntil;
        private Dictionary<string, FunctionBase> _functions;
        private readonly SharedObjects _shared;
        private readonly List<ProgramContext> _contexts;
        private ProgramContext _currentContext;
        private Dictionary<string, Variable> _savedPointers;
        
        // statistics
        public double TotalCompileTime = 0D;
        private double _totalUpdateTime;
        private double _totalTriggersTime;
        private double _totalExecutionTime;

        public int InstructionPointer
        {
            get { return _currentContext.InstructionPointer; }
            set { _currentContext.InstructionPointer = value; }
        }

        public double SessionTime { get { return _currentTime; } }


        public CPU(SharedObjects shared)
        {
            _shared = shared;
            _shared.Cpu = this;
            _stack = new Stack();
            _vars = new Dictionary<string, Variable>();
            _contexts = new List<ProgramContext>();
            if (_shared.UpdateHandler != null) _shared.UpdateHandler.AddObserver(this);
        }

        private void LoadFunctions()
        {
            _functions = new Dictionary<string, FunctionBase>();

            foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
            {
                var attr = (FunctionAttribute)type.GetCustomAttributes(typeof(FunctionAttribute), true).FirstOrDefault();
                if (attr == null) continue;

                object functionObject = Activator.CreateInstance(type);
                foreach (string functionName in attr.Names)
                {
                    if (functionName != string.Empty)
                    {
                        _functions.Add(functionName, (FunctionBase)functionObject);
                    }
                }
            }
        }

        public void Boot()
        {
            // break all running programs
            _currentContext = null;
            _contexts.Clear();
            PushInterpreterContext();
            _currentStatus = Status.Running;
            _currentTime = 0;
            _timeWaitUntil = 0;
            // clear stack
            _stack.Clear();
            // clear variables
            _vars.Clear();
            // clear interpreter
            if (_shared.Interpreter != null) _shared.Interpreter.Reset();
            // load functions
            LoadFunctions();
            // load bindings
            if (_shared.BindingMgr != null) _shared.BindingMgr.LoadBindings();
            // Booting message
            if (_shared.Screen != null)
            {
                _shared.Screen.ClearScreen();
                string bootMessage = "kOS Operating System\n" +
                                     "KerboScript v" + Core.VersionInfo + "\n \n" +
                                     "Proceed.\n ";
                _shared.Screen.Print(bootMessage);
            }
            
            if (_shared.VolumeMgr == null) { UnityEngine.Debug.Log("kOS: No volume mgr"); }
            else if (_shared.VolumeMgr.CurrentVolume == null) { UnityEngine.Debug.Log("kOS: No current volume"); }
            else if (_shared.ScriptHandler == null) { UnityEngine.Debug.Log("kOS: No script handler"); }
            else if (_shared.VolumeMgr.CurrentVolume.GetByName("boot") != null)
            {
                _shared.ScriptHandler.ClearContext("program");

                var programContext = _shared.Cpu.GetProgramContext();
                programContext.Silent = true;
                var options = new CompilerOptions {LoadProgramsInSameAddressSpace = true};
                List<CodePart> parts = _shared.ScriptHandler.Compile("run boot.", "program", options);
                programContext.AddParts(parts);
            }
        }

        private void PushInterpreterContext()
        {
            var interpreterContext = new ProgramContext(true);
            // initialize the context with an empty program
            interpreterContext.AddParts(new List<CodePart>());
            PushContext(interpreterContext);
        }

        private void PushContext(ProgramContext context)
        {
            UnityEngine.Debug.Log("kOS: Pushing context staring with: " + context.GetCodeFragment(0).FirstOrDefault());
            SaveAndClearPointers();
            _contexts.Add(context);
            _currentContext = _contexts.Last();

            if (_contexts.Count > 1)
            {
                _shared.Interpreter.SetInputLock(true);
            }
        }

        private void PopContext()
        {
            UnityEngine.Debug.Log("kOS: Popping context " + _contexts.Count);
            if (_contexts.Any())
            {
                // remove the last context
                var contextRemove = _contexts.Last();
                _contexts.Remove(contextRemove);
                contextRemove.DisableActiveFlyByWire(_shared.BindingMgr);
                UnityEngine.Debug.Log("kOS: Removed Context " + contextRemove.GetCodeFragment(0).FirstOrDefault());

                if (_contexts.Any())
                {
                    _currentContext = _contexts.Last();
                    _currentContext.EnableActiveFlyByWire(_shared.BindingMgr);
                    RestorePointers();
                    UnityEngine.Debug.Log("kOS: New current context " + _currentContext.GetCodeFragment(0).FirstOrDefault());
                }
                else
                {
                    _currentContext = null;
                }

                if (_contexts.Count == 1)
                {
                    _shared.Interpreter.SetInputLock(false);
                }
            }
        }

        private void PopFirstContext()
        {
            while (_contexts.Count > 1)
            {
                PopContext();
            }
        }

        // only two contexts exist now, one for the interpreter and one for the programs
        public ProgramContext GetInterpreterContext()
        {
            return _contexts[0];
        }

        public ProgramContext GetProgramContext()
        {
            if (_contexts.Count == 1)
            {
                PushContext(new ProgramContext(false));
            }
            return _currentContext;
        }

        private void SaveAndClearPointers()
        {
            _savedPointers = new Dictionary<string, Variable>();
            var pointers = new List<string>(_vars.Keys.Where(v => v.Contains('*')));

            foreach (var pointerName in pointers)
            {
                _savedPointers.Add(pointerName, _vars[pointerName]);
                _vars.Remove(pointerName);
            }
            UnityEngine.Debug.Log(string.Format("kOS: Saving and removing {0} pointers", pointers.Count));
        }

        private void RestorePointers()
        {
            int restoredPointers = 0;
            int deletedPointers = 0;

            foreach (var item in _savedPointers)
            {
                if (_vars.ContainsKey(item.Key))
                {
                    // if the pointer exists it means it was redefined from inside a program
                    // and it's going to be invalid outside of it, so we remove it
                    _vars.Remove(item.Key);
                    deletedPointers++;
                    // also remove the corresponding trigger if exists
                    if (item.Value.Value is int)
                        RemoveTrigger((int)item.Value.Value);
                }
                else
                {
                    _vars.Add(item.Key, item.Value);
                    restoredPointers++;
                }
            }

            UnityEngine.Debug.Log(string.Format("kOS: Deleting {0} pointers and restoring {1} pointers", deletedPointers, restoredPointers));
        }

        public void RunProgram(List<Opcode> program)
        {
            RunProgram(program, false);
        }

        public void RunProgram(List<Opcode> program, bool silent)
        {
            if (!program.Any()) return;

            var newContext = new ProgramContext(false, program) {Silent = silent};
            PushContext(newContext);
        }

        public void BreakExecution(bool manual)
        {
            UnityEngine.Debug.Log(string.Format("kOS: Breaking Execution {0} Contexts: {1}", manual ? "Manually" : "Automaticly", _contexts.Count));
            if (_contexts.Count > 1)
            {
                EndWait();

                if (manual)
                {
                    PopFirstContext();
                    _shared.Screen.Print("Program aborted.");
                    _shared.BindingMgr.UnBindAll();
                    PrintStatistics();
                }
                else
                {
                    bool silent = _currentContext.Silent;
                    PopContext();
                    if (_contexts.Count == 1 && !silent)
                    {
                        _shared.Screen.Print("Program ended.");
                        _shared.BindingMgr.UnBindAll();
                        PrintStatistics();
                    }
                }
            }
            else
            {
                _currentContext.Triggers.Clear();   // remove all the active triggers
                SkipCurrentInstructionId();
            }
        }

        public void PushStack(object item)
        {
            _stack.Push(item);
        }

        public object PopStack()
        {
            return _stack.Pop();
        }

        public void MoveStackPointer(int delta)
        {
            _stack.MoveStackPointer(delta);
        }

        private Variable GetOrCreateVariable(string identifier)
        {
            Variable variable;

            if (_vars.ContainsKey(identifier))
            {
                variable = GetVariable(identifier);
            }
            else
            {
                variable = new Variable {Name = identifier};
                AddVariable(variable, identifier);
            }
            return variable;
        }

        private Variable GetVariable(string identifier)
        {
            identifier = identifier.ToLower();
            if (_vars.ContainsKey(identifier))
            {
                return _vars[identifier];
            }
            throw new Exception(string.Format("Variable {0} is not defined", identifier.TrimStart('$')));
        }

        public void AddVariable(Variable variable, string identifier)
        {
            identifier = identifier.ToLower();
            
            if (!identifier.StartsWith("$"))
            {
                identifier = "$" + identifier;
            }

            if (_vars.ContainsKey(identifier))
            {
                _vars.Remove(identifier);
            }

            _vars.Add(identifier, variable);
        }

        public bool VariableIsRemovable(Variable variable)
        {
            return !(variable is Binding.BoundVariable);
        }

        public void RemoveVariable(string identifier)
        {
            identifier = identifier.ToLower();
            
            if (_vars.ContainsKey(identifier) &&
                VariableIsRemovable(_vars[identifier]))
            {
                // Tell Variable to orphan its old value now.  Faster than relying 
                // on waiting several seconds for GC to eventually call ~Variable()
                _vars[identifier].Value = null;
                
                _vars.Remove(identifier);
            }
        }

        public void RemoveAllVariables()
        {
            var removals = new List<string>();
            
            foreach (KeyValuePair<string, Variable> kvp in _vars)
            {
                if (VariableIsRemovable(kvp.Value))
                {
                    removals.Add(kvp.Key);
                }
            }

            foreach (string identifier in removals)
            {
                // Tell Variable to orphan its old value now.  Faster than relying 
                // on waiting several seconds for GC to eventually call ~Variable()
                _vars[identifier].Value = null;

                _vars.Remove(identifier);
            }
        }

        public object GetValue(object testValue)
        {
            // $cos     cos named variable
            // cos()    cos trigonometric function
            // cos      string literal "cos"

            if (testValue is string &&
                ((string)testValue).StartsWith("$"))
            {
                // value is a variable
                var identifier = (string)testValue;
                Variable variable = GetVariable(identifier);
                return variable.Value;
            }
            return testValue;
        }

        public void SetValue(string identifier, object value)
        {
            Variable variable = GetOrCreateVariable(identifier);
            variable.Value = value;
        }

        public object PopValue()
        {
            return GetValue(PopStack());
        }

        public void AddTrigger(int triggerFunctionPointer)
        {
            if (!_currentContext.Triggers.Contains(triggerFunctionPointer))
            {
                _currentContext.Triggers.Add(triggerFunctionPointer);
            }
        }

        public void RemoveTrigger(int triggerFunctionPointer)
        {
            if (_currentContext.Triggers.Contains(triggerFunctionPointer))
            {
                _currentContext.Triggers.Remove(triggerFunctionPointer);
            }
        }

        public void StartWait(double waitTime)
        {
            if (waitTime > 0)
            {
                _timeWaitUntil = _currentTime + waitTime;
            }
            _currentStatus = Status.Waiting;
        }

        public void EndWait()
        {
            _timeWaitUntil = 0;
            _currentStatus = Status.Running;
        }

        public void Update(double deltaTime)
        {
            bool showStatistics = Config.Instance.ShowStatistics;
            Stopwatch updateWatch = null;
            Stopwatch triggerWatch = null;
            Stopwatch executionWatch = null;

            if (showStatistics) updateWatch = Stopwatch.StartNew();

            _currentTime = _shared.UpdateHandler.CurrentTime;

            try
            {
                PreUpdateBindings();

                if (_currentContext != null && _currentContext.Program != null)
                {
                    if (showStatistics) triggerWatch = Stopwatch.StartNew();
                    ProcessTriggers();
                    if (showStatistics)
                    {
                        triggerWatch.Stop();
                        _totalTriggersTime += triggerWatch.ElapsedMilliseconds;
                    }

                    ProcessWait();

                    if (_currentStatus == Status.Running)
                    {
                        if (showStatistics) executionWatch = Stopwatch.StartNew();
                        ContinueExecution();
                        if (showStatistics)
                        {
                            executionWatch.Stop();
                            _totalExecutionTime += executionWatch.ElapsedMilliseconds;
                        }
                    }
                }

                PostUpdateBindings();
            }
            catch (Exception e)
            {
                if (_shared.Logger != null)
                {
                    _shared.Logger.Log(e);
                    UnityEngine.Debug.Log(_stack.Dump(15));
                }

                if (_contexts.Count == 1)
                {
                    // interpreter context
                    SkipCurrentInstructionId();
                }
                else
                {
                    // break execution of all programs and pop interpreter context
                    PopFirstContext();
                }
            }

            if (showStatistics)
            {
                updateWatch.Stop();
                _totalUpdateTime += updateWatch.ElapsedMilliseconds;
            }
        }

        private void PreUpdateBindings()
        {
            if (_shared.BindingMgr != null)
            {
                _shared.BindingMgr.PreUpdate();
            }
        }

        private void PostUpdateBindings()
        {
            if (_shared.BindingMgr != null)
            {
                _shared.BindingMgr.PostUpdate();
            }
        }

        private void ProcessWait()
        {
            if (_currentStatus == Status.Waiting && _timeWaitUntil > 0)
            {
                if (_currentTime >= _timeWaitUntil)
                {
                    EndWait();
                }
            }
        }

        private void ProcessTriggers()
        {
            if (_currentContext.Triggers.Count > 0)
            {
                int currentInstructionPointer = _currentContext.InstructionPointer;
                var triggerList = new List<int>(_currentContext.Triggers);

                foreach (int triggerPointer in triggerList)
                {
                    try
                    {
                        _currentContext.InstructionPointer = triggerPointer;

                        bool executeNext = true;
                        while (executeNext)
                        {
                            executeNext = ExecuteInstruction(_currentContext);
                        }
                    }
                    catch (Exception e)
                    {
                        RemoveTrigger(triggerPointer);
                        _shared.Logger.Log(e);
                    }
                }

                _currentContext.InstructionPointer = currentInstructionPointer;
            }
        }

        private void ContinueExecution()
        {
            int instructionCounter = 0;
            bool executeNext = true;
            int instructionsPerUpdate = Config.Instance.InstructionsPerUpdate;
            
            while (_currentStatus == Status.Running && 
                   instructionCounter < instructionsPerUpdate &&
                   executeNext &&
                   _currentContext != null)
            {
                executeNext = ExecuteInstruction(_currentContext);
                instructionCounter++;
            }
        }

        private bool ExecuteInstruction(ProgramContext context)
        {
            Opcode opcode = context.Program[context.InstructionPointer];
            if (!(opcode is OpcodeEOF || opcode is OpcodeEOP))
            {
                opcode.Execute(this);
                context.InstructionPointer += opcode.DeltaInstructionPointer;
                return true;
            }
            if (opcode is OpcodeEOP)
            {
                BreakExecution(false);
                UnityEngine.Debug.LogWarning("kOS: Execution Broken");
            }
            return false;
        }

        private void SkipCurrentInstructionId()
        {
            if (_currentContext.InstructionPointer < (_currentContext.Program.Count - 1))
            {
                int currentInstructionId = _currentContext.Program[_currentContext.InstructionPointer].InstructionId;

                while (_currentContext.InstructionPointer < _currentContext.Program.Count &&
                       _currentContext.Program[_currentContext.InstructionPointer].InstructionId == currentInstructionId)
                {
                    _currentContext.InstructionPointer++;
                }
            }
        }

        public void CallBuiltinFunction(string functionName)
        {
            if (_functions.ContainsKey(functionName))
            {
                FunctionBase function = _functions[functionName];
                function.Execute(_shared);
            }
            else
            {
                throw new Exception("Call to non-existent function " + functionName);
            }
        }

        public void ToggleFlyByWire(string paramName, bool enabled)
        {
            if (_shared.BindingMgr != null)
            {
                _shared.BindingMgr.ToggleFlyByWire(paramName, enabled);
                _currentContext.ToggleFlyByWire(paramName, enabled);
            }
        }

        public List<string> GetCodeFragment(int contextLines)
        {
            return _currentContext.GetCodeFragment(contextLines);
        }

        public void PrintStatistics()
        {
            if (!Config.Instance.ShowStatistics) return;

            _shared.Screen.Print(string.Format("Total compile time: {0:F3}ms", TotalCompileTime));
            _shared.Screen.Print(string.Format("Total update time: {0:F3}ms", _totalUpdateTime));
            _shared.Screen.Print(string.Format("Total triggers time: {0:F3}ms", _totalTriggersTime));
            _shared.Screen.Print(string.Format("Total execution time: {0:F3}ms", _totalExecutionTime));
            _shared.Screen.Print(" ");

            TotalCompileTime = 0D;
            _totalUpdateTime = 0D;
            _totalTriggersTime = 0D;
            _totalExecutionTime = 0D;
        }

        public void OnSave(ConfigNode node)
        {
            try
            {
                var contextNode = new ConfigNode("context");

                // Save variables
                if (_vars.Count > 0)
                {
                    var varNode = new ConfigNode("variables");

                    foreach (var kvp in _vars)
                    {
                        if (!(kvp.Value is Binding.BoundVariable) &&
                            (kvp.Value.Name.IndexOfAny(new[] { '*', '-' }) == -1))  // variables that have this characters are internal and shouldn't be persisted
                        {
                            varNode.AddValue(kvp.Key.TrimStart('$'), Persistence.ProgramFile.EncodeLine(kvp.Value.Value.ToString()));
                        }
                    }

                    contextNode.AddNode(varNode);
                }

                node.AddNode(contextNode);
            }
            catch (Exception e)
            {
                if (_shared.Logger != null) _shared.Logger.Log(e);
            }
        }

        public void OnLoad(ConfigNode node)
        {
            try
            {
                var scriptBuilder = new StringBuilder();

                foreach (ConfigNode contextNode in node.GetNodes("context"))
                {
                    foreach (ConfigNode varNode in contextNode.GetNodes("variables"))
                    {
                        foreach (ConfigNode.Value value in varNode.values)
                        {
                            string varValue = Persistence.ProgramFile.DecodeLine(value.value);
                            scriptBuilder.AppendLine(string.Format("set {0} to {1}.", value.name, varValue));
                        }
                    }
                }

                if (_shared.ScriptHandler != null && scriptBuilder.Length > 0)
                {
                    var programBuilder = new ProgramBuilder();
                    programBuilder.AddRange(_shared.ScriptHandler.Compile(scriptBuilder.ToString()));
                    List<Opcode> program = programBuilder.BuildProgram();
                    RunProgram(program, true);
                }
            }
            catch (Exception e)
            {
                if (_shared.Logger != null) _shared.Logger.Log(e);
            }
        }
    }
}
