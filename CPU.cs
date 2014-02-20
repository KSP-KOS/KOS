using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;

namespace kOS
{
    public class CPU
    {
        private enum Status
        {
            Running = 1,
            Waiting = 2
        }

        private Stack _stack;
        private Dictionary<string, Variable> _vars;
        private Status _currentStatus;
        private double _currentTime;
        private double _timeWaitUntil;
        private Dictionary<string, Function> _functions;
        private SharedObjects _shared;
        private List<ProgramContext> _contexts;
        private ProgramContext _currentContext;
        
        // statistics
        private double _totalUpdateTime = 0D;
        private double _totalTriggersTime = 0D;
        private double _totalExecutionTime = 0D;

        public int InstructionPointer
        {
            get { return _currentContext.InstructionPointer; }
            set { _currentContext.InstructionPointer = value; }
        }


        public CPU(SharedObjects shared)
        {
            _shared = shared;
            _shared.Cpu = this;
            _stack = new Stack();
            _vars = new Dictionary<string, Variable>();
            _contexts = new List<ProgramContext>();
            Boot();
        }

        private void LoadFunctions()
        {
            _functions = new Dictionary<string, Function>();

            foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
            {
                FunctionAttribute attr = (FunctionAttribute)type.GetCustomAttributes(typeof(FunctionAttribute), true).FirstOrDefault();
                if (attr != null)
                {
                    if (attr.functionName != string.Empty)
                    {
                        _functions.Add(attr.functionName, (Function)Activator.CreateInstance(type));
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
                string bootMessage = "kRISC Operating System\n" +
                                     "KerboScript v" + Core.VersionInfo.ToString() + "\n \n" +
                                     "Proceed.\n ";
                _shared.Screen.Print(bootMessage);
            }
        }

        private void PushInterpreterContext()
        {
            ProgramBuilder builder = new ProgramBuilder();
            List<Opcode> emptyProgram = builder.BuildProgram(true);
            PushContext(new ProgramContext(emptyProgram));
        }

        private void PushContext(ProgramContext context)
        {
            _contexts.Add(context);
            _currentContext = _contexts[_contexts.Count - 1];

            if (_contexts.Count > 1)
            {
                _shared.Interpreter.SetInputLock(true);
            }
        }

        private void PopContext()
        {
            if (_contexts.Count > 0)
            {
                // remove the last context
                ProgramContext contextRemove = _contexts[_contexts.Count - 1];
                _contexts.Remove(contextRemove);
                contextRemove.DisableActiveFlyByWire(_shared.BindingMgr);

                if (_contexts.Count > 0)
                {
                    _currentContext = _contexts[_contexts.Count - 1];
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

        public void RunProgram(List<Opcode> program)
        {
            RunProgram(program, false);
        }

        public void RunProgram(List<Opcode> program, bool silent)
        {
            if (program.Count > 0)
            {
                ProgramContext newContext = new ProgramContext(program);
                newContext.Silent = silent;
                PushContext(newContext);

                // only reset the statistics for the first executed program
                // all subprogram calls sum in the parent statistics
                if (_contexts.Count == 2)
                {
                    _totalUpdateTime = 0D;
                    _totalTriggersTime = 0D;
                    _totalExecutionTime = 0D;
                }
            }
        }

        public void UpdateProgram(List<Opcode> program)
        {
            if (program.Count > 0)
            {
                if (_currentContext != null && _currentContext.Program != null)
                {
                    _currentContext.UpdateProgram(program);
                }
                else
                {
                    RunProgram(program);
                }
            }
        }

        public void BreakExecution(bool manual)
        {
            if (_contexts.Count > 1)
            {
                EndWait();

                if (manual)
                {
                    PopFirstContext();
                    _shared.Screen.Print("Program aborted.");
                    PrintStatistics();
                }
                else
	            {
                    bool silent = _currentContext.Silent;
                    PopContext();
                    if (_contexts.Count == 1 && !silent)
                    {
                        _shared.Screen.Print("Program ended.");
                        PrintStatistics();
                    }
	            }
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
                variable = new Variable();
                variable.Name = identifier;
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
            else
            {
                throw new Exception(string.Format("Variable {0} is not defined", identifier.TrimStart('$')));
            }
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

        public object GetValue(object testValue)
        {
            // $cos     cos named variable
            // cos()    cos trigonometric function
            // cos      string literal "cos"

            if (testValue is string &&
                ((string)testValue).StartsWith("$"))
            {
                // value is a variable
                string identifier = (string)testValue;
                string[] suffixes = identifier.Split(':');

                Variable variable = GetVariable(suffixes[0]);
                object value = variable.Value;

                if (value is SpecialValue && suffixes.Length > 1)
                {
                    for (int suffixIndex = 1; suffixIndex < suffixes.Length; suffixIndex++)
                    {
                        string suffixName = suffixes[suffixIndex].ToUpper();
                        value = ((SpecialValue)value).GetSuffix(suffixName);
                        if (value == null)
                        {
                            throw new Exception(string.Format("Suffix {0} not found on object {1}", suffixName, variable.Name));
                        }
                    }
                }
                
                return value;
            }
            else
            {
                return testValue;
            }
        }

        public void SetValue(string identifier, object value)
        {
            string[] suffixes = identifier.Split(':');
            Variable variable = GetOrCreateVariable(suffixes[0]);

            if (suffixes.Length > 1 && variable.Value is SpecialValue)
            {
                SpecialValue specialValue = (SpecialValue)variable.Value;
                string suffixName;

                if (suffixes.Length > 2)
                {
                    for (int suffixIndex = 1; suffixIndex < (suffixes.Length-1); suffixIndex++)
                    {
                        suffixName = suffixes[suffixIndex].ToUpper();
                        specialValue = (SpecialValue)specialValue.GetSuffix(suffixName);
                        if (specialValue == null)
                        {
                            throw new Exception(string.Format("Suffix {0} not found on object {1}", suffixName, variable.Name));
                        }
                    }
                }

                suffixName = suffixes[suffixes.Length - 1].ToUpper();
                if (!specialValue.SetSuffix(suffixName, value))
                {
                    throw new Exception(string.Format("Suffix {0} not found on object {1}", suffixName, variable.Name));
                }
            }
            else
            {
                variable.Value = value;
            }
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
            //else
            //{
            //    if (_shared.Logger != null)
            //    {
            //        _shared.Logger.Log(string.Format("Can't remove trigger: {0}    IP: {1}", triggerFunctionPointer, _currentContext.InstructionPointer));
            //    }
            //}
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
            bool showStatistics = Config.GetInstance().ShowStatistics;
            Stopwatch updateWatch = null;
            Stopwatch triggerWatch = null;
            Stopwatch executionWatch = null;

            if (showStatistics) updateWatch = Stopwatch.StartNew();
            
            _currentTime += deltaTime;

            try
            {
                PreUpdateBindings(deltaTime);

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

                if (_shared.Logger != null)
                {
                    _shared.Logger.Log(e.Message);
                }
            }

            if (showStatistics)
            {
                updateWatch.Stop();
                _totalUpdateTime += updateWatch.ElapsedMilliseconds;
            }
        }

        private void PreUpdateBindings(double deltaTime)
        {
            if (_shared.BindingMgr != null)
            {
                _shared.BindingMgr.PreUpdate(deltaTime);
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
                List<int> triggerList = new List<int>(_currentContext.Triggers);

                foreach (int triggerPointer in triggerList)
                {
                    _currentContext.InstructionPointer = triggerPointer;

                    bool executeNext = true;
                    while (executeNext)
                    {
                        executeNext = ExecuteInstruction(_currentContext);
                    }
                }

                _currentContext.InstructionPointer = currentInstructionPointer;
            }
        }

        private void ContinueExecution()
        {
            int instructionCounter = 0;
            bool executeNext = true;
            int instructionsPerUpdate = Config.GetInstance().InstructionsPerUpdate;
            
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
            else
            {
                if (opcode is OpcodeEOP)
                {
                    BreakExecution(false);
                }
                return false;
            }
        }

        private void SkipCurrentInstructionId()
        {
            int currentInstructionId = _currentContext.Program[_currentContext.InstructionPointer].InstructionId;

            while (_currentContext.InstructionPointer < _currentContext.Program.Count &&
                   _currentContext.Program[_currentContext.InstructionPointer].InstructionId == currentInstructionId)
            {
                _currentContext.InstructionPointer++;
            }
        }

        public void CallBuiltinFunction(string functionName)
        {
            if (_functions.ContainsKey(functionName))
            {
                Function function = _functions[functionName];
                function.Execute(_shared);
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

        public void PrintStatistics()
        {
            if (Config.GetInstance().ShowStatistics)
            {
                _shared.Screen.Print(string.Format("Total update time: {0:F3}ms", _totalUpdateTime));
                _shared.Screen.Print(string.Format("Total triggers time: {0:F3}ms", _totalTriggersTime));
                _shared.Screen.Print(string.Format("Total execution time: {0:F3}ms", _totalExecutionTime));
                _shared.Screen.Print(" ");
            }
        }

        public void OnSave(ConfigNode node)
        {
            ConfigNode contextNode = new ConfigNode("context");

            // Save variables
            if (_vars.Count > 0)
            {
                ConfigNode varNode = new ConfigNode("variables");

                foreach (var kvp in _vars)
                {
                    if (!(kvp.Value is BoundVariable))
                    {
                        varNode.AddValue(kvp.Key.TrimStart('$'), ProgramFile.EncodeLine(kvp.Value.Value.ToString()));
                    }
                }

                contextNode.AddNode(varNode);
            }

            node.AddNode(contextNode);
        }

        public void OnLoad(ConfigNode node)
        {
            StringBuilder scriptBuilder = new StringBuilder();
            
            foreach (ConfigNode contextNode in node.GetNodes("context"))
            {
                foreach (ConfigNode varNode in contextNode.GetNodes("variables"))
                {
                    foreach (ConfigNode.Value value in varNode.values)
                    {
                        string varValue = ProgramFile.DecodeLine(value.value);
                        scriptBuilder.AppendLine(string.Format("set {0} to {1}.", value.name, varValue));
                    }
                }
            }

            if (_shared.ScriptHandler != null && scriptBuilder.Length > 0)
            {
                ProgramBuilder programBuilder = new ProgramBuilder();
                programBuilder.AddRange(_shared.ScriptHandler.Compile(scriptBuilder.ToString()));
                List<Opcode> program = programBuilder.BuildProgram(false);
                RunProgram(program, true);
            }
        }
    }
}
