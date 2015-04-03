using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using kOS.Safe.Binding;
using kOS.Safe.Compilation;
using kOS.Safe.Execution;
using kOS.Safe.Exceptions;
using kOS.Safe.Persistence;
using kOS.Safe.Utilities;
using kOS.Suffixed;
using kOS.Persistence;

namespace kOS.Execution
{
    public class CPU: ICpu
    {
        private enum Status
        {
            Running = 1,
            Waiting = 2
        }

        private readonly Stack stack;
        private readonly VariableScope globalVariables;
        private Status currentStatus;
        private double currentTime;
        private double timeWaitUntil;
        private readonly SharedObjects shared;
        private readonly List<ProgramContext> contexts;
        private ProgramContext currentContext;
        private VariableScope savedPointers;
        private int instructionsSoFarInUpdate;
        private int instructionsPerUpdate;
        
        // statistics
        private double totalCompileTime;
        private double totalUpdateTime;
        private double totalTriggersTime;
        private double totalExecutionTime;
        private int maxMainlineInstructionsSoFar;
        private int maxTriggerInstructionsSoFar;
        private readonly StringBuilder executeLog = new StringBuilder();

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
            globalVariables = new VariableScope(0, -1);
            contexts = new List<ProgramContext>();
            if (this.shared.UpdateHandler != null) this.shared.UpdateHandler.AddFixedObserver(this);
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
            // clear stack (which also orphans all local variables so they can get garbage collected)
            stack.Clear();
            // clear global variables
            globalVariables.Variables.Clear();
            // clear interpreter
            if (shared.Interpreter != null) shared.Interpreter.Reset();
            // load functions
            if(shared.FunctionManager != null)shared.FunctionManager.Load();
            // load bindings
            if (shared.BindingMgr != null) shared.BindingMgr.Load();
            // Booting message
            if (shared.Screen != null)
            {
                shared.Screen.ClearScreen();
                string bootMessage = string.Format("kOS Operating System\n" + "KerboScript v{0}\n \n" + "Proceed.\n", Core.VersionInfo);
                List<string>nags = Safe.Utilities.Debug.GetPendingNags();
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

                    bootMessage = nags.Aggregate(bootMessage, (current, msg) => current + (msg + "\n"));
                    bootMessage += "##################################################\n";
                }
                shared.Screen.Print(bootMessage);
            }

