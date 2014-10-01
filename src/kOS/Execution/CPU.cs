using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using kOS.Safe;
using kOS.Safe.Compilation;
using kOS.Safe.Execution;
using kOS.Suffixed;
using kOS.Function;

namespace kOS.Execution
{
    public class CPU: IUpdateObserver , ICpu
    {
        private enum Status
        {
            Running = 1,
            Waiting = 2
        }

        private readonly Stack stack;
        private readonly Dictionary<string, Variable> variables;
        private Status currentStatus;
        private double currentTime;
        private double timeWaitUntil;
        private Dictionary<string, FunctionBase> functions;
        private readonly SharedObjects shared;
        private readonly List<ProgramContext> contexts;
        private ProgramContext currentContext;
        private Dictionary<string, Variable> savedPointers;
        private int instructionsSoFarInUpdate;
        private int instructionsPerUpdate;
        
        // statistics
        private double totalCompileTime;
        private double totalUpdateTime;
        private double totalTriggersTime;
        private double totalExecutionTime;
        private int maxMainlineInstructionsSoFar;
        private int maxTriggerInstructionsSoFar;

        public int InstructionPointer
        {
            get { return currentContext.InstructionPointer; }
            set { currentContext.InstructionPointer = value; }
        }

        public double SessionTime { get { return currentTime; } }


        public CPU(SharedObjects shared)
        {
            this.shared = shared;
            this.shared.Cpu = this;
            stack = new Stack();
            variables = new Dictionary<string, Variable>();
            contexts = new List<ProgramContext>();
            if (this.shared.UpdateHandler != null) this.shared.UpdateHandler.AddObserver(this);
        }

        private void LoadFunctions()
        {
            functions = new Dictionary<string, FunctionBase>();

            foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
            {
                var attr = (FunctionAttribute)type.GetCustomAttributes(typeof(FunctionAttribute), true).FirstOrDefault();
                if (attr == null) continue;

                object functionObject = Activator.CreateInstance(type);
                foreach (string functionName in attr.Names)
                {
                    if (functionName != string.Empty)
                    {
                        functions.Add(functionName, (FunctionBase)functionObject);
                    }
                }
            }
        }

