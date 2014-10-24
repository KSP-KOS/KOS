using System;
using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Exceptions;

namespace kOS.Safe.Compilation.KS
{
    class Compiler
    {
        private CodePart _part;
        private Context _context;
        private List<Opcode> _currentCodeSection = null;
        private bool _addBranchDestination = false;
        private ParseNode _lastNode = null;
        private int _startLineNum = 1;
        private short _lastLine = 0;
        private short _lastColumn = 0;
        private List<List<Opcode>> _breakList = new List<List<Opcode>>();
        private List<string> _triggerRemoveNames = new List<string>();
        private bool _nowCompilingTrigger = false;
        private bool _compilingSetDestination = false;
        private bool _identifierIsVariable = false;
        private bool _identifierIsSuffix = false;
        private bool _nowInALoop = false;
        private List<ParseNode> _programParameters = new List<ParseNode>();
        private CompilerOptions _options;

        private readonly Dictionary<string, string> _functionsOverloads = new Dictionary<string, string>() { { "round|1", "roundnearest" },
            { "round|2", "round"} };
        
        public CodePart Compile(int startLineNum, ParseTree tree, Context context, CompilerOptions options)
        {
            _part = new CodePart();
            _context = context;
            _options = options;
            _startLineNum = startLineNum;
            
            // Set to true if you need to diagnose bugs in the compiler and turn off the catch-all
            // exception suppression.
            //
            bool testingCompiler = true;

            try
            {
                if (tree.Nodes.Count > 0)
                {
                    PreProcess(tree);
                    CompileProgram(tree);
                }
            }
            catch (KOSException kosException)
            {
                if (_lastNode != null)
                {
                    throw;  // TODO something more sophisticated will go here that will
                    // attach source/line information to the exception before throwing it upward.
                    // that's why this seemingly pointless "catch and then throw again" is here.
                }
                // It turns out Console.WriteLine gets redirected to the same file as UnityEngine.Debug.Log uses,
                // and it's a way to log things in kOS.Safe without depending on Unity libraries.
                Console.WriteLine("Exception in Compiler: " + kosException.Message);
                Console.WriteLine(kosException.StackTrace);
                throw;  // throw it up in addition to logging the stack trace, so the kOS terminal will also give the user some message.
            }

            return _part;
        }

        private void CompileProgram(ParseTree tree)
        {
            _currentCodeSection = _part.MainCode;
            PushProgramParameters();
            VisitNode(tree.Nodes[0]);

            if (_addBranchDestination)
            {
                AddOpcode(new OpcodeNOP());
            }
        }
        