            if (shared.VolumeMgr == null) { SafeHouse.Logger.Log("No volume mgr"); }
            else if (!shared.VolumeMgr.CheckCurrentVolumeRange(shared.Vessel)) { SafeHouse.Logger.Log("Boot volume not in range"); }
            else if (shared.VolumeMgr.CurrentVolume == null) { SafeHouse.Logger.Log("No current volume"); }
            else if (shared.ScriptHandler == null) { SafeHouse.Logger.Log("No script handler"); }
            else if (shared.VolumeMgr.CurrentVolume.GetByName("boot") == null) { SafeHouse.Logger.Log("Boot File is Missing"); }
            else {
                shared.ScriptHandler.ClearContext("program");

                var programContext = ((CPU)shared.Cpu).GetProgramContext();
                programContext.Silent = true;
                var options = new CompilerOptions { LoadProgramsInSameAddressSpace = true };
                string filePath = shared.VolumeMgr.GetVolumeRawIdentifier(shared.VolumeMgr.CurrentVolume) + "/" + "boot";
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
            SafeHouse.Logger.Log("Pushing context staring with: " + context.GetCodeFragment(0).FirstOrDefault());
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
            SafeHouse.Logger.Log("Popping context " + contexts.Count);
            if (contexts.Any())
            {
                // remove the last context
                var contextRemove = contexts.Last();
                contexts.Remove(contextRemove);
                contextRemove.DisableActiveFlyByWire(shared.BindingMgr);
                SafeHouse.Logger.Log("Removed Context " + contextRemove.GetCodeFragment(0).FirstOrDefault());

                if (contexts.Any())
                {
                    currentContext = contexts.Last();
                    currentContext.EnableActiveFlyByWire(shared.BindingMgr);
                    RestorePointers();
                    SafeHouse.Logger.Log("New current context " + currentContext.GetCodeFragment(0).FirstOrDefault());
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
        
        /// <summary>
        /// Push a single thing onto the secret "over" stack.
        /// </summary>
        public void PushAboveStack(object thing)
        {
            PushStack(thing);
            MoveStackPointer(-1);            
        }
        
        /// <summary>
        /// Pop one or more things from the secret "over" stack, only returning the
        /// finalmost thing popped.  (i.e if you pop 3 things then you get:
        /// pop once and throw away, pop again and throw away, pop again and return the popped thing.)
        /// </summary>
        public object PopAboveStack(int howMany)
        {
            object returnVal = new Int32(); // bogus return val if given a bogus "pop zero things" request.
            while (howMany > 0)
            {
                MoveStackPointer(1);
                returnVal = PopStack();
                --howMany;
            }
            return returnVal;
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
            // To be honest, I'm a little afraid of this.  It appears to be doing
            // something with locks (and now user functions) whenever you
            // switch contexts from interpreter to program and it seems to be
            // presuming the only such pointers that need to exist are going to be
            // global.  This was written by marianoapp before I added locals,
            // and I don't understand what it's for -- Dunbaratu
            
            savedPointers = new VariableScope(0,-1);
            var pointers = new List<string>(globalVariables.Variables.Keys.Where(v => v.Contains('*')));

            foreach (var pointerName in pointers)
            {
                savedPointers.Variables.Add(pointerName, globalVariables.Variables[pointerName]);
                globalVariables.Variables.Remove(pointerName);
            }
            SafeHouse.Logger.Log(string.Format("Saving and removing {0} pointers", pointers.Count));
        }

        private void RestorePointers()
        {
            // To be honest, I'm a little afraid of this.  It appears to be doing
            // something with locks (and now user functions) whenever you
            // switch contexts from program to interpreter and it seems to be
            // presuming the only such pointers that need to exist are going to be
            // global.  This was written by marianoapp before I added locals,
            // and I don't understand what it's for -- Dunbaratu
            
            int restoredPointers = 0;
            int deletedPointers = 0;

            foreach (var item in savedPointers.Variables)
            {
                if (globalVariables.Variables.ContainsKey(item.Key))
                {
                    // if the pointer exists it means it was redefined from inside a program
                    // and it's going to be invalid outside of it, so we remove it
                    globalVariables.Variables.Remove(item.Key);
                    deletedPointers++;
                    // also remove the corresponding trigger if exists
                    if (item.Value.Value is int)
                        RemoveTrigger((int)item.Value.Value);
                }
                else
                {
                    globalVariables.Variables.Add(item.Key, item.Value);
                    restoredPointers++;
                }
            }

            SafeHouse.Logger.Log(string.Format("Deleting {0} pointers and restoring {1} pointers", deletedPointers, restoredPointers));
        }

        public void RunProgram(List<Opcode> program)
        {
            RunProgram(program, false);
        }

        public void RunProgram(List<Opcode> program, bool silent)
        {
            if (!program.Any()) return;

            var newContext = new ProgramContext(false, program) { Silent = silent };
            PushContext(newContext);
        }

        public void BreakExecution(bool manual)
        {
            SafeHouse.Logger.Log(string.Format("Breaking Execution {0} Contexts: {1}", manual ? "Manually" : "Automatically", contexts.Count));
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
        /// Gets the dictionary N levels of nesting down the dictionary stack,
        /// where zero is the current localmost level.
        /// Never errors out or fails.  If N is too large you just end up with
        /// the global scope dictionary.
        /// Does not allow the walk to go past the start of the current function
        /// scope.
        /// </summary>
        /// <param name="peekDepth">how far down the peek under the top.  0 = localmost.</param>
        /// <returns>The dictionary found, or the global dictionary if peekDepth is too big.</returns>
        private VariableScope GetNestedDictionary(int peekDepth)
        {
            object stackItem = true; // any non-null value will do here, just to get the loop started.
            for (int rawStackDepth = 0 ; stackItem != null && peekDepth >= 0; ++rawStackDepth)
            {
                stackItem = stack.Peek(-1 - rawStackDepth);
                if (stackItem is VariableScope)
                    --peekDepth;
                if (stackItem is SubroutineContext)
                    stackItem = null; // once we hit the bottom of the current subroutine on the runtime stack - jump all the way out to global.
            }
            return stackItem == null ? globalVariables : (VariableScope) stackItem;
        }

        /// <summary>
        /// Gets the dictionary that contains the given identifier, starting the
        /// search at the local level and scanning the scopes upward all the
        /// way to the global dictionary.<br/>
        /// Does not allow the walk to use scope frames that were not directly in this
        /// scope's lexical chain.  It skips over scope frames from other branches
        /// of the parse tree.  (i.e. if a function calls a function elsewhere).<br/>
        /// Returns null when no hit was found.<br/>
        /// </summary>
        /// <param name="identifier">identifier name to search for</param>
        /// <returns>The dictionary found, or null if no dictionary contains the identifier.</returns>
        private VariableScope GetNestedDictionary(string identifier)
        {
            Int16 rawStackDepth = 0 ;
            while (true) /*all of this loop's exits are explicit break or return statements*/
            {
                object stackItem;
                bool stackExhausted = !(stack.PeekCheck(-1 - rawStackDepth, out stackItem));
                if (stackExhausted) 
                    break;
                VariableScope localDict = stackItem as VariableScope;
                if (localDict == null) // some items on the stack might not be variable scopes.  skip them.
                {
                    ++rawStackDepth;
                    continue;
                }

                if (localDict.Variables.ContainsKey(identifier))
                    return localDict;

                
                // Get the next VariableScope that is the lexical (not runtime) parent of this one:
                // -------------------------------------------------------------------------------

                // Scan the stack until the variable scope with the right parent ID is seen:
                Int16 skippedLevels = 0;
                while ( !(stackExhausted))
                {
                    bool needsIncrement = true;
                    VariableScope scopeFrame = stackItem as VariableScope;
                    if (scopeFrame != null) // skip cases where the thing on the stack isn't a variable scope.
                    {
                        // If the scope id of this frame is my parent ID, then we found it and are done.
                        if (scopeFrame.ScopeId == localDict.ParentScopeId)
                        {
                            needsIncrement = false;
                            break;
                        }

                        // In the case where the variable scope is the SAME lexical ID as myself, that
                        // means I recursively called myself and the thing on the runtime stack just before
                        // me is ... another instance of me.  In that case just follow it's parent skip level
                        if (scopeFrame.ScopeId == localDict.ScopeId && scopeFrame.ParentSkipLevels > 0)
                        {
                            skippedLevels += scopeFrame.ParentSkipLevels;
                            rawStackDepth += scopeFrame.ParentSkipLevels;
                            needsIncrement = false;
                        }
                    }

                    if (needsIncrement)
                    {
                        ++skippedLevels;
                        ++rawStackDepth;
                    }
                    stackExhausted = !(stack.PeekCheck(-1 - rawStackDepth, out stackItem));
                }
                
                // Record how many levels had to be skipped for that to work.  In future calls of this
                // method, it will know how far to jump in the stack without doing that scan.  This can
                // be quite a speedup when dealing with nested recursion, where the runtime stack might
                // be a hundred levels deep of the same function calling itself before hitting its lexical parent.
                if (stackItem != null && localDict.ParentSkipLevels == 0)
                    localDict.ParentSkipLevels = skippedLevels;
            }
            if (globalVariables.Variables.ContainsKey(identifier))
                return globalVariables;
            else
                return null;
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
            trace.Insert(0, currentContext.InstructionPointer); // perpend current IP
            return trace;
        }

        /// <summary>
        /// Get the value of a variable or create it at global scope if not found.
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        private Variable GetOrCreateVariable(string identifier)
        {
            Variable variable = GetVariable(identifier,false,true);
            if (variable == null)
            {
                variable = new Variable {Name = identifier};
                AddVariable(variable, identifier, false);
            }
            return variable;
        }
        
        public string DumpVariables()
        {
            var msg = new StringBuilder();
            msg.AppendLine("============== STACK VARIABLES ===============");
            msg.AppendLine(stack.Dump());
            msg.AppendLine("============== GLOBAL VARIABLES ==============");
            foreach (string ident in globalVariables.Variables.Keys)
            {
                string line;
                try
                {
                    Variable v = globalVariables.Variables[ident];
                    line = ident;
                    if (v == null || v.Value == null)
                        line += "is <null>";
                    else
                        line += " is a " + v.Value.GetType().FullName + " with value = " + v.Value;
                }
                catch (Exception e)
                {
                    // This is necessary because of the deprecation exceptions that
                    // get raised by FlightStats when you try to print all of them out:
                    line = ident + "= <value caused exception>\n    " + e.Message;
                }
                msg.AppendLine(line);
            }
            SafeHouse.Logger.Log(msg.ToString());
            return "Variable dump is in the output log";
        }

        /// <summary>
        /// Get the variable's contents, performing a lookup through all nesting levels
        /// up to global.
        /// </summary>
        /// <param name="identifier">variable to look for</param>
        /// <param name="barewordOkay">Is it acceptable for the variable to
        ///   not exist, in which case its bare name will be returned as the value.</param>
        /// <param name="failOkay">Is it acceptable for the variable to
        ///   not exist, in which case a null will be returned as the value.</param>
        /// <returns>the value that was found</returns>
        private Variable GetVariable(string identifier, bool barewordOkay = false, bool failOkay = false)
        {
            identifier = identifier.ToLower();
            VariableScope foundDict = GetNestedDictionary(identifier);
            if (foundDict != null)
                return foundDict.Variables[identifier];
            if (barewordOkay)
            {
                string strippedIdent = identifier.TrimStart('$');
                return new Variable {Name = strippedIdent, Value = strippedIdent};
            }
            if (failOkay)
                return null;
            else
                throw new KOSUndefinedIdentifierException(identifier.TrimStart('$'),"");
        }

        /// <summary>
        /// Make a new variable at either the local depth or the
        /// global depth depending.
        /// throws exception if it already exists
        /// </summary>
        /// <param name="variable">variable to add</param>
        /// <param name="identifier">name of variable to adde</param>
        /// <param name="local">true if you want to make it at local depth</param>
        public void AddVariable(Variable variable, string identifier, bool local)
        {
            identifier = identifier.ToLower();
            
            if (!identifier.StartsWith("$"))
            {
                identifier = "$" + identifier;
            }
            
            VariableScope whichDict;
            if (local)
                whichDict = GetNestedDictionary(0);
            else
                whichDict = globalVariables;
            if (whichDict.Variables.ContainsKey(identifier))
            {
                if (whichDict.Variables[identifier].Value is BoundVariable)
                    throw new KOSIdentiferClashException(identifier);
                else
                    whichDict.Variables.Remove(identifier);
            }
            whichDict.Variables.Add(identifier, variable);
        }

        public bool VariableIsRemovable(Variable variable)
        {
            return !(variable is BoundVariable);
        }

        /// <summary>
        /// Removes a variable, following current scoping rules, removing
        /// the innermost scope of the variable that is found.<br/>
        /// <br/>
        /// If the variable cannot be found, it fails silently without complaint.
        /// </summary>
        /// <param name="identifier">varible to remove.</param>
        public void RemoveVariable(string identifier)
        {
            identifier = identifier.ToLower();
            VariableScope foundDict = GetNestedDictionary(identifier);
            if (foundDict != null && VariableIsRemovable(foundDict.Variables[identifier]))
            {
                // Tell Variable to orphan its old value now.  Faster than relying 
                // on waiting several seconds for GC to eventually call ~Variable()
                foundDict.Variables[identifier].Value = null;
                
                foundDict.Variables.Remove(identifier);
            }
        }

        /// <summary>
        ///   Given a value which may or may not be a variable name, return the value
        ///   back.  If it's not a variable, return it as-is.  If it's a variable,
        ///   look it up and return that.
        /// </summary>
        /// <param name="testValue">the object which might be a variable name</param>
        /// <param name="barewordOkay">
        ///   Is this a case in which it's acceptable for the
        ///   variable not to exist, and if it doesn't exist then the variable name itself
        ///   is the value?
        /// </param>
        /// <returns>The value after the steps described have been performed.</returns>
        public object GetValue(object testValue, bool barewordOkay = false)
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
                Variable variable = GetVariable(identifier, barewordOkay);
                return variable.Value;
            }
            return testValue;
        }

        /// <summary>
        /// Try to make a new local variable at the localmost scoping level and
        /// give it a starting value.  It errors out of there is already one there
        /// by the same name.<br/>
        /// <br/>
        /// This does NOT scan up the scoping stack like SetValue() does.
        /// It operates at the local level only.<br/>
        /// <br/>
        /// This is the normal way to make a new local variable.  You cannot make a 
        /// local variable without attempting to give it a value.
        /// </summary>
        /// <param name="identifier">variable name to attempt to store into</param>
        /// <param name="value">value to put into it</param>
        public void SetNewLocal(string identifier, object value)
        {
            Variable variable = new Variable {Name = identifier};
            AddVariable(variable, identifier, true);
            variable.Value = value;
        }

        /// <summary>
        /// Try to set the value of the identifier at the localmost
        /// level possible, by scanning up the scope stack to find
        /// the local-most level at which the identifier is a variable,
        /// and assigning it the value there.<br/>
        /// <br/>
        /// If no such value is found, all the way up to the global level,
        /// then it resorts to making a global variable with the name and using that.<br/>
        /// <br/>
        /// This is the normal way to make a new global variable.  You cannot make a 
        /// global variable without attempting to give it a value.
        /// </summary>
        /// <param name="identifier">variable name to attempt to store into</param>
        /// <param name="value">value to put into it</param>
        public void SetValue(string identifier, object value)
        {
            Variable variable = GetOrCreateVariable(identifier);
            variable.Value = value;
        }
        
        /// <summary>
        /// Try to set the value of the identifier at the localmost
        /// level possible, by scanning up the scope stack to find
        /// the local-most level at which the identifier is a variable,
        /// and assigning it the value there.<br/>
        /// <br/>
        /// If no such value is found, an error is thrown.  It only stores into
        /// variables that already exist, refusing to create new variables.<br/>
        /// <br/>
        /// </summary>
        /// <param name="identifier">variable name to attempt to store into</param>
        /// <param name="value">value to put into it</param>
        public void SetValueExists(string identifier, object value)
        {
            Variable variable = GetVariable(identifier);
            variable.Value = value;
        }

        /// <summary>
        /// Pop a value off the stack, and if it's a variable name then get its value,
        /// else just return it as it is.
        /// </summary>
        /// <param name="barewordOkay">Is this a context in which it's acceptable for
        ///   a variable not existing error to occur (in which case the identifier itself
        ///   should therefore become a string object returned)?</param>
        /// <returns>value off the stack</returns>
        public object PopValue(bool barewordOkay = false)
        {
            return GetValue(PopStack(), barewordOkay);
        }
        
        /// <summary>
        /// Peek at a value atop the stack without popping it, and if it's a variable name then get its value,
        /// else just return it as it is.<br/>
        /// <br/>
        /// NOTE: Evaluating variables when you don't really need to is pointlessly expensive, as it
        /// needs to walk the scoping stack to exhaust a search.  If you don't need to evaluate variables,
        /// then consider using PeekRaw() instead.<br/>
        /// </summary>
        /// <param name="digDepth">Peek at the element this far down the stack (0 means top, 1 means just under the top, etc)</param>
        /// <param name="barewordOkay">Is this a context in which it's acceptable for
        ///   a variable not existing error to occur (in which case the identifier itself
        ///   should therefore become a string object returned)?</param>
        /// <returns>value off the stack</returns>
        public object PeekValue(int digDepth, bool barewordOkay = false)
        {
            return GetValue(stack.Peek(digDepth), barewordOkay);
        }

        /// <summary>
        /// Peek at a value atop the stack without popping it, and without evaluating it to get the variable's
        /// value.  (i.e. if the thing in the stack is $foo, and the variable foo has value 5, you'll get the string
        /// "$foo" returned, not the integer 5).
        /// </summary>
        /// <param name="digDepth">Peek at the element this far down the stack (0 means top, 1 means just under the top, etc)</param>
        /// <param name="checkOkay">Tells you whether or not the stack was exhausted.  If it's false, then the peek went too deep.</param>
        /// <returns>value off the stack</returns>        
        public object PeekRaw(int digDepth, out bool checkOkay)
        {
            object returnValue;
            checkOkay = stack.PeekCheck(digDepth,out returnValue);
            return returnValue;
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

        public void KOSFixedUpdate(double deltaTime)
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
                    SafeHouse.Logger.Log(stack.Dump());
                }

                if (contexts.Count == 1)
                {
                    // interpreter context
                    SkipCurrentInstructionId();
                    stack.Clear(); // Get rid of this interpreter command's cruft.
                }
                else
                {
                    // break execution of all programs and pop interpreter context
                    PopFirstContext();
                    stack.Clear(); // If breaking all execution, get rid of the cruft here too.
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
                    executeLog.Remove(0,executeLog.Length); // why doesn't StringBuilder just have a Clear() operator?
                    while (executeNext && instructionsSoFarInUpdate < instructionsPerUpdate)
                    {
                        executeNext = ExecuteInstruction(currentContext);
                        instructionsSoFarInUpdate++;
                    }
                    if (executeLog.Length > 0)
                        SafeHouse.Logger.Log(executeLog.ToString());
                }
                catch (Exception e)
                {
                    RemoveTrigger(triggerPointer);
                    shared.Logger.Log(e);
                }
                if (instructionsSoFarInUpdate >= instructionsPerUpdate)
                {
                    throw new KOSLongTriggerException(instructionsSoFarInUpdate);
                }
            }

            currentContext.InstructionPointer = currentInstructionPointer;
        }