        public void Boot()
        {
            // break all running programs
            currentContext = null;
            contexts.Clear();
            PushInterpreterContext();
            currentStatus = Status.Running;
            currentTime = 0;
            timeWaitUntil = 0;
            // clear stack
            stack.Clear();
            // clear variables
            variables.Clear();
            // clear interpreter
            if (shared.Interpreter != null) shared.Interpreter.Reset();
            // load functions
            LoadFunctions();
            // load bindings
            if (shared.BindingMgr != null) shared.BindingMgr.LoadBindings();
            // Booting message
            if (shared.Screen != null)
            {
                shared.Screen.ClearScreen();
                string bootMessage = string.Format("kOS Operating System\n" + "KerboScript v{0}\n \n" + "Proceed.\n", Core.VersionInfo);
                List<string>nags = Utilities.Utils.GetPendingNags();
                if (nags.Count > 0)
                {
                    bootMessage +=
                        "##################################################\n" +
                        "#               NAG MESSAGES                     #\n" +
                        "##################################################\n" +
                        "# Further details about these important messages #\n" +
                        "# can be found in the KSP error log.  If you see #\n" +
                        "# this, then read the error log.  It's important.#\n" +
                        "--------------------------------------------------\n";

                    foreach (string msg in nags)
                        bootMessage += msg + "\n";
                    bootMessage +=
                        "##################################################\n";
                }
                shared.Screen.Print(bootMessage);
            }
            
            if (shared.VolumeMgr == null) { UnityEngine.Debug.Log("kOS: No volume mgr"); }
            else if (shared.VolumeMgr.CurrentVolume == null) { UnityEngine.Debug.Log("kOS: No current volume"); }
            else if (shared.ScriptHandler == null) { UnityEngine.Debug.Log("kOS: No script handler"); }
            else if (shared.VolumeMgr.CurrentVolume.GetByName("boot") != null)
            {
                shared.ScriptHandler.ClearContext("program");

                var programContext = shared.Cpu.GetProgramContext();
                programContext.Silent = true;
                var options = new CompilerOptions {LoadProgramsInSameAddressSpace = true};
                string filePath = shared.VolumeMgr.GetVolumeRawIdentifier(shared.VolumeMgr.CurrentVolume) + "/" + "boot" ;
                List<CodePart> parts = shared.ScriptHandler.Compile(filePath, 1, "run boot.", "program", options);
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
            contexts.Add(context);
            currentContext = contexts.Last();

            if (contexts.Count > 1)
            {
                shared.Interpreter.SetInputLock(true);
            }
        }

        private void PopContext()
        {
            UnityEngine.Debug.Log("kOS: Popping context " + contexts.Count);
            if (contexts.Any())
            {
                // remove the last context
                var contextRemove = contexts.Last();
                contexts.Remove(contextRemove);
                contextRemove.DisableActiveFlyByWire(shared.BindingMgr);
                UnityEngine.Debug.Log("kOS: Removed Context " + contextRemove.GetCodeFragment(0).FirstOrDefault());

                if (contexts.Any())
                {
                    currentContext = contexts.Last();
                    currentContext.EnableActiveFlyByWire(shared.BindingMgr);
                    RestorePointers();
                    UnityEngine.Debug.Log("kOS: New current context " + currentContext.GetCodeFragment(0).FirstOrDefault());
                }
                else
                {
                    currentContext = null;
                }

                if (contexts.Count == 1)
                {
                    shared.Interpreter.SetInputLock(false);
                }
            }
        }

        private void PopFirstContext()
        {
            while (contexts.Count > 1)
            {
                PopContext();
            }
        }

        // only two contexts exist now, one for the interpreter and one for the programs
        public ProgramContext GetInterpreterContext()
        {
            return contexts[0];
        }

        public ProgramContext GetProgramContext()
        {
            if (contexts.Count == 1)
            {
                PushContext(new ProgramContext(false));
            }
            return currentContext;
        }
        
        public Opcode GetCurrentOpcode()
        {
            return currentContext.Program[currentContext.InstructionPointer];
        }
        
        public Opcode GetOpcodeAt(int instructionPtr)
        {
            if (instructionPtr < 0 || instructionPtr >= currentContext.Program.Count)
            {
                return new OpcodeBogus();
            }
            return currentContext.Program[instructionPtr];
        }

        private void SaveAndClearPointers()
        {
            savedPointers = new Dictionary<string, Variable>();
            var pointers = new List<string>(variables.Keys.Where(v => v.Contains('*')));

            foreach (var pointerName in pointers)
            {
                savedPointers.Add(pointerName, variables[pointerName]);
                variables.Remove(pointerName);
            }
            UnityEngine.Debug.Log(string.Format("kOS: Saving and removing {0} pointers", pointers.Count));
        }

        private void RestorePointers()
        {
            int restoredPointers = 0;
            int deletedPointers = 0;

            foreach (var item in savedPointers)
            {
                if (variables.ContainsKey(item.Key))
                {
                    // if the pointer exists it means it was redefined from inside a program
                    // and it's going to be invalid outside of it, so we remove it
                    variables.Remove(item.Key);
                    deletedPointers++;
                    // also remove the corresponding trigger if exists
                    if (item.Value.Value is int)
                        RemoveTrigger((int)item.Value.Value);
                }
                else
                {
                    variables.Add(item.Key, item.Value);
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
            UnityEngine.Debug.Log(string.Format("kOS: Breaking Execution {0} Contexts: {1}", manual ? "Manually" : "Automaticly", contexts.Count));
            if (contexts.Count > 1)
            {
                EndWait();

                if (manual)
                {
                    PopFirstContext();
                    shared.Screen.Print("Program aborted.");
                    shared.BindingMgr.UnBindAll();
                    PrintStatistics();
                }
                else
                {
                    bool silent = currentContext.Silent;
                    PopContext();
                    if (contexts.Count == 1 && !silent)
                    {
                        shared.Screen.Print("Program ended.");
                        shared.BindingMgr.UnBindAll();
                        PrintStatistics();
                    }
                }
            }
            else
            {
                currentContext.Triggers.Clear();   // remove all the active triggers
                SkipCurrentInstructionId();
            }
        }

        public void PushStack(object item)
        {
            stack.Push(item);
        }

        public object PopStack()
        {
            return stack.Pop();
        }

        public void MoveStackPointer(int delta)
        {
            stack.MoveStackPointer(delta);
        }

        /// <summary>
        /// Return the subroutine call trace of how the code got to where it is right now.
        /// </summary>
        /// <returns>The first item in the list is the current instruction pointer.
        /// The rest of the items in the list after that are the instruction pointers of the Opcodecall instructions
        /// that got us to here.</returns>
        public List<int> GetCallTrace()
        {
            List<int> trace = stack.GetCallTrace();
            trace.Insert(0, currentContext.InstructionPointer); // prepend current IP
            return trace;
        }

        private Variable GetOrCreateVariable(string identifier)
        {
            Variable variable;

            if (variables.ContainsKey(identifier))
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
            if (variables.ContainsKey(identifier))
            {
                return variables[identifier];
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

            if (variables.ContainsKey(identifier))
            {
                variables.Remove(identifier);
            }

            variables.Add(identifier, variable);
        }

        public bool VariableIsRemovable(Variable variable)
        {
            return !(variable is Binding.BoundVariable);
        }

        public void RemoveVariable(string identifier)
        {
            identifier = identifier.ToLower();
            
            if (variables.ContainsKey(identifier) &&
                VariableIsRemovable(variables[identifier]))
            {
                // Tell Variable to orphan its old value now.  Faster than relying 
                // on waiting several seconds for GC to eventually call ~Variable()
                variables[identifier].Value = null;
                
                variables.Remove(identifier);
            }
        }

        public void RemoveAllVariables()
        {
            var removals = variables.
                Where(v => VariableIsRemovable(v.Value)).
                Select(kvp => kvp.Key).ToList();

            foreach (string identifier in removals)
            {
                // Tell Variable to orphan its old value now.  Faster than relying 
                // on waiting several seconds for GC to eventually call ~Variable()
                variables[identifier].Value = null;

                variables.Remove(identifier);
            }
        }

        public object GetValue(object testValue)
        {
            // $cos     cos named variable
            // cos()    cos trigonometric function
            // cos      string literal "cos"

            // If it's a variable, meaning it starts with "$" but
            // does NOT have a value like $<.....>, which are special
            // flags used internally:
            if (testValue is string &&
                ((string)testValue).Length > 1 &&
                ((string)testValue)[0] == '$' &&
                ((string)testValue)[1] != '<' )
            {
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
        
        public object PeekValue(int digDepth)
        {
            return GetValue(stack.Peek(digDepth));
        }
        
        public int GetStackSize()
        {
            return stack.GetLogicalSize();
        }

        public void AddTrigger(int triggerFunctionPointer)
        {
            if (!currentContext.Triggers.Contains(triggerFunctionPointer))
            {
                currentContext.Triggers.Add(triggerFunctionPointer);
            }
        }

        public void RemoveTrigger(int triggerFunctionPointer)
        {
            if (currentContext.Triggers.Contains(triggerFunctionPointer))
            {
                currentContext.Triggers.Remove(triggerFunctionPointer);
            }
        }

        public void StartWait(double waitTime)
        {
            if (waitTime > 0)
            {
                timeWaitUntil = currentTime + waitTime;
            }
            currentStatus = Status.Waiting;
        }

        public void EndWait()
        {
            timeWaitUntil = 0;
            currentStatus = Status.Running;
        }

        public void Update(double deltaTime)
        {
            bool showStatistics = Config.Instance.ShowStatistics;
            Stopwatch updateWatch = null;
            Stopwatch triggerWatch = null;
            Stopwatch executionWatch = null;
            
            // If the script changes config value, it doesn't take effect until next update:
            instructionsPerUpdate = Config.Instance.InstructionsPerUpdate;
            instructionsSoFarInUpdate = 0;
            int numTriggerInstructions = 0;
            int numMainlineInstructions = 0;

            if (showStatistics) updateWatch = Stopwatch.StartNew();

            currentTime = shared.UpdateHandler.CurrentTime;

            try
            {
                PreUpdateBindings();

                if (currentContext != null && currentContext.Program != null)
                {
                    if (showStatistics) triggerWatch = Stopwatch.StartNew();
                    ProcessTriggers();
                    numTriggerInstructions = instructionsSoFarInUpdate;
                    if (showStatistics)
                    {
                        triggerWatch.Stop();
                        totalTriggersTime += triggerWatch.ElapsedMilliseconds;
                    }

                    ProcessWait();

                    if (currentStatus == Status.Running)
                    {
                        if (showStatistics) executionWatch = Stopwatch.StartNew();
                        ContinueExecution();
                        numMainlineInstructions = instructionsSoFarInUpdate - numTriggerInstructions;
                        if (showStatistics)
                        {
                            executionWatch.Stop();
                            totalExecutionTime += executionWatch.ElapsedMilliseconds;
                        }
                    }
                }

                PostUpdateBindings();
            }
            catch (Exception e)
            {
                if (shared.Logger != null)
                {
                    shared.Logger.Log(e);
                    UnityEngine.Debug.Log(stack.Dump(15));
                }

                if (contexts.Count == 1)
                {
                    // interpreter context
                    SkipCurrentInstructionId();
                }
                else
                {
                    // break execution of all programs and pop interpreter context
                    PopFirstContext();
                    stack.Clear(); // If breaking all exection, get rid of the cruft here too.
                }
            }

            if (showStatistics)
            {
                updateWatch.Stop();
                totalUpdateTime += updateWatch.ElapsedMilliseconds;
                if (maxTriggerInstructionsSoFar < numTriggerInstructions)
                    maxTriggerInstructionsSoFar = numTriggerInstructions;
                if (maxMainlineInstructionsSoFar < numMainlineInstructions)
                    maxMainlineInstructionsSoFar = numMainlineInstructions;
            }
        }

        private void PreUpdateBindings()
        {
            if (shared.BindingMgr != null)
            {
                shared.BindingMgr.PreUpdate();
            }
        }

        private void PostUpdateBindings()
        {
            if (shared.BindingMgr != null)
            {
                shared.BindingMgr.PostUpdate();
            }
        }

        private void ProcessWait()
        {
            if (currentStatus == Status.Waiting && timeWaitUntil > 0)
            {
                if (currentTime >= timeWaitUntil)
                {
                    EndWait();
                }
            }
        }

        private void ProcessTriggers()
        {
            if (currentContext.Triggers.Count <= 0) return;

            int currentInstructionPointer = currentContext.InstructionPointer;
            var triggerList = new List<int>(currentContext.Triggers);

            foreach (int triggerPointer in triggerList)
            {
                try
                {
                    currentContext.InstructionPointer = triggerPointer;

                    bool executeNext = true;
                    while (executeNext && instructionsSoFarInUpdate < instructionsPerUpdate)
                    {
                        executeNext = ExecuteInstruction(currentContext);
                        instructionsSoFarInUpdate++;
                    }
                }
                catch (Exception e)
                {
                    RemoveTrigger(triggerPointer);
                    shared.Logger.Log(e);
                }
                if (instructionsSoFarInUpdate >= instructionsPerUpdate)
                {
                    // This is a verbose error message, but it's important, as without knowing
                    // the internals, a user has no idea why it's happening.  The verbose
                    // error message helps direct the user to areas of the documentation
                    // where longer explanations can be found.
                    throw new Exception("__________________________________________________\n" +
                                        "Triggers (*) exceeded max Instructions-Per-Update.\n" +
                                        "TO FIX THIS PROBLEM, TRY ONE OR MORE OF THESE:\n" +
                                        " - Redesign your triggers to use less code.\n" +
                                        " - Make CONFIG:IPU value bigger.\n" +
                                        " - If your trigger body was meant to be a loop, \n" +
                                        "     consider using the PRESERVE keyword instead\n" +
                                        "     to make it run one iteration per Update.\n" +
                                        "* (\"Trigger\" means a WHEN or ON or LOCK command.)\n");
                }
            }

            currentContext.InstructionPointer = currentInstructionPointer;
        }

        private void ContinueExecution()
        {
            bool executeNext = true;
            
            while (currentStatus == Status.Running && 
                   instructionsSoFarInUpdate < instructionsPerUpdate &&
                   executeNext &&
                   currentContext != null)
            {
                executeNext = ExecuteInstruction(currentContext);
                instructionsSoFarInUpdate++;
            }
        }

        private bool ExecuteInstruction(ProgramContext context)
        {
            bool DEBUG_EACH_OPCODE = false;
            
            Opcode opcode = context.Program[context.InstructionPointer];
            if (DEBUG_EACH_OPCODE)
            {
                UnityEngine.Debug.Log("ExecuteInstruction.  Opcode number " + context.InstructionPointer + " out of " + context.Program.Count +
                                      "\n                   Opcode is: " + opcode.ToString() );
            }
            
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
            if (currentContext.InstructionPointer >= (currentContext.Program.Count - 1)) return;

            string currentSourceName = currentContext.Program[currentContext.InstructionPointer].SourceName;

            while (currentContext.InstructionPointer < currentContext.Program.Count && 
                   currentContext.Program[currentContext.InstructionPointer].SourceName == currentSourceName)
            {
                currentContext.InstructionPointer++;
            }
        }

        public void CallBuiltinFunction(string functionName)
        {
            if (!functions.ContainsKey(functionName))
            {
                throw new Exception("Call to non-existent function " + functionName);
            }

            FunctionBase function = functions[functionName];
            function.Execute(shared);
        }

        public void ToggleFlyByWire(string paramName, bool enabled)
        {
            if (shared.BindingMgr == null) return;

            shared.BindingMgr.ToggleFlyByWire(paramName, enabled);
            currentContext.ToggleFlyByWire(paramName, enabled);
        }

        public List<string> GetCodeFragment(int contextLines)
        {
            return currentContext.GetCodeFragment(contextLines);
        }

        public void PrintStatistics()
        {
            if (!Config.Instance.ShowStatistics) return;

            shared.Screen.Print(string.Format("Total compile time: {0:F3}ms", totalCompileTime));
            shared.Screen.Print(string.Format("Total update time: {0:F3}ms", totalUpdateTime));
            shared.Screen.Print(string.Format("Total triggers time: {0:F3}ms", totalTriggersTime));
            shared.Screen.Print(string.Format("Total execution time: {0:F3}ms", totalExecutionTime));
            shared.Screen.Print(string.Format("Most Trigger instructions in one update: {0}", maxTriggerInstructionsSoFar));
            shared.Screen.Print(string.Format("Most Mainline instructions in one update: {0}", maxMainlineInstructionsSoFar));
            shared.Screen.Print(" ");

            totalCompileTime = 0D;
            totalUpdateTime = 0D;
            totalTriggersTime = 0D;
            totalExecutionTime = 0D;
            maxMainlineInstructionsSoFar = 0;
            maxTriggerInstructionsSoFar = 0;
        }

        public void OnSave(ConfigNode node)
        {
            try
            {
                var contextNode = new ConfigNode("context");

                // Save variables
                if (variables.Count > 0)
                {
                    var varNode = new ConfigNode("variables");

                    foreach (var kvp in variables)
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
                if (shared.Logger != null) shared.Logger.Log(e);
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

                if (shared.ScriptHandler != null && scriptBuilder.Length > 0)
                {
                    var programBuilder = new ProgramBuilder();
                    // TODO: figure out how to store the filename and reload it for arg 1 below:
                    // (Possibly all of OnLoad needs work because it never seemed to bring
                    // back the context fully right anyway, which is why this hasn't been
                    // addressed yet).
                    try
                    {
                        programBuilder.AddRange(shared.ScriptHandler.Compile("reloaded file", 1, scriptBuilder.ToString()));
                        List<Opcode> program = programBuilder.BuildProgram();
                        RunProgram(program, true);
                    }
                    catch (NullReferenceException ex)
                    {
                        UnityEngine.Debug.LogError("kOS: program builder failed on load. " + ex.Message);
                    }
                }
            }
            catch (Exception e)
            {
                if (shared.Logger != null) shared.Logger.Log(e);
            }
        }
    }
}
