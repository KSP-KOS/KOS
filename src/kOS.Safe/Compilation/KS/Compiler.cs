using System;
using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Exceptions;
using kOS.Safe.Utilities;

namespace kOS.Safe.Compilation.KS
{
    class Compiler
    {
        private CodePart part;
        private Context context;
        private List<Opcode> currentCodeSection;
        private bool addBranchDestination;
        private ParseNode lastNode;
        private int startLineNum = 1;
        private short lastLine;
        private short lastColumn;
        private readonly List<List<Opcode>> breakList = new List<List<Opcode>>();
        private readonly List<string> triggerRemoveNames = new List<string>();
        private bool nowCompilingTrigger;
        private bool compilingSetDestination;
        private bool identifierIsVariable;
        private bool identifierIsSuffix;
        private bool nowInALoop;
        private readonly List<ParseNode> programParameters = new List<ParseNode>();
        private CompilerOptions options;
        private const bool TRACE_PARSE = false; // set to true to Debug Log each ParseNode as it's visited.

        private readonly Dictionary<string, string> functionsOverloads = new Dictionary<string, string>
        { 
            { "round|1", "roundnearest" },
            { "round|2", "round"} 
        };

        public CodePart Compile(int startLineNum, ParseTree tree, Context context, CompilerOptions options)
        {
            part = new CodePart();
            this.context = context;
            this.options = options;
            this.startLineNum = startLineNum;

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
                if (lastNode != null)
                {
                    throw;  // TODO something more sophisticated will go here that will
                    // attach source/line information to the exception before throwing it upward.
                    // that's why this seemingly pointless "catch and then throw again" is here.
                }
                Debug.Logger.Log("Exception in Compiler: " + kosException.Message);
                Debug.Logger.Log(kosException.StackTrace);
                throw;  // throw it up in addition to logging the stack trace, so the kOS terminal will also give the user some message.
            }

            return part;
        }

        private void CompileProgram(ParseTree tree)
        {
            currentCodeSection = part.MainCode;
            PushProgramParameters();
            VisitNode(tree.Nodes[0]);

            if (addBranchDestination)
            {
                AddOpcode(new OpcodeNOP());
            }
        }

        /// <summary>
        /// Set the current line/column info and potentially also make a helpful
        /// debug trace useful when making syntax changes.
        /// 
        /// </summary>
        /// <returns>true if a line number was found in this node.  mostly used for internal recursion
        /// and can be safely ignored when this is called.</returns>
        private bool NodeStartHousekeeping(ParseNode node)
        {
            if (node == null) { throw new ArgumentNullException("node"); }

            if (TRACE_PARSE)
                Debug.Logger.Log("traceParse: visiting node: " + node.Token.Type.ToString() + ", " + node.Token.Text);

            if (node.Token == null || node.Token.Line <= 0)
            {
                // Only those nodes which are primitive tokens will have line number
                // information.  So perform a leftmost search of the subtree of nodes
                // until a node with a token with a line number is found:
                return node.Nodes.Any(NodeStartHousekeeping);
            }

            lastLine = (short)(node.Token.Line + (startLineNum - 1));
            lastColumn = (short)(node.Token.Column);
            return true;

        }

        private Opcode AddOpcode(Opcode opcode, string destinationLabel)
        {
            opcode.Label = GetNextLabel(true);
            opcode.DestinationLabel = destinationLabel;
            opcode.SourceLine = lastLine;
            opcode.SourceColumn = lastColumn;
            currentCodeSection.Add(opcode);
            addBranchDestination = false;
            return opcode;
        }

        private Opcode AddOpcode(Opcode opcode)
        {
            return AddOpcode(opcode, string.Empty);
        }