        private void ContinueExecution()
        {
            bool executeNext = true;
            executeLog.Remove(0,executeLog.Length); // why doesn't StringBuilder just have a Clear() operator?
            while (currentStatus == Status.Running && 
                   instructionsSoFarInUpdate < instructionsPerUpdate &&
                   executeNext &&
                   currentContext != null)
            {
                executeNext = ExecuteInstruction(currentContext);
                instructionsSoFarInUpdate++;
            }
            if (executeLog.Length > 0)
                SafeHouse.Logger.Log(executeLog.ToString());
        }

        private bool ExecuteInstruction(ProgramContext context)
        {
            bool DEBUG_EACH_OPCODE = false;
            
            Opcode opcode = context.Program[context.InstructionPointer];
            if (DEBUG_EACH_OPCODE)
            {
                executeLog.Append(String.Format("Executing Opcode {0:0000}/{1:0000} {2} {3}\n",
                                                context.InstructionPointer, context.Program.Count, opcode.Label, opcode.ToString()));
            }
            try
            {
                if (!(opcode is OpcodeEOF || opcode is OpcodeEOP))
                {
                    opcode.Execute(this);
                    context.InstructionPointer += opcode.DeltaInstructionPointer;
                    return true;
                }
                if (opcode is OpcodeEOP)
                {
                    BreakExecution(false);
                    SafeHouse.Logger.Log("Execution Broken");
                }
            }
            catch (Exception)
            {
                // exception will skip the normal printing of the log buffer,
                // so print what we have so far before throwing up the exception:
                if (executeLog.Length > 0)
                    SafeHouse.Logger.Log(executeLog.ToString());
                throw;
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
            shared.FunctionManager.CallFunction(functionName);
        }
        
        public bool BuiltInExists(string functionName)
        {
            return shared.FunctionManager.Exists(functionName);
        }

        public void ToggleFlyByWire(string paramName, bool enabled)
        {
            if (shared.BindingMgr == null) return;

            shared.BindingMgr.ToggleFlyByWire(paramName, enabled);
            currentContext.ToggleFlyByWire(paramName, enabled);
        }

        public void SelectAutopilotMode(string autopilotMode)
        {
            shared.BindingMgr.SelectAutopilotMode(autopilotMode);
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
                if (globalVariables.Variables.Count > 0)
                {
                    var varNode = new ConfigNode("variables");

                    foreach (var kvp in globalVariables.Variables)
                    {
                        if (!(kvp.Value is BoundVariable) &&
                            (kvp.Value.Name.IndexOfAny(new[] { '*', '-' }) == -1))  // variables that have this characters are internal and shouldn't be persisted
                        {
                            if (kvp.Value.Value.GetType().ToString() == "System.String")  // if the variable is a string, enclose the value in ""
                            {
                                varNode.AddValue(kvp.Key.TrimStart('$'), (char)34 + PersistenceUtilities.EncodeLine(kvp.Value.Value.ToString()) + (char)34);
                            }
                            else
                            {
                                varNode.AddValue(kvp.Key.TrimStart('$'), PersistenceUtilities.EncodeLine(kvp.Value.Value.ToString()));
                            }
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
                            string varValue = PersistenceUtilities.DecodeLine(value.value);
                            scriptBuilder.AppendLine(string.Format("set {0} to {1}.", value.name, varValue));
                        }
                    }
                }

                if (shared.ScriptHandler == null || scriptBuilder.Length <= 0) return;

                var programBuilder = new ProgramBuilder();
                // TODO: figure out how to store the filename and reload it for arg 1 below:
                // (Possibly all of OnLoad needs work because it never seemed to bring
                // back the context fully right anyway, which is why this hasn't been
                // addressed yet).
                try
                {
                    SafeHouse.Logger.Log("Parsing Context:\n\n" + scriptBuilder);
                    
                    // TODO - make this set up compiler options and pass them in properly, so we can detect built-ins properly.
                    // (for the compiler to detect the difference between a user function call and a built-in, it needs to be
                    // passed the FunctionManager object from Shared.)
                    // this isn't fixed mainly because this OnLoad() code is a major bug fire already anyway and needs to be 
                    // fixed, but that's way out of scope for the moment:
                    programBuilder.AddRange(shared.ScriptHandler.Compile("reloaded file", 1, scriptBuilder.ToString()));
                    List<Opcode> program = programBuilder.BuildProgram();
                    RunProgram(program, true);
                }
                catch (NullReferenceException ex)
                {
                    SafeHouse.Logger.Log("program builder failed on load. " + ex.Message);
                }
            }
            catch (Exception e)
            {
                if (shared.Logger != null) shared.Logger.Log(e);
            }
        }

        public void Dispose()
        {
            shared.UpdateHandler.RemoveFixedObserver(this);
        }
    }
}