        /// <summary>
        /// Only those nodes which are primitive tokens will have line number
        /// information.  So perform a leftmost search of the subtree of nodes
        /// until a node with a token with a line number is found:
        /// </summary>
        /// <returns>true if a line number was found in this node.  mostly used for internal recursion
        /// and can be safely ignored when this is called.</returns>
        private bool SetLineNum(ParseNode node)
        {
            if (node != null && node.Token != null && node.Token.Line > 0)
            {
                _lastLine = (short) (node.Token.Line + (_startLineNum - 1));
                _lastColumn = (short) (node.Token.Column);
                return true;
            }
            else
            {
                foreach (ParseNode childNode in node.Nodes)
                {
                    if (SetLineNum(childNode))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        
        private Opcode AddOpcode(Opcode opcode, string destinationLabel)
        {
            opcode.Label = GetNextLabel(true);
            opcode.DestinationLabel = destinationLabel;
            opcode.SourceLine = _lastLine;
            opcode.SourceColumn = _lastColumn;
            _currentCodeSection.Add(opcode);
            _addBranchDestination = false;
            return opcode;
        }

        private Opcode AddOpcode(Opcode opcode)
        {
            return AddOpcode(opcode, "");
        }

        private string GetNextLabel(bool increment)
        {
            string newLabel = string.Format("@{0:0000}", _context.LabelIndex + 1);
            if (increment) _context.LabelIndex++;
            return newLabel;
        }

        private void PreProcess(ParseTree tree)
        {
            ParseNode rootNode = tree.Nodes[0];
            PreProcessLocks(rootNode);
            PreProcessStatements(rootNode);
        }

        private void PreProcessLocks(ParseNode node)
        {
            IterateLocks(node, IdentifyLocks);
            IterateLocks(node, PreProcessLockStatement);
        }

        private void IterateLocks(ParseNode node, Action<ParseNode> action)
        {
            switch (node.Token.Type)
            {
                    // statements that can have a lock inside
                case TokenType.Start:
                case TokenType.instruction_block:
                case TokenType.instruction:
                case TokenType.if_stmt:
                case TokenType.until_stmt:
                case TokenType.on_stmt:
                case TokenType.when_stmt:
                    foreach (ParseNode childNode in node.Nodes)
                        IterateLocks(childNode, action);
                    break;
                case TokenType.lock_stmt:
                    action.Invoke(node);
                    break;
                default:
                    break;
            }
        }

        private void PreProcessStatements(ParseNode node)
        {
            
            _lastNode = node;
            
            SetLineNum(node);
            
            switch (node.Token.Type)
            {
                    // statements that can have a lock inside
                case TokenType.Start:
                case TokenType.instruction_block:
                case TokenType.instruction:
                case TokenType.if_stmt:
                case TokenType.until_stmt:
                    PreProcessChildNodes(node);
                    break;
                case TokenType.on_stmt:
                    PreProcessChildNodes(node);
                    PreProcessOnStatement(node);
                    break;
                case TokenType.when_stmt:
                    PreProcessChildNodes(node);
                    PreProcessWhenStatement(node);
                    break;
                case TokenType.wait_stmt:
                    PreProcessWaitStatement(node);
                    break;
                case TokenType.declare_stmt:
                    PreProcessProgramParameters(node);
                    break;
                case TokenType.run_stmt:
                    PreProcessRunStatement(node);
                    break;
                default:
                    break;
            }
        }

        private void PreProcessChildNodes(ParseNode node)
        {
            foreach (ParseNode childNode in node.Nodes)
            {
                PreProcessStatements(childNode);
            }
        }

        private void PreProcessOnStatement(ParseNode node)
        {
            SetLineNum(node);
            int expressionHash = ConcatenateNodes(node).GetHashCode();
            string triggerIdentifier = "on-" + expressionHash.ToString();
            Trigger triggerObject = _context.Triggers.GetTrigger(triggerIdentifier);
            triggerObject.SetTriggerVariable(GetIdentifierText(node));

            _currentCodeSection = triggerObject.Code;
            AddOpcode(new OpcodePush(triggerObject.VariableNameOldValue));
            AddOpcode(new OpcodePush(triggerObject.VariableName));
            AddOpcode(new OpcodeCompareEqual());
            AddOpcode(new OpcodeLogicNot());
            Opcode branchOpcode = AddOpcode(new OpcodeBranchIfFalse());
            
            // make flag that remembers whether to remove trigger:
            // defaults to true = removal should happen.
            string triggerRemoveVarName = "$remove-" + triggerIdentifier;
            PushTriggerRemoveName( triggerRemoveVarName );
            AddOpcode(new OpcodePush( triggerRemoveVarName ));
            AddOpcode(new OpcodePush(true));
            AddOpcode(new OpcodeStore());

            VisitNode(node.Nodes[2]);

            // Reset the "old value" so the boolean has to change *again* to retrigger
            AddOpcode(new OpcodePush(triggerObject.VariableNameOldValue));
            AddOpcode(new OpcodePush(triggerObject.VariableName));
            AddOpcode(new OpcodeStore());

            // Skip removing the trigger if PRESERVE happened:
            PopTriggerRemoveName(); // Throw away return value.
            AddOpcode(new OpcodePush( triggerRemoveVarName ));
            Opcode skipRemoval = AddOpcode(new OpcodeBranchIfFalse());
            
            AddOpcode(new OpcodePushRelocateLater(null),triggerObject.GetFunctionLabel());
            AddOpcode(new OpcodeRemoveTrigger());
            Opcode eofOpcode = AddOpcode(new OpcodeEOF());
            branchOpcode.DestinationLabel = eofOpcode.Label;
            skipRemoval.DestinationLabel = eofOpcode.Label;
        }

        private void PreProcessWhenStatement(ParseNode node)
        {
            SetLineNum(node);
            int expressionHash = ConcatenateNodes(node).GetHashCode();
            string triggerIdentifier = "when-" + expressionHash.ToString();
            Trigger triggerObject = _context.Triggers.GetTrigger(triggerIdentifier);

            _currentCodeSection = triggerObject.Code;
            VisitNode(node.Nodes[1]);
            Opcode branchOpcode = AddOpcode(new OpcodeBranchIfFalse());

            // make flag that remembers whether to remove trigger:
            // defaults to true = removal should happen.
            string triggerRemoveVarName = "$remove-" + triggerIdentifier;
            PushTriggerRemoveName( triggerRemoveVarName );
            AddOpcode(new OpcodePush( triggerRemoveVarName ));
            AddOpcode(new OpcodePush(true));
            AddOpcode(new OpcodeStore());

            VisitNode(node.Nodes[3]);

            // Skip removing the trigger if PRESERVE happened:
            PopTriggerRemoveName(); // Throw away return value.
            AddOpcode(new OpcodePush( triggerRemoveVarName ));
            Opcode skipRemoval = AddOpcode(new OpcodeBranchIfFalse());

            AddOpcode(new OpcodePushRelocateLater(null),triggerObject.GetFunctionLabel());
            AddOpcode(new OpcodeRemoveTrigger());
            Opcode eofOpcode = AddOpcode(new OpcodeEOF());
            branchOpcode.DestinationLabel = eofOpcode.Label;
            skipRemoval.DestinationLabel = eofOpcode.Label;
        }

        private void PreProcessWaitStatement(ParseNode node)
        {
            SetLineNum(node);
            if (node.Nodes.Count == 4)
            {
                // wait condition
                int expressionHash = ConcatenateNodes(node).GetHashCode();
                string triggerIdentifier = "wait-" + expressionHash.ToString();
                Trigger triggerObject = _context.Triggers.GetTrigger(triggerIdentifier);

                _currentCodeSection = triggerObject.Code;
                VisitNode(node.Nodes[2]);
                Opcode branchOpcode = AddOpcode(new OpcodeBranchIfFalse());
                AddOpcode(new OpcodeEndWait());
                AddOpcode(new OpcodePushRelocateLater(null),triggerObject.GetFunctionLabel());
                AddOpcode(new OpcodeRemoveTrigger());
                Opcode eofOpcode = AddOpcode(new OpcodeEOF());
                branchOpcode.DestinationLabel = eofOpcode.Label;
            }
        }

        private string ConcatenateNodes(ParseNode node)
        {
            string concatenated = node.Token.Text;

            if (node.Nodes.Count > 0)
            {
                foreach (ParseNode childNode in node.Nodes)
                {
                    concatenated += ConcatenateNodes(childNode);
                }
            }

            return concatenated;
        }

        private void IdentifyLocks(ParseNode node)
        {
            string lockIdentifier = node.Nodes[1].Token.Text;
            Lock lockObject = _context.Locks.GetLock(lockIdentifier);
        }

        private void PreProcessLockStatement(ParseNode node)
        {
            SetLineNum(node);
            string lockIdentifier = node.Nodes[1].Token.Text;
            Lock lockObject = _context.Locks.GetLock(lockIdentifier);
            int expressionHash = ConcatenateNodes(node.Nodes[3]).GetHashCode();

            if (!lockObject.IsInitialized())
            {
                // initialization code
                _currentCodeSection = lockObject.InitializationCode;
                AddOpcode(new OpcodePush(lockObject.PointerIdentifier));
                AddOpcode(new OpcodePushRelocateLater(null),lockObject.DefaultLabel);
                AddOpcode(new OpcodeStore());

                if (lockObject.IsSystemLock())
                {
                    // add trigger
                    string triggerIdentifier = "lock-" + lockObject.Identifier;
                    Trigger triggerObject = _context.Triggers.GetTrigger(triggerIdentifier);

                    short rememberLastLine = _lastLine;
                    _lastLine = -1; // special flag telling the error handler that these opcodes came from the system itself, when reporting the error
                    _currentCodeSection = triggerObject.Code;
                    AddOpcode(new OpcodePush("$" + lockObject.Identifier));
                    AddOpcode(new OpcodeCall(lockObject.PointerIdentifier));
                    AddOpcode(new OpcodeStore());
                    AddOpcode(new OpcodeEOF());
                    _lastLine = rememberLastLine;
                }

                // default function
                _currentCodeSection = lockObject.GetLockFunction(0);
                AddOpcode(new OpcodePush("$" + lockObject.Identifier)).Label = lockObject.DefaultLabel;
                AddOpcode(new OpcodeReturn());
            }

            // function code
            _currentCodeSection = lockObject.GetLockFunction(expressionHash);
            VisitNode(node.Nodes[3]);
            AddOpcode(new OpcodeReturn());
        }

        private void PreProcessProgramParameters(ParseNode node)
        {
            SetLineNum(node);
            // if the declaration is a parameter
            if (node.Nodes[1].Token.Type == TokenType.PARAMETER)
            {
                for (int index = 2; index < node.Nodes.Count; index += 2)
                {
                    _programParameters.Add(node.Nodes[index]);
                }
            }
        }

        private void PreProcessRunStatement(ParseNode node)
        {
            SetLineNum(node);
            if (_options.LoadProgramsInSameAddressSpace)
            {
                bool hasON = node.Nodes.Any(cn => cn.Token.Type == TokenType.ON);
                if (!hasON)
                {
                    string subprogramName = node.Nodes[1].Token.Text;
                    if (!_context.Subprograms.Contains(subprogramName))
                    {
                        Subprogram subprogramObject = _context.Subprograms.GetSubprogram(subprogramName);
                        // Function code
                        _currentCodeSection = subprogramObject.FunctionCode;
                        // verify if the program has been loaded
                        Opcode functionStart = AddOpcode(new OpcodePush(subprogramObject.PointerIdentifier));
                        AddOpcode(new OpcodePush(0));
                        AddOpcode(new OpcodeCompareEqual());
                        OpcodeBranchIfFalse branchOpcode = new OpcodeBranchIfFalse();
                        AddOpcode(branchOpcode);
                        // if it wasn't then load it now
                        AddOpcode(new OpcodePush(subprogramObject.PointerIdentifier));
                        AddOpcode(new OpcodePush(subprogramObject.SubprogramName));
                        AddOpcode(new OpcodePush(null)); // The output filename - only used for compile-to-file rather than for running.
                        AddOpcode(new OpcodeCall("load()"));
                        // store the address of the program in the pointer variable
                        // (the load() function pushes the address onto the stack)
                        AddOpcode(new OpcodeStore());
                        // call the program
                        Opcode callOpcode = AddOpcode(new OpcodeCall(subprogramObject.PointerIdentifier));
                        // set the call opcode as the destination of the previous branch
                        branchOpcode.DestinationLabel = callOpcode.Label;
                        // return to the caller address
                        AddOpcode(new OpcodeReturn());
                        // set the function start label
                        subprogramObject.FunctionLabel = functionStart.Label;

                        // Initialization code
                        _currentCodeSection = subprogramObject.InitializationCode;
                        // initialize the pointer to zero
                        AddOpcode(new OpcodePush(subprogramObject.PointerIdentifier));
                        AddOpcode(new OpcodePush(0));
                        AddOpcode(new OpcodeStore());
                    }
                }
            }
        }

        private void PushProgramParameters()
        {
            // reverse the order of parameters so the stack
            // is popped in the correct order
            _programParameters.Reverse();
            foreach (ParseNode node in _programParameters)
            {
                VisitNode(node);
                AddOpcode(new OpcodeSwap());
                AddOpcode(new OpcodeStore());
            }
        }

        private void PushTriggerRemoveName(string newLabel)
        {
            _triggerRemoveNames.Add(newLabel);
            _nowCompilingTrigger = true;
        }

        private string PeekTriggerRemoveName()
        {
            if( _nowCompilingTrigger)
                return _triggerRemoveNames[_triggerRemoveNames.Count - 1];
            else
                return "";
        }

        private string PopTriggerRemoveName()
        {
            // Will throw exception if list is empty, but that "should
            // never happen" as pushes and pops should be balanced in
            // the compiler's code.  If it throws exception we want to
            // let the exception happen to highlight the bug:
            string returnVal = _triggerRemoveNames[_triggerRemoveNames.Count - 1];
            _triggerRemoveNames.RemoveAt(_triggerRemoveNames.Count - 1);
            _nowCompilingTrigger = (_triggerRemoveNames.Count > 0);
            return returnVal;
        }

        private void PushBreakList()
        {
            List<Opcode> list = new List<Opcode>();
            _breakList.Add(list);
        }

        private void AddToBreakList(Opcode opcode)
        {
            if (_breakList.Count > 0)
            {
                List<Opcode> list = _breakList[_breakList.Count - 1];
                list.Add(opcode);
            }
        }

        private void PopBreakList(string label)
        {
            if (_breakList.Count > 0)
            {
                List<Opcode> list = _breakList[_breakList.Count - 1];
                if (list != null)
                {
                    _breakList.Remove(list);
                    foreach (Opcode opcode in list)
                    {
                        opcode.DestinationLabel = label;
                    }
                }
            }
        }

        private void VisitNode(ParseNode node)
        {
            _lastNode = node;

            SetLineNum(node);

            switch (node.Token.Type)
            {
                case TokenType.Start:
                case TokenType.instruction_block:
                case TokenType.instruction:
                    VisitChildNodes(node);
                    break;
                case TokenType.set_stmt:
                    VisitSetStatement(node);
                    break;
                case TokenType.if_stmt:
                    VisitIfStatement(node);
                    break;
                case TokenType.until_stmt:
                    VisitUntilStatement(node);
                    break;
                case TokenType.lock_stmt:
                    VisitLockStatement(node);
                    break;
                case TokenType.unlock_stmt:
                    VisitUnlockStatement(node);
                    break;
                case TokenType.print_stmt:
                    VisitPrintStatement(node);
                    break;
                case TokenType.on_stmt:
                    VisitOnStatement(node);
                    break;
                case TokenType.toggle_stmt:
                    VisitToggleStatement(node);
                    break;
                case TokenType.wait_stmt:
                    VisitWaitStatement(node);
                    break;
                case TokenType.when_stmt:
                    VisitWhenStatement(node);
                    break;
                case TokenType.onoff_stmt:
                    VisitOnOffStatement(node);
                    break;
                case TokenType.stage_stmt:
                    VisitStageStatement(node);
                    break;
                case TokenType.clear_stmt:
                    VisitClearStatement(node);
                    break;
                case TokenType.add_stmt:
                    VisitAddStatement(node);
                    break;
                case TokenType.remove_stmt:
                    VisitRemoveStatement(node);
                    break;
                case TokenType.log_stmt:
                    VisitLogStatement(node);
                    break;
                case TokenType.break_stmt:
                    VisitBreakStatement(node);
                    break;
                case TokenType.preserve_stmt:
                    VisitPreserveStatement(node);
                    break;
                case TokenType.declare_stmt:
                    VisitDeclareStatement(node);
                    break;
                case TokenType.switch_stmt:
                    VisitSwitchStatement(node);
                    break;
                case TokenType.copy_stmt:
                    VisitCopyStatement(node);
                    break;
                case TokenType.rename_stmt:
                    VisitRenameStatement(node);
                    break;
                case TokenType.delete_stmt:
                    VisitDeleteStatement(node);
                    break;
                case TokenType.edit_stmt:
                    VisitEditStatement(node);
                    break;
                case TokenType.run_stmt:
                    VisitRunStatement(node);
                    break;
                case TokenType.compile_stmt:
                    VisitCompileStatement(node);
                    break;
                case TokenType.list_stmt:
                    VisitListStatement(node);
                    break;
                case TokenType.reboot_stmt:
                    VisitRebootStatement(node);
                    break;
                case TokenType.shutdown_stmt:
                    VisitShutdownStatement(node);
                    break;
                case TokenType.for_stmt:
                    VisitForStatement(node);
                    break;
                case TokenType.unset_stmt:
                    VisitUnsetStatement(node);
                    break;
                case TokenType.batch_stmt:
                    VisitBatchStatement(node);
                    break;
                case TokenType.deploy_stmt:
                    VisitDeployStatement(node);
                    break;
                case TokenType.filevol_name:
                    VisitFileVol(node);
                    break;
                case TokenType.arglist:
                    VisitArgList(node);
                    break;
                case TokenType.expr:
                case TokenType.compare_expr: // for issue #20
                case TokenType.and_expr:
                case TokenType.arith_expr:
                case TokenType.multdiv_expr:
                case TokenType.factor:
                    VisitExpression(node);
                    break;
                case TokenType.suffix:
                    VisitSuffix(node);
                    break;
                case TokenType.atom:
                    VisitAtom(node);
                    break;
                case TokenType.sci_number:
                    VisitSciNumber(node);
                    break;
                case TokenType.number:
                    VisitNumber(node);
                    break;
                case TokenType.INTEGER:
                    VisitInteger(node);
                    break;
                case TokenType.DOUBLE:
                    VisitDouble(node);
                    break;
                case TokenType.PLUSMINUS:
                    VisitPlusMinus(node);
                    break;
                case TokenType.MULT:
                    VisitMult(node);
                    break;
                case TokenType.DIV:
                    VisitDiv(node);
                    break;
                case TokenType.POWER:
                    VisitPower(node);
                    break;
                case TokenType.varidentifier:
                    VisitVarIdentifier(node);
                    break;
                case TokenType.array:
                    VisitArray(node);
                    break;
                case TokenType.function:
                    VisitFunction(node);
                    break;
                case TokenType.IDENTIFIER:
                    VisitIdentifier(node);
                    break;
                case TokenType.STRING:
                    VisitString(node);
                    break;
                case TokenType.TRUEFALSE:
                    VisitTrueFalse(node);
                    break;
                case TokenType.COMPARATOR:
                    VisitComparator(node);
                    break;
                case TokenType.AND:
                    VisitAnd(node);
                    break;
                case TokenType.OR:
                    VisitOr(node);
                    break;
                case TokenType.identifier_led_stmt:
                    VisitIdentifierLedStatement(node);
                    break;
                case TokenType.identifier_led_expr:
                    VisitIdentifierLedExpression(node);
                    break;
                default:
                    break;
            }
        }

        private void VisitChildNodes(ParseNode node)
        {
            SetLineNum(node);
            foreach (ParseNode childNode in node.Nodes)
            {
                VisitNode(childNode);
            }
        }

        private void VisitVariableNode(ParseNode node)
        {
            SetLineNum(node);
            _identifierIsVariable = true;
            VisitNode(node);
            _identifierIsVariable = false;
        }

        private void VisitExpression(ParseNode node)
        {
            SetLineNum(node);
            if (node.Nodes.Count > 1)
            {
                // it should always be odd, two arguments and one operator
                if ((node.Nodes.Count % 2) == 1)
                {
                    VisitNode(node.Nodes[0]);

                    int nodeIndex = 2;
                    while (nodeIndex < node.Nodes.Count)
                    {
                        VisitNode(node.Nodes[nodeIndex]);
                        nodeIndex -= 1;
                        VisitNode(node.Nodes[nodeIndex]);
                        nodeIndex += 3;
                    }
                }
            }
            else
            {
                if (node.Nodes.Count == 1)
                {
                    VisitNode(node.Nodes[0]);
                }
            }
        }

        private void VisitAtom(ParseNode node)
        {
            SetLineNum(node);
            if (node.Nodes.Count > 0)
            {
                bool addNegation = false;
                bool addNot = false;
                int nodeIndex = 0;

                if (node.Nodes[0].Token.Type == TokenType.PLUSMINUS)
                {
                    nodeIndex++;
                    if (node.Nodes[0].Token.Text == "-")
                    {
                        addNegation = true;
                    }
                }
                else if (node.Nodes[0].Token.Type == TokenType.NOT)
                {
                    nodeIndex++;
                    addNot = true;
                }
                
                if (node.Nodes[nodeIndex].Token.Type == TokenType.BRACKETOPEN)
                {
                    VisitNode(node.Nodes[nodeIndex + 1]);
                }
                else
                {
                    VisitNode(node.Nodes[nodeIndex]);
                }

                if (addNegation)
                {
                    AddOpcode(new OpcodeMathNegate());
                }
                if (addNot)
                {
                    AddOpcode(new OpcodeLogicNot());
                }
            }
        }

        private void VisitSciNumber(ParseNode node)
        {
            SetLineNum(node);
            if (node.Nodes.Count == 1)
            {
                VisitNumber(node.Nodes[0]);
            }
            else
            {
                //number in scientific notation
                int exponentIndex = 2;
                int exponentSign = 1;
                
                double mantissa = double.Parse(node.Nodes[0].Nodes[0].Token.Text);

                if (node.Nodes[2].Token.Type == TokenType.PLUSMINUS)
                {
                    exponentIndex++;
                    exponentSign = (node.Nodes[2].Token.Text == "-") ? -1 : 1;
                }

                int exponent = exponentSign * int.Parse(node.Nodes[exponentIndex].Token.Text);
                double number = mantissa * Math.Pow(10, exponent);
                AddOpcode(new OpcodePush(number));
            }
        }

        private void VisitNumber(ParseNode node)
        {
            SetLineNum(node);
            VisitNode(node.Nodes[0]);
        }

        private void VisitInteger(ParseNode node)
        {
            SetLineNum(node);
            object number = null;
            int integerNumber;

            if (int.TryParse(node.Token.Text, out integerNumber))
            {
                number = integerNumber;
            }
            else
            {
                number = double.Parse(node.Token.Text);
            }

            if (number != null)
            {
                AddOpcode(new OpcodePush(number));
            }
        }

        private void VisitDouble(ParseNode node)
        {
            SetLineNum(node);
            object number = null;
            number = double.Parse(node.Token.Text);

            if (number != null)
            {
                AddOpcode(new OpcodePush(number));
            }
        }

        private void VisitTrueFalse(ParseNode node)
        {
            SetLineNum(node);
            bool boolValue;
            if (bool.TryParse(node.Token.Text, out boolValue))
            {
                AddOpcode(new OpcodePush(boolValue));
            }
        }

        /// <summary>
        /// Do the work for function calls.
        /// </summary>
        /// <param name="node">parse node for the function term of the parse tree.</param>
        /// <param name="isDirect">true if it should make an OpcodeCall that is Direct, false if it should make an indirect one.
        /// See the documentation for OpcodeCall.Direct for the full explanation of the difference.  If isDirect is true, then
        /// the name to the left of the parenthesss will be the name of the function call.  If isDirect is false, then it will
        /// ignore the name to the left of the parenetheses and presume the function name, delegate, or branch index was
        /// already placed atop the stack by other parts of this compiler.</param>
        private void VisitActualFunction(ParseNode node, bool isDirect)
        {
            SetLineNum(node);
            
            int parameterCount = 0;
            ParseNode trailerNode = node.Nodes[1]; // the function_trailer rule is here.
            
            if (! isDirect)
            {
                // Need to tell OpcodeCall where in the stack the bottom of the arg list is.
                // Even if there are no arguments, it still has to be TOLD that by showing
                // it the marker atop the stack with nothing above it.
                AddOpcode(new OpcodePush(OpcodeCall.ARG_MARKER_STRING));
            }

            if (trailerNode.Nodes[1].Token.Type == TokenType.arglist)
            {
                
                parameterCount = (trailerNode.Nodes[1].Nodes.Count / 2) + 1;
                
                bool remember = _identifierIsSuffix;
                _identifierIsSuffix = false;
                
                VisitNode(trailerNode.Nodes[1]);

                _identifierIsSuffix = remember;
            }
            
            if (isDirect)
            {            
                string functionName = GetIdentifierText(node);

                string overloadedFunctionName = GetFunctionOverload(functionName, parameterCount) + "()";
                AddOpcode(new OpcodeCall(overloadedFunctionName));
            }
            else
            {
                OpcodeCall op = new OpcodeCall("");
                op.Direct = false;
                AddOpcode(op);
            }
        }

        private string GetFunctionOverload(string functionName, int parameterCount)
        {
            string functionKey = string.Format("{0}|{1}", functionName, parameterCount);
            if (_functionsOverloads.ContainsKey(functionKey))
            {
                return _functionsOverloads[functionKey];
            }
            else
            {
                return functionName;
            }
        }

        private void VisitFileVol(ParseNode node)
        {
            SetLineNum(node);
            VisitNode(node.Nodes[0]);
        }

        private void VisitArgList(ParseNode node)
        {
            SetLineNum(node);
            int nodeIndex = 0;
            while (nodeIndex < node.Nodes.Count)
            {
                VisitNode(node.Nodes[nodeIndex]);
                nodeIndex += 2;
            }
        }

        private void VisitVarIdentifier(ParseNode node)
        {
            SetLineNum(node);
            
            // I might be called on a raw IDENTIFIER, in which case I have no
            // child nodes to descend into.  But if I *do* have a child node
            // to descend into, then do so:
            if (node.Nodes.Count == 0)
                VisitNode(node);
            else
                VisitNode(node.Nodes[0]);
        }
        
        private void VisitSuffix(ParseNode node)
        {
            SetLineNum(node);
            
            if (node.Token.Type == TokenType.identifier_led_expr) // have to fake it because we don't have the full parse tree under the ident node.
                _identifierIsVariable = true;
            VisitNode(node.Nodes[0]);
            if (node.Token.Type == TokenType.identifier_led_expr) // have to fake it because we don't have the full parse tree under the ident node.
                _identifierIsVariable = false;

            if (node.Nodes.Count > 1)
            {
                int nodeIndex = 1;
                while (nodeIndex < node.Nodes.Count)
                {
                    bool remember = _identifierIsSuffix;
                    _identifierIsSuffix = true;
                    ParseNode subTerm = node.Nodes[nodeIndex];
                    ParseNode subTermAfterColon = node.Nodes[nodeIndex].Nodes[1];

                    bool isFunc = IsActualFunctionCall(subTerm);
                    bool isArray = IsActualArrayIndexing(subTerm);
                    Console.WriteLine("term at index " +nodeIndex+" ("+subTerm.Text+") : isFunc="+isFunc+", isArray="+isArray);
                    if (isFunc || isArray)
                    {
                        // Don't parse it as a function/array call yet.  Just visit its leftmost
                        // term (the string name of the suffix), so that GetMember can do
                        // it's work and retrieve the value of the suffix, then we'll go back and
                        // parse the function/array:
                        ParseNode identNode = DepthFirstLeftSearch(subTerm, TokenType.IDENTIFIER);

                        bool rememberIsV = _identifierIsVariable;
                        _identifierIsVariable = false;
                        Console.WriteLine("term text = " +identNode.Token.Text);

                        VisitNode(identNode);

                        _identifierIsVariable = rememberIsV;
                    }
                    else
                    {
                        Console.WriteLine("term text = " +subTerm.Token.Text);
                        VisitNode(subTermAfterColon);
                    }

                    _identifierIsSuffix = remember;

                    // when we are setting a member value we need to leave
                    // the last object and the last suffix in the stack
                    bool usingSetMember = (!isFunc && !isArray) && (_compilingSetDestination && nodeIndex == (node.Nodes.Count -1));
                    if (! usingSetMember)
                    {
                        AddOpcode(isFunc ? new OpcodeGetMethod() : new OpcodeGetMember());
                    }
                    if (isFunc)
                    {
                        ParseNode funcNode = DepthFirstLeftSearch(subTermAfterColon, TokenType.function);
                        VisitActualFunction(funcNode, false);
                    }
                    if (isArray)
                    {
                        ParseNode arrayNode = DepthFirstLeftSearch(subTermAfterColon, TokenType.array);
                        VisitActualArray(arrayNode);
                    }

                    nodeIndex += 1;
                }
            }
        }

        /// <summary>
        /// Do the work for array index references.  It assumes the array object has already
        /// been pushed on top of the stack so there's no reason to read that from the
        /// node's children.  It just reads the indexing part.
        /// </summary>
        /// <param name="node">parse node for the array suffix of the parse tree.</param>
        private void VisitActualArray(ParseNode node)
        {
            ParseNode trailerNode = node.Nodes[1]; // should be type array_trailer.

            int nodeIndex = 1;
            while (nodeIndex < trailerNode.Nodes.Count)
            {
                // Skip two tokens instead of one bewteen dimensions if using the "[]" syntax:
                if (trailerNode.Nodes[nodeIndex].Token.Type == TokenType.SQUAREOPEN){
                    ++nodeIndex;
                }
                
                bool remember = _identifierIsSuffix;
                _identifierIsSuffix = false;
                
                VisitNode(trailerNode.Nodes[nodeIndex]);
                
                _identifierIsSuffix = remember;

                // Two ways to check if this is the last index (i.e. the 'k' in arr[i][j][k]'),
                // depeding on whether using the "#" syntax or the "[..]" syntax:
                bool isLastIndex = false;
                var previousNodeType = trailerNode.Nodes[nodeIndex - 1].Token.Type;
                switch (previousNodeType)
                {
                    case TokenType.ARRAYINDEX:
                        isLastIndex = (nodeIndex == trailerNode.Nodes.Count-1);
                        break;
                    case TokenType.SQUAREOPEN:
                        isLastIndex = (nodeIndex == trailerNode.Nodes.Count-2);
                        break;
                }

                // when we are setting a member value we need to leave
                // the last object and the last index in the stack
                // the only exception is when we are setting a suffix of the indexed value
                var hasSuffix = VarIdentifierPreceedsSuffix(node.Parent);
                if (!(_compilingSetDestination && isLastIndex) || hasSuffix)
                {
                    AddOpcode(new OpcodeGetIndex());
                }

                nodeIndex += 2;
            }
            
        }
        
        private void VisitArray(ParseNode node)
        {
            SetLineNum(node);
            _identifierIsVariable = true;
            VisitNode(node.Nodes[0]);
            _identifierIsVariable = false;

            if (node.Nodes.Count > 1)
            {
                VisitActualArray(node);
            }
        }

        private string GetIdentifierText(ParseNode node)
        {
            if (node.Token.Type == TokenType.IDENTIFIER)
            {
                return node.Token.Text;
            }
            else
            {
                foreach (ParseNode child in node.Nodes)
                {
                    string identifier = GetIdentifierText(child);
                    if (identifier != string.Empty)
                        return identifier;
                }
            }

            return string.Empty;
        }
                
        /// <summary>
        /// The Function parse node contains both actual function calls
        /// (with parentheses) and just plain vanilla terms.  This
        /// determines if it's REALLY a function call or not.
        /// </summary>
        /// <param name="node"></param>
        /// <returns>true if it's really a function call.  False otherwise.</returns>
        private bool IsActualFunctionCall(ParseNode node)
        {
            // This can be called at the level of the parent of the function node, so get down to it first:
            ParseNode child = node;
            while (child != null && 
                   (child.Token.Type != TokenType.function &&
                    child.Token.Type != TokenType.identifier_led_expr ))
            {
                if (child.Nodes.Count>1)
                    child = child.Nodes[1];
                else
                    child = null;
            }
            if (child == null)
                return false;

            // If it has the optional function_trailer node tacked on to it, then it's really a function call with
            // parentheses, not just using the function node as a dummy wrapper around a plain array node.
            return child.Nodes.Count > 1;
        }

        /// <summary>
        /// The Array parse node contains both actual Array calls
        /// (with index given) and just plain vanilla terms.  This
        /// determines if it's REALLY an array index call or not.
        /// </summary>
        /// <param name="node"></param>
        /// <returns>true if it's really an array index referece call.  False otherwise.</returns>
        private bool IsActualArrayIndexing(ParseNode node)
        {
            // This can be called at the level of the parent of the array node, so get down to it first:
            ParseNode child = node;
            while (child != null && 
                   (child.Token.Type != TokenType.array &&
                    child.Token.Type != TokenType.identifier_led_expr ))
            {
                if (child.Nodes.Count>1)
                    child = child.Nodes[1];
                else
                    child = null;
            }
            if (child == null)
                return false;
            
            // If it has the optional array_trailer node tacked on to it, then it's really an array index, not just using array as
            // a dummy wrapper around a plain atom node
            return child.Nodes.Count > 1;
        }

        private void VisitFunction(ParseNode node)
        {
            SetLineNum(node);
            
            if (node.Nodes.Count > 1 &&
                node.Nodes[1].Token.Type == TokenType.function_trailer)
            {
                // if a bracket follows an identifier then its a function call
                VisitActualFunction(node,true);
            }
            else
            {
                VisitNode(node.Nodes[0]); // I'm not really a function call after all - just a wrapper around another node type.
            }
        }

        private void VisitIdentifier(ParseNode node)
        {
            SetLineNum(node);
            bool isVariable = (_identifierIsVariable && !_identifierIsSuffix);
            string prefix = isVariable ? "$" : String.Empty;
            string identifier = GetIdentifierText(node);
            if (isVariable && _context.Locks.Contains(identifier))
            {
                Lock lockObject = _context.Locks.GetLock(identifier);
                if (_compilingSetDestination)
                {
                    UnlockIdentifier(lockObject);
                    AddOpcode(new OpcodePush("$" + identifier));
                }
                else
                {
                    AddOpcode(new OpcodeCall(lockObject.PointerIdentifier));
                }
            }
            else                
            {
                AddOpcode(new OpcodePush(prefix + identifier));
            }
        }

        private void VisitString(ParseNode node)
        {
            SetLineNum(node);
            AddOpcode(new OpcodePush(node.Token.Text.Trim('"')));
        }

        /// <summary>
        /// Check for if the var_identifer node has a suffix term as its very next neighor
        /// to the right.  i.e. in the following syntax:<br/>
        ///     AAA:BBB:CCC[0]:DDD:EEE[0]<br/>
        /// This method should return true if called on the AAA, BBB, or DDD nodes,
        /// but not when called on the CCC or EEE nodes.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private bool VarIdentifierPreceedsSuffix(ParseNode node)
        {
            // If it's a var_identifier being worked on, drop down one level first
            // to get into the actual meat of the syntax tree it represents:
            if (node.Token.Type == TokenType.varidentifier)
                return VarIdentifierPreceedsSuffix(node.Nodes.First());

            if (node.Token.Type == TokenType.suffix_trailer ||
                node.Nodes.Count>1 && node.Nodes[1].Token.Type == TokenType.suffix_trailer)
            {
                return true;
            }
            // Descend into child nodes to try them but don't parse too far over to the right, just the immediate neighbor only.
            if (node.Nodes.Count > 1)
            {
                ParseNode child = node.Nodes[0];
                if (VarIdentifierPreceedsSuffix(child))
                    return true;
            }
            return false;
        }
        
        ///<summary>
        /// Check for if the rightmost thing in the var_identifier node
        /// is a suffix term.  i.e. return true if the var_identifier is:<br/>
        ///    AAA:BBB, or<br/>
        ///    AAA[0]:BBB,<br/>
        /// but NOT if it's this:<br/>
        ///    AAA:BBB[0].<br/>
        /// (Which does *contain* a suffix, but not as the rightmost part of it.  The rightmost
        /// part of it is the array indexer "[0]".)
        /// </summary>
        private bool VarIdentifierEndsWithSuffix(ParseNode node)
        {
            // If it's a var_identifier being worked on, drop down one level first
            // to get into the actual meat of the syntax tree it represents:
            if (node.Token.Type == TokenType.varidentifier)
                return VarIdentifierEndsWithSuffix(node.Nodes.First());
            
            // Descend the rightmost children until encountering the deepest node that is
            // still a suffix_trailer, array_trailer, or function_trailer.  If that node
            // was a suffix_trailer, return true, else it's false.
            ParseNode prevChild = node;
            ParseNode thisChild = node.Nodes.Last();
            while (thisChild.Token.Type == TokenType.suffix_trailer ||
                   thisChild.Token.Type == TokenType.array_trailer ||
                   thisChild.Token.Type == TokenType.function_trailer)
            {
                prevChild = thisChild;
                thisChild = thisChild.Nodes.Last();
            }
            return prevChild.Token.Type == TokenType.suffix_trailer;
        }
        
        /// <summary>
        /// Check for if the var_identifer node has an array index as its very next neighbor
        /// to the right.  i.e. in the following syntax:<br/>
        ///     AAA:BBB:CCC[0]:DDD:EEE[0]<br/>
        /// This method should return true if called on the CCC node or the EEE node,
        /// but not when called on the AAA, BBB, or DDD nodes.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private bool VarIdentifierPreceedsIndex(ParseNode node)
        {
            // If it's a var_identifier being worked on, drop down one level first
            // to get into the actual meat of the syntax tree it represents:
            if (node.Token.Type == TokenType.varidentifier)
                return VarIdentifierPreceedsIndex(node.Nodes.First());

            if (node.Token.Type == TokenType.array_trailer ||
                node.Nodes.Count>1 && node.Nodes[1].Token.Type == TokenType.array_trailer)
            {
                return true;
            }
            // Descend into child nodes to try them but don't parse too far over to the right, just the immediate neighbor only.
            if (node.Nodes.Count > 1)
            {
                ParseNode child = node.Nodes[0];
                if (VarIdentifierPreceedsIndex(child))
                    return true;
            }                    
            return false;
        }

        ///<summary>
        /// Check for if the rightmost thing in the var_identifier node
        /// is an array indexer.  i.e. return true if the var_identifier is:<br/>
        ///    AAA:BBB[0], or<br/>
        ///    AAA[0],<br/>
        /// but NOT if it's this:<br/>
        ///    AAA[0]:BBB<br/>
        /// (Which does *contain* an array indexer, but not as the rightmost part of it.  The rightmost
        /// part of it is the suffix term ":BBB".)
        /// </summary>
        private bool VarIdentifierEndsWithIndex(ParseNode node)
        {
            // If it's a var_identifier being worked on, drop down one level first
            // to get into the actual meat of the syntax tree it represents:
            if (node.Token.Type == TokenType.varidentifier)
                return VarIdentifierEndsWithIndex(node.Nodes.First());

            // Descend the rightmost children until encountering the deepest node that is
            // still a suffix_trailer, array_trailer, or function_trailer.  If that node
            // was an array_trailer, return true, else it's false.
            ParseNode prevChild = node;
            ParseNode thisChild = node.Nodes.Last();
            while (thisChild.Token.Type == TokenType.suffix_trailer ||
                   thisChild.Token.Type == TokenType.array_trailer ||
                   thisChild.Token.Type == TokenType.function_trailer)
            {
                prevChild = thisChild;
                thisChild = thisChild.Nodes.Last();
            }
            return prevChild.Token.Type == TokenType.array_trailer;
        }
        
        /// <summary>
        /// Perform a depth-first leftmost search of the parse tree from the starting
        /// point given to find the first occurrance of a node of the given token type.<br/>
        /// This is intended as a way to make code that might be a bit more robustly able
        /// to handle shifts and adjustments to the parse grammar in the TinyPG file.<br/>
        /// Instead of assuming "I know that array nodes are always one level underneath
        /// function nodes", it instead lets you say "Get the array node version of this
        /// node, no matter how many levels down it may be."<br/>
        /// This is needed because TinyPG's LL(1) limitations made it so we had to define
        /// things like "we're going to call this node a 'function' even though it might
        /// not actually be because the "(..)" part of the syntax is optional.  In reality
        /// it's *potentially* a function, or maybe actually an array, or an identifier.<br/>
        /// This method is intended to let you descend however far down is required to get
        /// the node in the sort of context you're looking for.
        /// </summary>
        /// <param name="node">start the search from this point in the parse tree</param>
        /// <param name="tokType">look for this kind of node</param>
        /// <returns>the found node, or null if no such node found.</returns>
        private ParseNode DepthFirstLeftSearch( ParseNode node, TokenType tokType)
        {
            if (node.Token.Type == tokType)
            {
                return node;
            }
            else
            {
                foreach (ParseNode child in node.Nodes)
                {
                    ParseNode hit = DepthFirstLeftSearch(child, tokType);
                    if (hit != null)
                        return hit;
                }
            }
            
            return null; // not found.
        }

        private void VisitSetStatement(ParseNode node)
        {
            SetLineNum(node);
            // destination
            _compilingSetDestination = true;
            VisitVarIdentifier(node.Nodes[1]);
            _compilingSetDestination = false;
            // expression
            VisitNode(node.Nodes[3]);

            if (VarIdentifierEndsWithSuffix(node.Nodes[1]))
            {
                AddOpcode(new OpcodeSetMember());
            }
            else if (VarIdentifierEndsWithIndex(node.Nodes[1]))
            {
                AddOpcode(new OpcodeSetIndex());
            }
            else
            {
                AddOpcode(new OpcodeStore());
            }
        }

        private void VisitIfStatement(ParseNode node)
        {
            SetLineNum(node);
            // The IF check:
            VisitNode(node.Nodes[1]);
            Opcode branchToFalse = AddOpcode(new OpcodeBranchIfFalse());
            // The IF BODY:
            VisitNode(node.Nodes[2]);
            if (node.Nodes.Count < 4) {
                // No ELSE exists.
                // Jump to after the IF BODY if false:
                branchToFalse.DestinationLabel = GetNextLabel(false);
                _addBranchDestination = true;
            } else {
                // The IF statement has an ELSE clause.

                // Jump past the ELSE body from the end of the IF body:
                Opcode branchPastElse = AddOpcode( new OpcodeBranchJump() );
                // This is where the ELSE clause starts:
                branchToFalse.DestinationLabel = GetNextLabel(false);
                // The else body:
                VisitNode(node.Nodes[4]);
                // End of Else body label:
                branchPastElse.DestinationLabel = GetNextLabel(false);
                _addBranchDestination = true;
            }
        }

        private void VisitUntilStatement(ParseNode node)
        {
            SetLineNum(node);

            bool remember = _nowInALoop;
            _nowInALoop = true;
            
            string conditionLabel = GetNextLabel(false);
            PushBreakList();
            VisitNode(node.Nodes[1]);
            AddOpcode(new OpcodeLogicNot());
            Opcode branch = AddOpcode(new OpcodeBranchIfFalse());
            AddToBreakList(branch);
            VisitNode(node.Nodes[2]);
            Opcode jump = AddOpcode(new OpcodeBranchJump());
            jump.DestinationLabel = conditionLabel;
            PopBreakList(GetNextLabel(false));
            _addBranchDestination = true;

            _nowInALoop = remember;
        }

        private void VisitPlusMinus(ParseNode node)
        {
            SetLineNum(node);
            if (node.Token.Text == "+")
            {
                AddOpcode(new OpcodeMathAdd());
            }
            else if (node.Token.Text == "-")
            {
                AddOpcode(new OpcodeMathSubtract());
            }
        }

        private void VisitMult(ParseNode node)
        {
            SetLineNum(node);
            AddOpcode(new OpcodeMathMultiply());
        }

        private void VisitDiv(ParseNode node)
        {
            SetLineNum(node);
            AddOpcode(new OpcodeMathDivide());
        }

        private void VisitPower(ParseNode node)
        {
            SetLineNum(node);
            AddOpcode(new OpcodeMathPower());
        }

        private void VisitAnd(ParseNode node)
        {
            SetLineNum(node);
            AddOpcode(new OpcodeLogicAnd());
        }

        private void VisitOr(ParseNode node)
        {
            SetLineNum(node);
            AddOpcode(new OpcodeLogicOr());
        }

        private void VisitComparator(ParseNode node)
        {
            SetLineNum(node);
            switch (node.Token.Text)
            {
                case ">":
                    AddOpcode(new OpcodeCompareGT());
                    break;
                case "<":
                    AddOpcode(new OpcodeCompareLT());
                    break;
                case ">=":
                    AddOpcode(new OpcodeCompareGTE());
                    break;
                case "<=":
                    AddOpcode(new OpcodeCompareLTE());
                    break;
                case "<>":
                    AddOpcode(new OpcodeCompareNE());
                    break;
                case "=":
                    AddOpcode(new OpcodeCompareEqual());
                    break;
                default:
                    break;
            }
        }

        private void VisitLockStatement(ParseNode node)
        {
            SetLineNum(node);
            string lockIdentifier = node.Nodes[1].Token.Text;
            int expressionHash = ConcatenateNodes(node.Nodes[3]).GetHashCode();
            Lock lockObject = _context.Locks.GetLock(lockIdentifier);

            if (lockObject.IsInitialized())
            {
                string functionLabel = lockObject.GetLockFunction(expressionHash)[0].Label;
                // lock variable
                AddOpcode(new OpcodePush(lockObject.PointerIdentifier));
                AddOpcode(new OpcodePushRelocateLater(null),functionLabel);
                AddOpcode(new OpcodeStore());

                if (lockObject.IsSystemLock())
                {
                    // add update trigger
                    string triggerIdentifier = "lock-" + lockIdentifier;
                    if (_context.Triggers.Contains(triggerIdentifier))
                    {
                        Trigger triggerObject = _context.Triggers.GetTrigger(triggerIdentifier);
                        AddOpcode(new OpcodePushRelocateLater(null),triggerObject.GetFunctionLabel());
                        AddOpcode(new OpcodeAddTrigger(false));
                    }
                    
                    // enable this FlyByWire parameter
                    AddOpcode(new OpcodePush(lockIdentifier));
                    AddOpcode(new OpcodePush(true));
                    AddOpcode(new OpcodeCall("toggleflybywire()"));
                }
            }
        }

        private void VisitUnlockStatement(ParseNode node)
        {
            SetLineNum(node);
            if (node.Nodes[1].Token.Type == TokenType.ALL)
            {
                // unlock all locks
                foreach(Lock lockObject in _context.Locks.GetLockList())
                    UnlockIdentifier(lockObject);
            }
            else
            {
                string lockIdentifier = node.Nodes[1].Token.Text;
                Lock lockObject = _context.Locks.GetLock(lockIdentifier);
                UnlockIdentifier(lockObject);
            }
        }

        private void UnlockIdentifier(Lock lockObject)
        {
            if (lockObject.IsInitialized())
            {
                if (lockObject.IsSystemLock())
                {
                    // disable this FlyByWire parameter
                    AddOpcode(new OpcodePush(lockObject.Identifier));
                    AddOpcode(new OpcodePush(false));
                    AddOpcode(new OpcodeCall("toggleflybywire()"));

                    // remove update trigger
                    string triggerIdentifier = "lock-" + lockObject.Identifier;
                    if (_context.Triggers.Contains(triggerIdentifier))
                    {
                        Trigger triggerObject = _context.Triggers.GetTrigger(triggerIdentifier);
                        AddOpcode(new OpcodePushRelocateLater(null),triggerObject.GetFunctionLabel());
                        AddOpcode(new OpcodeRemoveTrigger());
                    }
                }

                // unlock variable
                AddOpcode(new OpcodePush(lockObject.PointerIdentifier));
                AddOpcode(new OpcodePushRelocateLater(null),lockObject.DefaultLabel);
                AddOpcode(new OpcodeStore());
            }
        }

        private void VisitOnStatement(ParseNode node)
        {
            SetLineNum(node);
            int expressionHash = ConcatenateNodes(node).GetHashCode();
            string triggerIdentifier = "on-" + expressionHash.ToString();
            Trigger triggerObject = _context.Triggers.GetTrigger(triggerIdentifier);

            if (triggerObject.IsInitialized())
            {
                AddOpcode(new OpcodePush(triggerObject.VariableNameOldValue));
                AddOpcode(new OpcodePush(triggerObject.VariableName));
                AddOpcode(new OpcodeStore());
                AddOpcode(new OpcodePushRelocateLater(null),triggerObject.GetFunctionLabel());
                AddOpcode(new OpcodeAddTrigger(false));
            }
        }

        private void VisitWhenStatement(ParseNode node)
        {
            SetLineNum(node);
            int expressionHash = ConcatenateNodes(node).GetHashCode();
            string triggerIdentifier = "when-" + expressionHash.ToString();
            Trigger triggerObject = _context.Triggers.GetTrigger(triggerIdentifier);

            if (triggerObject.IsInitialized())
            {
                AddOpcode(new OpcodePushRelocateLater(null),triggerObject.GetFunctionLabel());
                AddOpcode(new OpcodeAddTrigger(false));
            }
        }

        private void VisitWaitStatement(ParseNode node)
        {
            SetLineNum(node);
            
            if (_nowCompilingTrigger)
                throw new KOSWaitInvalidHereException();
            
            if (node.Nodes.Count == 3)
            {
                // wait time
                VisitNode(node.Nodes[1]);
                AddOpcode(new OpcodeWait());
            }
            else
            {
                // wait condition
                int expressionHash = ConcatenateNodes(node).GetHashCode();
                string triggerIdentifier = "wait-" + expressionHash.ToString();
                Trigger triggerObject = _context.Triggers.GetTrigger(triggerIdentifier);

                if (triggerObject.IsInitialized())
                {
                    AddOpcode(new OpcodePushRelocateLater(null),triggerObject.GetFunctionLabel());
                    AddOpcode(new OpcodeAddTrigger(true));
                }
            }
        }

        private void VisitDeclareStatement(ParseNode node)
        {
            SetLineNum(node);
            if (node.Nodes.Count == 3)
            {
                // standard declare
                VisitNode(node.Nodes[1]);
                AddOpcode(new OpcodePush(0));
                AddOpcode(new OpcodeStore());
            }
        }

        private void VisitToggleStatement(ParseNode node)
        {
            SetLineNum(node);
            VisitVarIdentifier(node.Nodes[1]);
            VisitVarIdentifier(node.Nodes[1]);
            AddOpcode(new OpcodeLogicToBool());
            AddOpcode(new OpcodeLogicNot());
            AddOpcode(new OpcodeStore());
        }

        private void VisitOnOffStatement(ParseNode node)
        {
            SetLineNum(node);
            VisitVarIdentifier(node.Nodes[0]);
            
            // node.Nodes[1].Token.Type == TokenType.onoff_trailer at this point
                
            if (node.Nodes[1].Nodes[0].Token.Type == TokenType.ON)
                AddOpcode(new OpcodePush(true));
            else
                AddOpcode(new OpcodePush(false));
            AddOpcode(new OpcodeStore());
        }

        private void VisitPrintStatement(ParseNode node)
        {
            SetLineNum(node);
            if (node.Nodes.Count == 3)
            {
                VisitNode(node.Nodes[1]);
                AddOpcode(new OpcodeCall("print()"));
            }
            else
            {
                VisitNode(node.Nodes[1]);
                VisitNode(node.Nodes[4]);
                VisitNode(node.Nodes[6]);
                AddOpcode(new OpcodeCall("printat()"));
            }
        }

        private void VisitStageStatement(ParseNode node)
        {
            SetLineNum(node);
            AddOpcode(new OpcodeCall("stage()"));
        }

        private void VisitAddStatement(ParseNode node)
        {
            SetLineNum(node);
            VisitNode(node.Nodes[1]);
            AddOpcode(new OpcodeCall("add()"));
        }

        private void VisitRemoveStatement(ParseNode node)
        {
            SetLineNum(node);
            VisitNode(node.Nodes[1]);
            AddOpcode(new OpcodeCall("remove()"));
        }

        private void VisitClearStatement(ParseNode node)
        {
            SetLineNum(node);
            AddOpcode(new OpcodeCall("clearscreen()"));
        }

        private void VisitEditStatement(ParseNode node)
        {
            SetLineNum(node);
            string fileName = node.Nodes[1].Token.Text;
            AddOpcode(new OpcodePush(fileName) );
            AddOpcode(new OpcodeCall("edit()"));
        }

        private void VisitRunStatement(ParseNode node)
        {
            SetLineNum(node);
            int volumeIndex = 3;

            // process program arguments
            if (node.Nodes.Count > 3 && node.Nodes[3].Token.Type == TokenType.arglist)
            {
                VisitNode(node.Nodes[3]);
                volumeIndex += 3;
            }

            bool hasON = node.Nodes.Any(cn => cn.Token.Type == TokenType.ON);
            if (!hasON && _options.LoadProgramsInSameAddressSpace)
            {
                string subprogramName = node.Nodes[1].Token.Text;
                if (_context.Subprograms.Contains(subprogramName))
                {
                    Subprogram subprogramObject = _context.Subprograms.GetSubprogram(subprogramName);
                    AddOpcode(new OpcodeCall(null)).DestinationLabel = subprogramObject.FunctionLabel;
                }
            }
            else
            {
                // program name
                VisitNode(node.Nodes[1]);

                // volume where program should be executed (null means local)
                if (volumeIndex < node.Nodes.Count)
                    VisitNode(node.Nodes[volumeIndex]);
                else
                    AddOpcode(new OpcodePush(null));

                AddOpcode(new OpcodeCall("run()"));
            }
        }
        
        private void VisitCompileStatement(ParseNode node)
        {
            SetLineNum(node);
            string fileNameIn = node.Nodes[1].Token.Text;
            string fileNameOut = node.Nodes[3].Token.Text;
            AddOpcode(new OpcodePush(fileNameIn));
            AddOpcode(new OpcodePush(fileNameOut));
            AddOpcode(new OpcodeCall("load()"));
        }

        private void VisitSwitchStatement(ParseNode node)
        {
            SetLineNum(node);
            VisitNode(node.Nodes[2]);
            AddOpcode(new OpcodeCall("switch()"));
        }

        private void VisitCopyStatement(ParseNode node)
        {
            SetLineNum(node);
            VisitNode(node.Nodes[1]);
            
            if (node.Nodes[2].Token.Type == TokenType.FROM)
                AddOpcode(new OpcodePush("from"));
            else
                AddOpcode(new OpcodePush("to"));
            
            VisitNode(node.Nodes[3]);
            AddOpcode(new OpcodeCall("copy()"));
        }

        private void VisitRenameStatement(ParseNode node)
        {
            SetLineNum(node);
            int oldNameIndex = 2;
            int newNameIndex = 4;
            
            if (node.Nodes.Count == 5)
            {
                oldNameIndex--;
                newNameIndex--;
                AddOpcode(new OpcodePush("file"));
            }
            else
            {
                if (node.Nodes[1].Token.Type == TokenType.FILE)
                {
                    AddOpcode(new OpcodePush("file"));
                }
                else
                {
                    AddOpcode(new OpcodePush("volume"));
                }
            }

            VisitNode(node.Nodes[oldNameIndex]);
            VisitNode(node.Nodes[newNameIndex]);
            AddOpcode(new OpcodeCall("rename()"));
        }

        private void VisitDeleteStatement(ParseNode node)
        {
            SetLineNum(node);
            VisitNode(node.Nodes[1]);
            
            if (node.Nodes.Count == 5)
                VisitNode(node.Nodes[3]);
            else
                AddOpcode(new OpcodePush(null));

            AddOpcode(new OpcodeCall("delete()"));
        }

        private void VisitListStatement(ParseNode node)
        {
            SetLineNum(node);
            bool hasIdentifier = (node.Nodes[1].Token.Type == TokenType.IDENTIFIER);
            bool hasIn = hasIdentifier && (node.Nodes[2].Token.Type == TokenType.IN);

            if (hasIn)
            {
                // destination variable
                VisitVariableNode(node.Nodes[3]);
                // list type
                VisitNode(node.Nodes[1]);
                // build list
                AddOpcode(new OpcodeCall("buildlist()"));
                AddOpcode(new OpcodeStore());
            }
            else
            {
                // list type
                if (hasIdentifier) VisitNode(node.Nodes[1]);
                else AddOpcode(new OpcodePush("files"));
                // print list
                AddOpcode(new OpcodeCall("printlist()"));
            }
        }

        private void VisitLogStatement(ParseNode node)
        {
            SetLineNum(node);
            VisitNode(node.Nodes[1]);
            VisitNode(node.Nodes[3]);
            AddOpcode(new OpcodeCall("logfile()"));
        }

        private void VisitBreakStatement(ParseNode node)
        {
            SetLineNum(node);

            if (! _nowInALoop)
                throw new KOSBreakInvalidHereException();

            Opcode jump = AddOpcode(new OpcodeBranchJump());
            AddToBreakList(jump);
        }

        private void VisitPreserveStatement(ParseNode node)
        {
            SetLineNum(node);

            if (! _nowCompilingTrigger)
                throw new KOSPreserveInvalidHereException();

            string flagName = PeekTriggerRemoveName();
            AddOpcode(new OpcodePush(flagName));
            AddOpcode(new OpcodePush(false));
            AddOpcode(new OpcodeStore());
        }

        private void VisitRebootStatement(ParseNode node)
        {
            SetLineNum(node);
            AddOpcode(new OpcodeCall("reboot()"));
        }

        private void VisitShutdownStatement(ParseNode node)
        {
            SetLineNum(node);
            AddOpcode(new OpcodeCall("shutdown()"));
        }

        private void VisitForStatement(ParseNode node)
        {
            SetLineNum(node);

            bool remember = _nowInALoop;
            _nowInALoop = true;
            
            string iteratorIdentifier = "$" + GetIdentifierText(node.Nodes[3]) + "-iterator";

            PushBreakList();
            AddOpcode(new OpcodePush(iteratorIdentifier));
            VisitNode(node.Nodes[3]);
            AddOpcode(new OpcodePush("iterator"));
            AddOpcode(new OpcodeGetMember());
            AddOpcode(new OpcodeStore());
            // loop condition
            Opcode condition = AddOpcode(new OpcodePush(iteratorIdentifier));
            string conditionLabel = condition.Label;
            AddOpcode(new OpcodePush("next"));
            AddOpcode(new OpcodeGetMember());
            // branch
            Opcode branch = AddOpcode(new OpcodeBranchIfFalse());
            AddToBreakList(branch);
            // assign value to iteration variable
            VisitVariableNode(node.Nodes[1]);
            AddOpcode(new OpcodePush(iteratorIdentifier));
            AddOpcode(new OpcodePush("value"));
            AddOpcode(new OpcodeGetMember());
            AddOpcode(new OpcodeStore());
            // instructions in FOR body
            VisitNode(node.Nodes[4]);
            // jump to condition
            Opcode jump = AddOpcode(new OpcodeBranchJump());
            jump.DestinationLabel = conditionLabel;
            // end of loop, cleanup
            Opcode endLoop = AddOpcode(new OpcodePush(iteratorIdentifier));
            AddOpcode(new OpcodePush("reset"));
            AddOpcode(new OpcodeGetMember());
            AddOpcode(new OpcodePop()); // removes the "true" returned by the previous getmember
            // unset of iterator and iteration variable
            AddOpcode(new OpcodePush(iteratorIdentifier));
            AddOpcode(new OpcodeUnset());
            VisitVariableNode(node.Nodes[1]);
            AddOpcode(new OpcodeUnset());
            PopBreakList(endLoop.Label);

            _nowInALoop = remember;
        }

        private void VisitUnsetStatement(ParseNode node)
        {
            SetLineNum(node);
            if (node.Nodes[1].Token.Type == TokenType.ALL)
            {
                // null means all variables
                AddOpcode(new OpcodePush(null));
            }
            else
            {
                VisitVariableNode(node.Nodes[1]);
            }

            AddOpcode(new OpcodeUnset());
        }

        private void VisitBatchStatement(ParseNode node)
        {
            SetLineNum(node);
            throw new Exception("Batch mode can only be used when in immediate mode.");
        }

        private void VisitDeployStatement(ParseNode node)
        {
            SetLineNum(node);
            throw new Exception("Batch mode can only be used when in immediate mode.");
        }
        
        private void VisitIdentifierLedStatement(ParseNode node)
        {
            SetLineNum(node);

            // IdentifierLedStatement is an IdentifierLedExpression with the end marker "." tacked on.
            // Just parse the IdentifierLedExpression:
            VisitNode(node.Nodes[0]);
        }
        
        private void VisitIdentifierLedExpression(ParseNode node)
        {
            SetLineNum(node);
            bool needThrowawayPop = false;
            
            // Figure out which part of the parse tree this is *really* meant to be and parse accordingly, pretending
            // to be a node of that type of rule.
            // Do so by reading the second value to see which kind of thing the parser decided it was.
            switch (node.Nodes[1].Token.Type)
            {
                case TokenType.suffix_trailer:
                    VisitSuffix(node);
                    needThrowawayPop = true;
                    break;
                case TokenType.function_trailer:
                    VisitFunction(node);
                    needThrowawayPop = true;
                    break;
                case TokenType.array_trailer:
                    VisitArray(node);
                    needThrowawayPop = true;
                    break;
                    
                // The onoff statement is a wacky exception that lives here only
                // because Nivekk originally decided to make it a statement that
                // starts with an identifier even though it's not a user-land made up
                // statement but a built-in instead:
                case TokenType.onoff_trailer:
                    VisitOnOffStatement(node);
                    needThrowawayPop = false;
                    break;
            }
            
            // The expressions above (except for onoff statements) leave a return value on the stack as the result
            // of the expression, but in this context they're being used as raw statements that ignore the return value.
            // So pop the expression's result and throw it away to keep the stack aligned correctly.
            //
            // To keep this code simple, we just have the rule that there will unconditionally always
            // be something atop the stack that all function calls leave behind, even if it's a dummy.)
            //
            if (needThrowawayPop)
            {
                AddOpcode(new OpcodePop());
            }            
        }
    }
}