        private string GetNextLabel(bool increment)
        {
            string newLabel = string.Format("@{0:0000}", context.LabelIndex + 1);
            if (increment) context.LabelIndex++;
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
            }
        }

        private void PreProcessStatements(ParseNode node)
        {

            lastNode = node;

            NodeStartHousekeeping(node);

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
            NodeStartHousekeeping(node);
            int expressionHash = ConcatenateNodes(node).GetHashCode();
            string triggerIdentifier = "on-" + expressionHash.ToString();
            Trigger triggerObject = context.Triggers.GetTrigger(triggerIdentifier);
            triggerObject.SetTriggerVariable(GetIdentifierText(node));

            currentCodeSection = triggerObject.Code;
            AddOpcode(new OpcodePush(triggerObject.VariableNameOldValue));
            AddOpcode(new OpcodePush(triggerObject.VariableName));
            AddOpcode(new OpcodeCompareEqual());
            AddOpcode(new OpcodeLogicNot());
            Opcode branchOpcode = AddOpcode(new OpcodeBranchIfFalse());

            // make flag that remembers whether to remove trigger:
            // defaults to true = removal should happen.
            string triggerRemoveVarName = "$remove-" + triggerIdentifier;
            PushTriggerRemoveName(triggerRemoveVarName);
            AddOpcode(new OpcodePush(triggerRemoveVarName));
            AddOpcode(new OpcodePush(true));
            AddOpcode(new OpcodeStore());

            VisitNode(node.Nodes[2]);

            // Reset the "old value" so the boolean has to change *again* to re-trigger
            AddOpcode(new OpcodePush(triggerObject.VariableNameOldValue));
            AddOpcode(new OpcodePush(triggerObject.VariableName));
            AddOpcode(new OpcodeStore());

            // Skip removing the trigger if PRESERVE happened:
            PopTriggerRemoveName(); // Throw away return value.
            AddOpcode(new OpcodePush(triggerRemoveVarName));
            Opcode skipRemoval = AddOpcode(new OpcodeBranchIfFalse());

            AddOpcode(new OpcodePushRelocateLater(null), triggerObject.GetFunctionLabel());
            AddOpcode(new OpcodeRemoveTrigger());
            Opcode eofOpcode = AddOpcode(new OpcodeEOF());
            branchOpcode.DestinationLabel = eofOpcode.Label;
            skipRemoval.DestinationLabel = eofOpcode.Label;
        }

        private void PreProcessWhenStatement(ParseNode node)
        {
            NodeStartHousekeeping(node);
            int expressionHash = ConcatenateNodes(node).GetHashCode();
            string triggerIdentifier = "when-" + expressionHash.ToString();
            Trigger triggerObject = context.Triggers.GetTrigger(triggerIdentifier);

            currentCodeSection = triggerObject.Code;
            VisitNode(node.Nodes[1]);
            Opcode branchOpcode = AddOpcode(new OpcodeBranchIfFalse());

            // make flag that remembers whether to remove trigger:
            // defaults to true = removal should happen.
            string triggerRemoveVarName = "$remove-" + triggerIdentifier;
            PushTriggerRemoveName(triggerRemoveVarName);
            AddOpcode(new OpcodePush(triggerRemoveVarName));
            AddOpcode(new OpcodePush(true));
            AddOpcode(new OpcodeStore());

            VisitNode(node.Nodes[3]);

            // Skip removing the trigger if PRESERVE happened:
            PopTriggerRemoveName(); // Throw away return value.
            AddOpcode(new OpcodePush(triggerRemoveVarName));
            Opcode skipRemoval = AddOpcode(new OpcodeBranchIfFalse());

            AddOpcode(new OpcodePushRelocateLater(null), triggerObject.GetFunctionLabel());
            AddOpcode(new OpcodeRemoveTrigger());
            Opcode eofOpcode = AddOpcode(new OpcodeEOF());
            branchOpcode.DestinationLabel = eofOpcode.Label;
            skipRemoval.DestinationLabel = eofOpcode.Label;
        }

        private void PreProcessWaitStatement(ParseNode node)
        {
            NodeStartHousekeeping(node);
            if (node.Nodes.Count == 4)
            {
                // wait condition
                int expressionHash = ConcatenateNodes(node).GetHashCode();
                string triggerIdentifier = "wait-" + expressionHash.ToString();
                Trigger triggerObject = context.Triggers.GetTrigger(triggerIdentifier);

                currentCodeSection = triggerObject.Code;
                VisitNode(node.Nodes[2]);
                Opcode branchOpcode = AddOpcode(new OpcodeBranchIfFalse());
                AddOpcode(new OpcodeEndWait());
                AddOpcode(new OpcodePushRelocateLater(null), triggerObject.GetFunctionLabel());
                AddOpcode(new OpcodeRemoveTrigger());
                Opcode eofOpcode = AddOpcode(new OpcodeEOF());
                branchOpcode.DestinationLabel = eofOpcode.Label;
            }
        }

        private string ConcatenateNodes(ParseNode node)
        {
            string concatenated = node.Token.Text;

            if (node.Nodes.Any())
            {
                return node.Nodes.Aggregate(concatenated, (current, childNode) => current + ConcatenateNodes(childNode));
            }

            return concatenated;
        }

        private void IdentifyLocks(ParseNode node)
        {
            string lockIdentifier = node.Nodes[1].Token.Text;
            context.Locks.GetLock(lockIdentifier);
        }

        private void PreProcessLockStatement(ParseNode node)
        {
            NodeStartHousekeeping(node);
            string lockIdentifier = node.Nodes[1].Token.Text;
            Lock lockObject = context.Locks.GetLock(lockIdentifier);
            int expressionHash = ConcatenateNodes(node.Nodes[3]).GetHashCode();

            if (!lockObject.IsInitialized())
            {
                // initialization code
                currentCodeSection = lockObject.InitializationCode;
                AddOpcode(new OpcodePush(lockObject.PointerIdentifier));
                AddOpcode(new OpcodePushRelocateLater(null), lockObject.DefaultLabel);
                AddOpcode(new OpcodeStore());

                if (lockObject.IsSystemLock())
                {
                    // add trigger
                    string triggerIdentifier = "lock-" + lockObject.Identifier;
                    Trigger triggerObject = context.Triggers.GetTrigger(triggerIdentifier);

                    short rememberLastLine = lastLine;
                    lastLine = -1; // special flag telling the error handler that these opcodes came from the system itself, when reporting the error
                    currentCodeSection = triggerObject.Code;
                    AddOpcode(new OpcodePush("$" + lockObject.Identifier));
                    AddOpcode(new OpcodeCall(lockObject.PointerIdentifier));
                    AddOpcode(new OpcodeStore());
                    AddOpcode(new OpcodeEOF());
                    lastLine = rememberLastLine;
                }

                // default function
                currentCodeSection = lockObject.GetLockFunction(0);
                AddOpcode(new OpcodePush("$" + lockObject.Identifier)).Label = lockObject.DefaultLabel;
                AddOpcode(new OpcodeReturn());
            }

            // function code
            currentCodeSection = lockObject.GetLockFunction(expressionHash);
            VisitNode(node.Nodes[3]);
            AddOpcode(new OpcodeReturn());
        }

        private void PreProcessProgramParameters(ParseNode node)
        {
            NodeStartHousekeeping(node);
            // if the declaration is a parameter
            if (node.Nodes[1].Token.Type == TokenType.PARAMETER)
            {
                for (int index = 2; index < node.Nodes.Count; index += 2)
                {
                    programParameters.Add(node.Nodes[index]);
                }
            }
        }

        private void PreProcessRunStatement(ParseNode node)
        {
            NodeStartHousekeeping(node);
            if (options.LoadProgramsInSameAddressSpace)
            {
                bool hasON = node.Nodes.Any(cn => cn.Token.Type == TokenType.ON);
                if (!hasON)
                {
                    string subprogramName = node.Nodes[1].Token.Text; // It assumes it already knows at compile-time how many unique program filenames exist, 
                    if (!context.Subprograms.Contains(subprogramName)) // which it uses to decide how many of these blocks to make,
                    {                                                   // which is why we can't defer run filenames until runtime like we can with the others.
                        Subprogram subprogramObject = context.Subprograms.GetSubprogram(subprogramName);
                        // Function code
                        currentCodeSection = subprogramObject.FunctionCode;
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
                        currentCodeSection = subprogramObject.InitializationCode;
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
            programParameters.Reverse();
            foreach (ParseNode node in programParameters)
            {
                VisitNode(node);
                AddOpcode(new OpcodeSwap());
                AddOpcode(new OpcodeStore());
            }
        }

        private void PushTriggerRemoveName(string newLabel)
        {
            triggerRemoveNames.Add(newLabel);
            nowCompilingTrigger = true;
        }

        private string PeekTriggerRemoveName()
        {
            return nowCompilingTrigger ? triggerRemoveNames[triggerRemoveNames.Count - 1] : string.Empty;
        }

        private string PopTriggerRemoveName()
        {
            // Will throw exception if list is empty, but that "should
            // never happen" as pushes and pops should be balanced in
            // the compiler's code.  If it throws exception we want to
            // let the exception happen to highlight the bug
            string returnVal = triggerRemoveNames[triggerRemoveNames.Count - 1];
            triggerRemoveNames.RemoveAt(triggerRemoveNames.Count - 1);
            nowCompilingTrigger = (triggerRemoveNames.Count > 0);
            return returnVal;
        }

        private void PushBreakList()
        {
            List<Opcode> list = new List<Opcode>();
            breakList.Add(list);
        }

        private void AddToBreakList(Opcode opcode)
        {
            if (breakList.Count > 0)
            {
                List<Opcode> list = breakList[breakList.Count - 1];
                list.Add(opcode);
            }
        }

        private void PopBreakList(string label)
        {
            if (breakList.Count > 0)
            {
                List<Opcode> list = breakList[breakList.Count - 1];
                if (list != null)
                {
                    breakList.Remove(list);
                    foreach (Opcode opcode in list)
                    {
                        opcode.DestinationLabel = label;
                    }
                }
            }
        }

        private void VisitNode(ParseNode node)
        {
            lastNode = node;

            NodeStartHousekeeping(node);

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
                case TokenType.onoff_trailer:
                    VisitOnOffTrailer(node);
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
                case TokenType.arglist:
                    VisitArgList(node);
                    break;
				case TokenType.hudtxt_stmt:
					VisitHudtxtStatement(node);
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
                case TokenType.suffixterm:
                    VisitSuffixTerm(node);
                    break;
                case TokenType.IDENTIFIER:
                    VisitIdentifier(node);
                    break;
                case TokenType.FILEIDENT:
                    VisitFileIdent(node);
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
            }
        }

        private void VisitChildNodes(ParseNode node)
        {
            NodeStartHousekeeping(node);
            foreach (ParseNode childNode in node.Nodes)
            {
                VisitNode(childNode);
            }
        }

        private void VisitVariableNode(ParseNode node)
        {
            NodeStartHousekeeping(node);
            identifierIsVariable = true;
            VisitNode(node);
            identifierIsVariable = false;
        }

        private void VisitExpression(ParseNode node)
        {
            NodeStartHousekeeping(node);
            if (node.Nodes.Count > 1)
            {
                // it should always be odd, two arguments and one operator
                if ((node.Nodes.Count % 2) != 1) return;

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
            NodeStartHousekeeping(node);
            if (node.Nodes.Count <= 0) return;

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

        private void VisitSciNumber(ParseNode node)
        {
            NodeStartHousekeeping(node);
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
                double number = mantissa * System.Math.Pow(10, exponent);
                AddOpcode(new OpcodePush(number));
            }
        }

        private void VisitNumber(ParseNode node)
        {
            NodeStartHousekeeping(node);
            VisitNode(node.Nodes[0]);
        }

        private void VisitInteger(ParseNode node)
        {
            NodeStartHousekeeping(node);
            object number;
            int integerNumber;

            if (int.TryParse(node.Token.Text, out integerNumber))
            {
                number = integerNumber;
            }
            else
            {
                number = double.Parse(node.Token.Text);
            }

            AddOpcode(new OpcodePush(number));
        }

        private void VisitDouble(ParseNode node)
        {
            NodeStartHousekeeping(node);
            object number = double.Parse(node.Token.Text);

            AddOpcode(new OpcodePush(number));
        }

        private void VisitTrueFalse(ParseNode node)
        {
            NodeStartHousekeeping(node);
            bool boolValue;
            if (bool.TryParse(node.Token.Text, out boolValue))
            {
                AddOpcode(new OpcodePush(boolValue));
            }
        }

        private void VisitOnOffTrailer(ParseNode node)
        {
            NodeStartHousekeeping(node);
            AddOpcode(new OpcodePush((node.Nodes[0].Token.Type == TokenType.ON) ? true : false));
        }

        /// <summary>
        /// Do the work for function calls.
        /// </summary>
        /// <param name="node">parse node for the function term of the parse tree.</param>
        /// <param name="isDirect">true if it should make an OpcodeCall that is Direct, false if it should make an indirect one.
        /// See the documentation for OpcodeCall.Direct for the full explanation of the difference.  If isDirect is true, then
        /// the name to the left of the parentheses will be the name of the function call.  If isDirect is false, then it will
        /// ignore the name to the left of the parentheses and presume the function name, delegate, or branch index was
        /// already placed atop the stack by other parts of this compiler.</param>
        /// <param name="directName">In the case where it's a direct function, what's the name of it?  In the case
        /// where it's not direct, this argument doesn't matter.</param>
        private void VisitActualFunction(ParseNode node, bool isDirect, string directName = "")
        {
            NodeStartHousekeeping(node);

            int parameterCount = 0;
            ParseNode trailerNode = node; // the function_trailer rule is here.

            if (!isDirect)
            {
                // Need to tell OpcodeCall where in the stack the bottom of the arg list is.
                // Even if there are no arguments, it still has to be TOLD that by showing
                // it the marker atop the stack with nothing above it.
                AddOpcode(new OpcodePush(OpcodeCall.ARG_MARKER_STRING));
            }

            if (trailerNode.Nodes[1].Token.Type == TokenType.arglist)
            {

                parameterCount = (trailerNode.Nodes[1].Nodes.Count / 2) + 1;

                bool remember = identifierIsSuffix;
                identifierIsSuffix = false;

                VisitNode(trailerNode.Nodes[1]);

                identifierIsSuffix = remember;
            }

            if (isDirect)
            {
                string functionName = directName;

                string overloadedFunctionName = GetFunctionOverload(functionName, parameterCount) + "()";
                AddOpcode(new OpcodeCall(overloadedFunctionName));
            }
            else
            {
                OpcodeCall op = new OpcodeCall(string.Empty) { Direct = false };
                AddOpcode(op);
            }
        }

        private string GetFunctionOverload(string functionName, int parameterCount)
        {
            string functionKey = string.Format("{0}|{1}", functionName, parameterCount);
            if (functionsOverloads.ContainsKey(functionKey))
            {
                return functionsOverloads[functionKey];
            }
            return functionName;
        }

        private void VisitArgList(ParseNode node)
        {
            NodeStartHousekeeping(node);
            int nodeIndex = 0;
            while (nodeIndex < node.Nodes.Count)
            {
                VisitNode(node.Nodes[nodeIndex]);
                nodeIndex += 2;
            }
        }

        private void VisitVarIdentifier(ParseNode node)
        {
            NodeStartHousekeeping(node);

            // I might be called on a raw IDENTIFIER, in which case I have no
            // child nodes to descend into.  But if I *do* have a child node
            // to descend into, then do so:
            VisitNode(node.Nodes.Count == 0 ? node : node.Nodes[0]);
        }

        // Parses this rule:
        // suffix             -> suffixterm (suffix_trailer)*;
        // suffix_trailer     -> (COLON suffixterm);
        private void VisitSuffix(ParseNode node)
        {
            NodeStartHousekeeping(node);

            // For each suffixterm between colons:
            for (int nodeIndex = 0; nodeIndex < node.Nodes.Count; ++nodeIndex)
            {

                bool remember = identifierIsSuffix;
                identifierIsSuffix = (nodeIndex > 0);

                ParseNode suffixTerm;
                if (nodeIndex == 0)
                    suffixTerm = node.Nodes[nodeIndex];
                else
                    // nodes after the first are suffix_trailers consisting of (COLON suffixterm).  This skips the colon.
                    suffixTerm = node.Nodes[nodeIndex].Nodes[1];

                // Is it being portrayed like a function call with parentheses?
                bool startsWithFunc =
                    (suffixTerm.Nodes.Count > 1 &&
                     suffixTerm.Nodes[1].Nodes.Count > 0 &&
                     suffixTerm.Nodes[1].Nodes[0].Token.Type == TokenType.function_trailer);

                string firstIdentifier = "";
                // The term starts with either an identifier or an expression.  If it's the start, then parse
                // it as a variable, else parse it as a raw identifier:
                bool rememberIsV = identifierIsVariable;
                identifierIsVariable = (!startsWithFunc) && nodeIndex == 0;

                // Push this term on the stack unless it's the name of the built-in-function (built-in-functions
                // being called without any preceding colon term, with methods on the other hand having suffixes):
                if (nodeIndex > 0 || !startsWithFunc)
                    VisitNode(suffixTerm.Nodes[0]);
                identifierIsVariable = rememberIsV;
                if (nodeIndex == 0)
                {
                    firstIdentifier = GetIdentifierText(suffixTerm);
                }
                else
                {
                    // when we are setting a member value we need to leave
                    // the last object and the last suffix in the stack
                    bool usingSetMember = (suffixTerm.Nodes.Count > 0) && (compilingSetDestination && nodeIndex == (node.Nodes.Count - 1));

                    if (!usingSetMember)
                    {
                        AddOpcode(startsWithFunc ? new OpcodeGetMethod() : new OpcodeGetMember());
                    }
                }


                // The remaining terms are a chain of function_trailers "(...)" and array_trailers "[...]" or "#.." in any arbitrary order:
                for (int trailerIndex = 1; trailerIndex < suffixTerm.Nodes.Count; ++trailerIndex)
                {
                    // suffixterm_trailer is always a wrapper around either function_trailer or array_trailer,
                    // so delve down one level to get which of them it is:
                    ParseNode trailerTerm = suffixTerm.Nodes[trailerIndex].Nodes[0];
                    bool isFunc = (trailerTerm.Token.Type == TokenType.function_trailer);
                    bool isArray = (trailerTerm.Token.Type == TokenType.array_trailer);

                    if (isFunc)
                    {
                        // direct if it's just one term like foo(aaa) but indirect
                        // if it's a list of suffixes like foo:bar(aaa):
                        VisitActualFunction(trailerTerm, (nodeIndex == 0), firstIdentifier);
                    }
                    if (isArray)
                    {
                        VisitActualArray(trailerTerm);
                    }
                }

                identifierIsSuffix = remember;

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
            ParseNode trailerNode = node; // should be type array_trailer.

            int nodeIndex = 1;
            while (nodeIndex < trailerNode.Nodes.Count)
            {
                // Skip two tokens instead of one between dimensions if using the "[]" syntax:
                if (trailerNode.Nodes[nodeIndex].Token.Type == TokenType.SQUAREOPEN)
                {
                    ++nodeIndex;
                }

                bool remember = identifierIsSuffix;
                identifierIsSuffix = false;

                VisitNode(trailerNode.Nodes[nodeIndex]);

                identifierIsSuffix = remember;

                // Two ways to check if this is the last index (i.e. the 'k' in arr[i][j][k]'),
                // depending on whether using the "#" syntax or the "[..]" syntax:
                bool isLastIndex = false;
                var previousNodeType = trailerNode.Nodes[nodeIndex - 1].Token.Type;
                switch (previousNodeType)
                {
                    case TokenType.ARRAYINDEX:
                        isLastIndex = (nodeIndex == trailerNode.Nodes.Count - 1);
                        break;
                    case TokenType.SQUAREOPEN:
                        isLastIndex = (nodeIndex == trailerNode.Nodes.Count - 2);
                        break;
                }

                // when we are setting a member value we need to leave
                // the last object and the last index in the stack
                // the only exception is when we are setting a suffix of the indexed value
                bool atEnd = IsLastmostTrailerInTerm(node);
                if (!(compilingSetDestination && isLastIndex) || (!atEnd))
                {
                    AddOpcode(new OpcodeGetIndex());
                }

                nodeIndex += 2;
            }

        }
        
        /// <summary>
        /// Returns true if this is the last most trailer term (array_trailer, suffix_trailer, or function_trailer)
        /// in a term inside a suffix rule of the parser.  Does this by a tree walk to look for siblings to
        /// the right of me.
        /// </summary>
        /// <param name="node">Node to check</param>
        /// <returns>true if I am the rightmost thing in the parse tree all the way up to the suffix term above me.</returns>
        private bool IsLastmostTrailerInTerm(ParseNode node)
        {
            ParseNode current = node;
            ParseNode parent = node.Parent;
            
            while (parent != null && current.Token.Type != TokenType.suffix && current.Token.Type != TokenType.varidentifier)
            {
                if (parent.Nodes.LastIndexOf(current) < parent.Nodes.Count - 1)
                    return false; // there is a child to the right of me.  I am not lastmost.

                current = parent;
                parent = current.Parent;
            }
            return true;
        }

        private string GetIdentifierText(ParseNode node)
        {
            if (node.Token.Type == TokenType.IDENTIFIER || node.Token.Type == TokenType.FILEIDENT)
            {
                return node.Token.Text;
            }
            foreach (ParseNode child in node.Nodes)
            {
                string identifier = GetIdentifierText(child);
                if (identifier != string.Empty)
                    return identifier;
            }

            return string.Empty;
        }

        /// <summary>
        /// The suffixterm parse node contains both actual function calls
        /// (with parentheses) and just plain vanilla terms.  This
        /// determines if it's REALLY a function call or not.
        /// </summary>
        /// <param name="node">the node to test</param>
        /// <returns>true if it's really a function call.  False otherwise.</returns>
        private bool IsActualFunctionCall(ParseNode node)
        {
            // This can be called at the level of the parent of the function node, so get down to it first:
            ParseNode child = node;
            while (child != null &&
                   (child.Token.Type != TokenType.function_trailer &&
                    child.Token.Type != TokenType.identifier_led_expr))
            {
                if (child.Nodes.Count > 1)
                    child = child.Nodes[1];
                else if (child.Nodes.Count == 1)
                    child = child.Nodes[0];
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
        /// <param name="node">The node to test</param>
        /// <returns>true if it's really an array index reference call.  False otherwise.</returns>
        private bool IsActualArrayIndexing(ParseNode node)
        {
            // This can be called at the level of the parent of the array node, so get down to it first:
            ParseNode child = node;
            while (child != null &&
                   (child.Token.Type != TokenType.array_trailer &&
                    child.Token.Type != TokenType.identifier_led_expr))
            {
                if (child.Nodes.Count > 1)
                    child = child.Nodes[1];
                else if (child.Nodes.Count == 1)
                    child = child.Nodes[0];
                else
                    child = null;
            }
            if (child == null)
                return false;

            // If it has the optional array_trailer node tacked on to it, then it's really an array index, not just using array as
            // a dummy wrapper around a plain atom node
            return child.Nodes.Count > 1;
        }

        private void VisitSuffixTerm(ParseNode node)
        {
            NodeStartHousekeeping(node);

            if (node.Nodes.Count > 1 &&
                node.Nodes[1].Token.Type == TokenType.function_trailer)
            {
                // if a bracket follows an identifier then its a function call
                VisitActualFunction(node.Nodes[1], true, GetIdentifierText(node));
            }
            else
            {
                VisitNode(node.Nodes[0]); // I'm not really a function call after all - just a wrapper around another node type.
            }
        }

        private void VisitIdentifier(ParseNode node)
        {
            NodeStartHousekeeping(node);
            bool isVariable = (identifierIsVariable && !identifierIsSuffix);
            string prefix = isVariable ? "$" : String.Empty;
            string identifier = GetIdentifierText(node);
            if (isVariable && context.Locks.Contains(identifier))
            {
                Lock lockObject = context.Locks.GetLock(identifier);
                if (compilingSetDestination)
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

        private void VisitFileIdent(ParseNode node)
        {
            NodeStartHousekeeping(node);
            string identifier = GetIdentifierText(node);
            AddOpcode(new OpcodePush(identifier));
        }

        private void VisitString(ParseNode node)
        {
            NodeStartHousekeeping(node);
            AddOpcode(new OpcodePush(node.Token.Text.Trim('"')));
        }

        /// <summary>
        /// Check for if the var_identifer node has a suffix term as its very next neighbor
        /// to the right.  i.e. in the following syntax:<br/>
        ///     AAA:BBB:CCC[0]:DDD:EEE[0]<br/>
        /// This method should return true if called on the AAA, BBB, or DDD nodes,
        /// but not when called on the CCC or EEE nodes.
        /// </summary>
        /// <param name="node">The node to test</param>
        /// <returns></returns>
        private bool VarIdentifierPreceedsSuffix(ParseNode node)
        {
            // If it's a var_identifier being worked on, drop down one level first
            // to get into the actual meat of the syntax tree it represents:
            if (node.Token.Type == TokenType.varidentifier)
                return VarIdentifierPreceedsSuffix(node.Nodes.First());

            if (node.Token.Type == TokenType.suffix_trailer ||
                node.Nodes.Count > 1 && node.Nodes[1].Token.Type == TokenType.suffix_trailer)
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
            
            bool descendedThroughAColon = false; // eeeewwww, sounds disgusting.
                
            while (thisChild.Token.Type == TokenType.suffix_trailer ||
                   thisChild.Token.Type == TokenType.suffix || 
                   thisChild.Token.Type == TokenType.suffixterm || 
                   thisChild.Token.Type == TokenType.suffixterm_trailer || 
                   thisChild.Token.Type == TokenType.array_trailer ||
                   thisChild.Token.Type == TokenType.function_trailer)
            {
                if (thisChild.Token.Type == TokenType.suffix_trailer)
                    descendedThroughAColon = true;
                prevChild = thisChild;
                thisChild = thisChild.Nodes.Last();
            }
            return descendedThroughAColon && 
                (prevChild.Token.Type == TokenType.suffix_trailer ||
                 prevChild.Token.Type == TokenType.suffixterm);
        }

        /// <summary>
        /// Check for if the var_identifer node has an array index as its very next neighbor
        /// to the right.  i.e. in the following syntax:<br/>
        ///     AAA:BBB:CCC[0]:DDD:EEE[0]<br/>
        /// This method should return true if called on the CCC node or the EEE node,
        /// but not when called on the AAA, BBB, or DDD nodes.
        /// </summary>
        /// <param name="node">The node to test</param>
        /// <returns></returns>
        private bool VarIdentifierPreceedsIndex(ParseNode node)
        {
            // If it's a var_identifier being worked on, drop down one level first
            // to get into the actual meat of the syntax tree it represents:
            if (node.Token.Type == TokenType.varidentifier)
                return VarIdentifierPreceedsIndex(node.Nodes.First());

            if (node.Token.Type == TokenType.array_trailer ||
                node.Nodes.Count > 1 && node.Nodes[1].Token.Type == TokenType.array_trailer)
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
                   thisChild.Token.Type == TokenType.suffix || 
                   thisChild.Token.Type == TokenType.suffixterm || 
                   thisChild.Token.Type == TokenType.suffixterm_trailer || 
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
        private ParseNode DepthFirstLeftSearch(ParseNode node, TokenType tokType)
        {
            if (node.Token.Type == tokType)
            {
                return node;
            }
            foreach (ParseNode child in node.Nodes)
            {
                ParseNode hit = DepthFirstLeftSearch(child, tokType);
                if (hit != null)
                    return hit;
            }

            return null; // not found.
        }

        private void VisitSetStatement(ParseNode node)
        {
            NodeStartHousekeeping(node);
            ProcessSetOperation(node.Nodes[1], node.Nodes[3]);
        }

        /// <summary>
        /// For any statement of the form "SET THIS TO THAT", or "THIS ON" or "THIS OFF".
        /// </summary>
        /// <param name="setThis">The lefthand-side expression to be set</param>
        /// <param name="toThis">The righthand-side expression to set it to</param>
        private void ProcessSetOperation(ParseNode setThis, ParseNode toThis)
        {
            // destination
            compilingSetDestination = true;
            VisitNode(setThis);
            compilingSetDestination = false;
            // expression
            VisitNode(toThis);

            if (VarIdentifierEndsWithSuffix(setThis))
            {
                AddOpcode(new OpcodeSetMember());
            }
            else if (VarIdentifierEndsWithIndex(setThis))
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
            NodeStartHousekeeping(node);
            // The IF check:
            VisitNode(node.Nodes[1]);
            Opcode branchToFalse = AddOpcode(new OpcodeBranchIfFalse());
            // The IF BODY:
            VisitNode(node.Nodes[2]);
            if (node.Nodes.Count < 4)
            {
                // No ELSE exists.
                // Jump to after the IF BODY if false:
                branchToFalse.DestinationLabel = GetNextLabel(false);
                addBranchDestination = true;
            }
            else
            {
                // The IF statement has an ELSE clause.

                // Jump past the ELSE body from the end of the IF body:
                Opcode branchPastElse = AddOpcode(new OpcodeBranchJump());
                // This is where the ELSE clause starts:
                branchToFalse.DestinationLabel = GetNextLabel(false);
                // The else body:
                VisitNode(node.Nodes[4]);
                // End of Else body label:
                branchPastElse.DestinationLabel = GetNextLabel(false);
                addBranchDestination = true;
            }
        }

        private void VisitUntilStatement(ParseNode node)
        {
            NodeStartHousekeeping(node);

            bool remember = nowInALoop;
            nowInALoop = true;

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
            addBranchDestination = true;

            nowInALoop = remember;
        }

        private void VisitPlusMinus(ParseNode node)
        {
            NodeStartHousekeeping(node);
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
            NodeStartHousekeeping(node);
            AddOpcode(new OpcodeMathMultiply());
        }

        private void VisitDiv(ParseNode node)
        {
            NodeStartHousekeeping(node);
            AddOpcode(new OpcodeMathDivide());
        }

        private void VisitPower(ParseNode node)
        {
            NodeStartHousekeeping(node);
            AddOpcode(new OpcodeMathPower());
        }

        private void VisitAnd(ParseNode node)
        {
            NodeStartHousekeeping(node);
            AddOpcode(new OpcodeLogicAnd());
        }

        private void VisitOr(ParseNode node)
        {
            NodeStartHousekeeping(node);
            AddOpcode(new OpcodeLogicOr());
        }

        private void VisitComparator(ParseNode node)
        {
            NodeStartHousekeeping(node);
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
            }
        }

        private void VisitLockStatement(ParseNode node)
        {
            NodeStartHousekeeping(node);
            string lockIdentifier = node.Nodes[1].Token.Text;
            int expressionHash = ConcatenateNodes(node.Nodes[3]).GetHashCode();
            Lock lockObject = context.Locks.GetLock(lockIdentifier);

            if (lockObject.IsInitialized())
            {
                string functionLabel = lockObject.GetLockFunction(expressionHash)[0].Label;
                // lock variable
                AddOpcode(new OpcodePush(lockObject.PointerIdentifier));
                AddOpcode(new OpcodePushRelocateLater(null), functionLabel);
                AddOpcode(new OpcodeStore());

                if (lockObject.IsSystemLock())
                {
                    // add update trigger
                    string triggerIdentifier = "lock-" + lockIdentifier;
                    if (context.Triggers.Contains(triggerIdentifier))
                    {
                        Trigger triggerObject = context.Triggers.GetTrigger(triggerIdentifier);
                        AddOpcode(new OpcodePushRelocateLater(null), triggerObject.GetFunctionLabel());
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
            NodeStartHousekeeping(node);
            if (node.Nodes[1].Token.Type == TokenType.ALL)
            {
                // unlock all locks
                foreach (Lock lockObject in context.Locks.GetLockList())
                    UnlockIdentifier(lockObject);
            }
            else
            {
                string lockIdentifier = node.Nodes[1].Token.Text;
                Lock lockObject = context.Locks.GetLock(lockIdentifier);
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
                    if (context.Triggers.Contains(triggerIdentifier))
                    {
                        Trigger triggerObject = context.Triggers.GetTrigger(triggerIdentifier);
                        AddOpcode(new OpcodePushRelocateLater(null), triggerObject.GetFunctionLabel());
                        AddOpcode(new OpcodeRemoveTrigger());
                    }
                }

                // unlock variable
                AddOpcode(new OpcodePush(lockObject.PointerIdentifier));
                AddOpcode(new OpcodePushRelocateLater(null), lockObject.DefaultLabel);
                AddOpcode(new OpcodeStore());
            }
        }

        private void VisitOnStatement(ParseNode node)
        {
            NodeStartHousekeeping(node);
            int expressionHash = ConcatenateNodes(node).GetHashCode();
            string triggerIdentifier = "on-" + expressionHash.ToString();
            Trigger triggerObject = context.Triggers.GetTrigger(triggerIdentifier);

            if (triggerObject.IsInitialized())
            {
                AddOpcode(new OpcodePush(triggerObject.VariableNameOldValue));
                AddOpcode(new OpcodePush(triggerObject.VariableName));
                AddOpcode(new OpcodeStore());
                AddOpcode(new OpcodePushRelocateLater(null), triggerObject.GetFunctionLabel());
                AddOpcode(new OpcodeAddTrigger(false));
            }
        }

        private void VisitWhenStatement(ParseNode node)
        {
            NodeStartHousekeeping(node);
            int expressionHash = ConcatenateNodes(node).GetHashCode();
            string triggerIdentifier = "when-" + expressionHash.ToString();
            Trigger triggerObject = context.Triggers.GetTrigger(triggerIdentifier);

            if (triggerObject.IsInitialized())
            {
                AddOpcode(new OpcodePushRelocateLater(null), triggerObject.GetFunctionLabel());
                AddOpcode(new OpcodeAddTrigger(false));
            }
        }

        private void VisitWaitStatement(ParseNode node)
        {
            NodeStartHousekeeping(node);

            if (nowCompilingTrigger)
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
                Trigger triggerObject = context.Triggers.GetTrigger(triggerIdentifier);

                if (triggerObject.IsInitialized())
                {
                    AddOpcode(new OpcodePushRelocateLater(null), triggerObject.GetFunctionLabel());
                    AddOpcode(new OpcodeAddTrigger(true));
                }
            }
        }

        private void VisitDeclareStatement(ParseNode node)
        {
            NodeStartHousekeeping(node);
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
            NodeStartHousekeeping(node);
            VisitVarIdentifier(node.Nodes[1]);
            VisitVarIdentifier(node.Nodes[1]);
            AddOpcode(new OpcodeLogicToBool());
            AddOpcode(new OpcodeLogicNot());
            AddOpcode(new OpcodeStore());
        }

		private void VisitHudtxtStatement(ParseNode node)
		{
			NodeStartHousekeeping(node);
			{
				VisitNode(node.Nodes[2]);
				VisitNode(node.Nodes[4]);
				VisitNode(node.Nodes[6]);
				VisitNode(node.Nodes[8]);
				VisitNode(node.Nodes[10]);
				VisitNode(node.Nodes[12]);
				AddOpcode(new OpcodeCall("hudtxt()"));
			}
		}

        private void VisitPrintStatement(ParseNode node)
        {
            NodeStartHousekeeping(node);
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
            NodeStartHousekeeping(node);
            AddOpcode(new OpcodeCall("stage()"));
        }

        private void VisitAddStatement(ParseNode node)
        {
            NodeStartHousekeeping(node);
            VisitNode(node.Nodes[1]);
            AddOpcode(new OpcodeCall("add()"));
        }

        private void VisitRemoveStatement(ParseNode node)
        {
            NodeStartHousekeeping(node);
            VisitNode(node.Nodes[1]);
            AddOpcode(new OpcodeCall("remove()"));
        }

        private void VisitClearStatement(ParseNode node)
        {
            NodeStartHousekeeping(node);
            AddOpcode(new OpcodeCall("clearscreen()"));
        }

        private void VisitEditStatement(ParseNode node)
        {
            NodeStartHousekeeping(node);
            VisitNode(node.Nodes[1]);
            AddOpcode(new OpcodeCall("edit()"));
        }

        private void VisitRunStatement(ParseNode node)
        {
            NodeStartHousekeeping(node);
            int volumeIndex = 3;

            // process program arguments
            if (node.Nodes.Count > 3 && node.Nodes[3].Token.Type == TokenType.arglist)
            {
                VisitNode(node.Nodes[3]);
                volumeIndex += 3;
            }

            bool hasON = node.Nodes.Any(cn => cn.Token.Type == TokenType.ON);
            if (!hasON && options.LoadProgramsInSameAddressSpace)
            {
                string subprogramName = node.Nodes[1].Token.Text; // This assumption that the filenames are known at compile-time is why we can't do RUN expr 
                if (context.Subprograms.Contains(subprogramName))  // and instead have to do RUN FILEIDENT, in the parser def.
                {
                    Subprogram subprogramObject = context.Subprograms.GetSubprogram(subprogramName);
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
            NodeStartHousekeeping(node);
            VisitNode(node.Nodes[1]);
            if (node.Nodes.Count > 3)
            {
                // It has a "TO outputfile" clause:
                VisitNode(node.Nodes[3]);
            }
            else
            {
                // It lacks a "TO outputfile" clause, so put a dummy string there for it to
                // detect later during the "load()" function as a flag telling it to
                // calculate the output name:
                AddOpcode(new OpcodePush("-default-compile-out-"));
            }
            AddOpcode(new OpcodeCall("load()"));
        }

        private void VisitSwitchStatement(ParseNode node)
        {
            NodeStartHousekeeping(node);
            VisitNode(node.Nodes[2]);
            AddOpcode(new OpcodeCall("switch()"));
        }

        private void VisitCopyStatement(ParseNode node)
        {
            NodeStartHousekeeping(node);
            VisitNode(node.Nodes[1]);

            AddOpcode(new OpcodePush(node.Nodes[2].Token.Type == TokenType.FROM ? "from" : "to"));

            VisitNode(node.Nodes[3]);
            AddOpcode(new OpcodeCall("copy()"));
        }

        private void VisitRenameStatement(ParseNode node)
        {
            NodeStartHousekeeping(node);
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
                AddOpcode(new OpcodePush(node.Nodes[2].Token.Type == TokenType.FROM ? "file" : "volume"));
            }

            VisitNode(node.Nodes[oldNameIndex]);
            VisitNode(node.Nodes[newNameIndex]);
            AddOpcode(new OpcodeCall("rename()"));
        }

        private void VisitDeleteStatement(ParseNode node)
        {
            NodeStartHousekeeping(node);
            VisitNode(node.Nodes[1]);

            if (node.Nodes.Count == 5)
                VisitNode(node.Nodes[3]);
            else
                AddOpcode(new OpcodePush(null));

            AddOpcode(new OpcodeCall("delete()"));
        }

        private void VisitListStatement(ParseNode node)
        {
            NodeStartHousekeeping(node);
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
            NodeStartHousekeeping(node);
            VisitNode(node.Nodes[1]);
            VisitNode(node.Nodes[3]);
            AddOpcode(new OpcodeCall("logfile()"));
        }

        private void VisitBreakStatement(ParseNode node)
        {
            NodeStartHousekeeping(node);

            if (!nowInALoop)
                throw new KOSBreakInvalidHereException();

            Opcode jump = AddOpcode(new OpcodeBranchJump());
            AddToBreakList(jump);
        }

        private void VisitPreserveStatement(ParseNode node)
        {
            NodeStartHousekeeping(node);

            if (!nowCompilingTrigger)
                throw new KOSPreserveInvalidHereException();

            string flagName = PeekTriggerRemoveName();
            AddOpcode(new OpcodePush(flagName));
            AddOpcode(new OpcodePush(false));
            AddOpcode(new OpcodeStore());
        }

        private void VisitRebootStatement(ParseNode node)
        {
            NodeStartHousekeeping(node);
            AddOpcode(new OpcodeCall("reboot()"));
        }

        private void VisitShutdownStatement(ParseNode node)
        {
            NodeStartHousekeeping(node);
            AddOpcode(new OpcodeCall("shutdown()"));
        }

        private void VisitForStatement(ParseNode node)
        {
            NodeStartHousekeeping(node);

            bool remember = nowInALoop;
            nowInALoop = true;

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

            nowInALoop = remember;
        }

        private void VisitUnsetStatement(ParseNode node)
        {
            NodeStartHousekeeping(node);
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
            NodeStartHousekeeping(node);
            throw new Exception("Batch mode can only be used when in immediate mode.");
        }

        private void VisitDeployStatement(ParseNode node)
        {
            NodeStartHousekeeping(node);
            throw new Exception("Batch mode can only be used when in immediate mode.");
        }



        private void VisitIdentifierLedStatement(ParseNode node)
        {
            NodeStartHousekeeping(node);

            // IdentifierLedStatement is an IdentifierLedExpression with the end marker "." tacked on.
            // Just parse the IdentifierLedExpression:
            VisitNode(node.Nodes[0]);
        }

        private void VisitIdentifierLedExpression(ParseNode node)
        {
            NodeStartHousekeeping(node);

            // This is either an ON/OFF statement like this:
            //     BUNCHA_STUFF ON.
            //     BUNCHA_STUFF OFF.
            // Or this is a statement calling a function like one of these:
            //     BUNCHA_STUFF(args).
            //     BUNCHA_STUFF.
            //     BUNCHA_STUFF[idx]. // Not valid but the compiler will complain later.
            // In all of the above the "BUNCHA_STUFF" should be parsed using the VisitSuffix
            // code, to handle colons, and chains of suffixes, and so on.

            if (node.Nodes.Count > 1 && node.Nodes[1].Token.Type == TokenType.onoff_trailer)
            {
                // In the case of an on/off statement:
                // Transform the statement into the equivalent more sane statement:
                // SET BUNCHA_STUFF TO TRUE. // OR FALSE, Depending.
                // And have the compiler parse it THAT way instead of trying to mess with it here:
                ProcessSetOperation(node.Nodes[0], node.Nodes[1]);
            }
            else
            {
                // In the case of anything other than an on/off statement:
                // Just do the normal stuff to parse the suffix rule expression, and then throw
                // the result away off the top of the stack.
                // To keep this code simple, we just have the rule that there will unconditionally always
                // be something atop the stack that all function calls leave behind, even if it's a dummy.)
                VisitNode(node.Nodes[0]);
                AddOpcode(new OpcodePop());
            }
        }
    }
}
