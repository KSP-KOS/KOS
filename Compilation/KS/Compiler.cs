using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace kOS.Compilation.KS
{
    class Compiler
    {
        private CodePart _part;
        private Context _context;
        private List<Opcode> _currentCodeSection = null;
        private bool _addBranchDestination = false;
        private ParseNode _lastNode = null;
        private List<List<Opcode>> _breakList = new List<List<Opcode>>();
        private bool _compilingSetDestination = false;

        private readonly Dictionary<string, string> _identifierReplacements = new Dictionary<string, string>() { { "alt:radar", "alt|radar" },
                                                                                                                 { "alt:apoapsis", "alt|apoapsis" },
                                                                                                                 { "alt:periapsis", "alt|periapsis" },
                                                                                                                 { "eta:apoapsis", "eta|apoapsis" },
                                                                                                                 { "eta:periapsis", "eta|periapsis" } };
        private readonly Dictionary<string, string> _functionsOverloads = new Dictionary<string, string>() { { "round|1", "roundnearest" },
                                                                                                             { "round|2", "round"} };
        
        public CodePart Compile(ParseTree tree, Context context)
        {
            _part = new CodePart();
            _context = context;

            try
            {
                if (tree.Nodes.Count > 0)
                {
                    PreProcess(tree);
                    CompileProgram(tree);
                }
            }
            catch (Exception e)
            {
                if (_lastNode != null)
                {
                    throw new Exception(string.Format("Error parsing {0}: {1}", ConcatenateNodes(_lastNode), e.Message));
                }
                else
                {
                    throw;
                }
            }
            
            return _part;
        }

        private void CompileProgram(ParseTree tree)
        {
            _currentCodeSection = _part.MainCode;
            VisitNode(tree.Nodes[0]);

            if (_addBranchDestination)
            {
                AddOpcode(new OpcodeNOP());
            }
        }

        private Opcode AddOpcode(Opcode opcode, string destinationLabel)
        {
            opcode.Label = GetNextLabel(true);
            opcode.DestinationLabel = destinationLabel;
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
            string newLabel = string.Format("KL_{0:0000}", _context.LabelIndex + 1);
            if (increment) _context.LabelIndex++;
            return newLabel;
        }

        private void PreProcess(ParseTree tree)
        {
            PreProcessStatements(tree.Nodes[0]);
        }

        private void PreProcessStatements(ParseNode node)
        {
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
                case TokenType.lock_stmt:
                    PreProcessLockStatement(node);
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
            string triggerIdentifier = "on-" + node.Token.StartPos.ToString();
            Trigger triggerObject = _context.Triggers.GetTrigger(triggerIdentifier);
            triggerObject.SetTriggerVariable(node.Nodes[1].Nodes[0].Token.Text);

            _currentCodeSection = triggerObject.Code;
            AddOpcode(new OpcodePush(triggerObject.VariableNameOldValue));
            AddOpcode(new OpcodePush(triggerObject.VariableName));
            AddOpcode(new OpcodeCompareEqual());
            AddOpcode(new OpcodeLogicNot());
            Opcode branchOpcode = AddOpcode(new OpcodeBranchIfFalse());
            VisitNode(node.Nodes[2]);
            AddOpcode(new OpcodePush(null)).DestinationLabel = triggerObject.GetFunctionLabel();
            AddOpcode(new OpcodeRemoveTrigger());
            Opcode eofOpcode = AddOpcode(new OpcodeEOF());
            branchOpcode.DestinationLabel = eofOpcode.Label;
        }

        private void PreProcessWhenStatement(ParseNode node)
        {
            string triggerIdentifier = "when-" + node.Token.StartPos.ToString();
            Trigger triggerObject = _context.Triggers.GetTrigger(triggerIdentifier);

            _currentCodeSection = triggerObject.Code;
            VisitNode(node.Nodes[1]);
            Opcode branchOpcode = AddOpcode(new OpcodeBranchIfFalse());
            VisitNode(node.Nodes[3]);
            AddOpcode(new OpcodePush(null)).DestinationLabel = triggerObject.GetFunctionLabel();
            AddOpcode(new OpcodeRemoveTrigger());
            Opcode eofOpcode = AddOpcode(new OpcodeEOF());
            branchOpcode.DestinationLabel = eofOpcode.Label;
        }

        private void PreProcessWaitStatement(ParseNode node)
        {
            if (node.Nodes.Count == 4)
            {
                // wait condition
                string triggerIdentifier = "wait-" + node.Token.StartPos.ToString();
                Trigger triggerObject = _context.Triggers.GetTrigger(triggerIdentifier);

                _currentCodeSection = triggerObject.Code;
                VisitNode(node.Nodes[2]);
                Opcode branchOpcode = AddOpcode(new OpcodeBranchIfFalse());
                AddOpcode(new OpcodeEndWait());
                AddOpcode(new OpcodePush(null)).DestinationLabel = triggerObject.GetFunctionLabel();
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

        private void PreProcessLockStatement(ParseNode node)
        {
            string lockIdentifier = node.Nodes[1].Token.Text;
            Lock lockObject = _context.Locks.GetLock(lockIdentifier);
            int expressionHash = ConcatenateNodes(node.Nodes[3]).GetHashCode();

            if (!lockObject.IsInitialized())
            {
                // initialization code
                _currentCodeSection = lockObject.InitializationCode;
                AddOpcode(new OpcodePush(lockObject.PointerIdentifier));
                AddOpcode(new OpcodePush(null)).DestinationLabel = lockObject.DefaultLabel;
                AddOpcode(new OpcodeStore());

                if (lockObject.IsSystemLock())
                {
                    // add trigger
                    string triggerIdentifier = "lock-" + lockObject.Identifier;
                    Trigger triggerObject = _context.Triggers.GetTrigger(triggerIdentifier);

                    _currentCodeSection = triggerObject.Code;
                    AddOpcode(new OpcodePush("$" + lockObject.Identifier));
                    AddOpcode(new OpcodeCall(lockObject.PointerIdentifier));
                    AddOpcode(new OpcodeStore());
                    AddOpcode(new OpcodeEOF());
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
                //case TokenType.edit_stmt:
                //    break;
                case TokenType.run_stmt:
                    VisitRunStatement(node);
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
                case TokenType.unset_stmt:
                    VisitUnsetStatement(node);
                    break;
                case TokenType.filevol_name:
                    VisitFileVol(node);
                    break;
                case TokenType.arglist:
                    VisitArgList(node);
                    break;
                case TokenType.expr:
                case TokenType.or_expr:
                case TokenType.and_expr:
                case TokenType.arith_expr:
                case TokenType.div_expr:
                case TokenType.mult_expr:
                case TokenType.factor:
                    VisitExpression(node);
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
                default:
                    break;
            }
        }

        private void VisitChildNodes(ParseNode node)
        {
            foreach (ParseNode childNode in node.Nodes)
            {
                VisitNode(childNode);
            }
        }

        private void VisitExpression(ParseNode node)
        {
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
            if (node.Nodes.Count > 0)
            {
                bool addNegation = false;
                int nodeIndex = 0;

                if (node.Nodes[0].Token.Type == TokenType.PLUSMINUS)
                {
                    nodeIndex++;
                    if (node.Nodes[0].Token.Text == "-")
                    {
                        addNegation = true;
                    }
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
                    AddOpcode(new OpcodeLogicNot());
                }
            }
        }

        private void VisitSciNumber(ParseNode node)
        {
            if (node.Nodes.Count == 3)
            {
                //number in scientific notation
                double mantissa = double.Parse(node.Nodes[0].Nodes[0].Token.Text);
                int exponent = int.Parse(node.Nodes[2].Token.Text);
                double number = mantissa * Math.Pow(10, exponent);
                AddOpcode(new OpcodePush(number));
            }
            else
            {
                if (node.Nodes.Count == 1)
                {
                    VisitNumber(node.Nodes[0]);
                }
            }
        }

        private void VisitNumber(ParseNode node)
        {
            VisitNode(node.Nodes[0]);
        }

        private void VisitInteger(ParseNode node)
        {
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
            object number = null;
            number = double.Parse(node.Token.Text);

            if (number != null)
            {
                AddOpcode(new OpcodePush(number));
            }
        }

        private void VisitTrueFalse(ParseNode node)
        {
            bool boolValue;
            if (bool.TryParse(node.Token.Text, out boolValue))
            {
                AddOpcode(new OpcodePush(boolValue));
            }
        }

        private void VisitFunction(ParseNode node)
        {
            int parameterCount = (node.Nodes[2].Nodes.Count / 2) + 1;
            string functionName = node.Nodes[0].Token.Text;
            string overloadedFunctionName = GetFunctionOverload(functionName, parameterCount) + "()";
            VisitNode(node.Nodes[2]);
            AddOpcode(new OpcodeCall(overloadedFunctionName));
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
            VisitNode(node.Nodes[0]);
        }

        private void VisitArgList(ParseNode node)
        {
            int nodeIndex = 0;
            while (nodeIndex < node.Nodes.Count)
            {
                VisitNode(node.Nodes[nodeIndex]);
                nodeIndex += 2;
            }
        }

        private void VisitVarIdentifier(ParseNode node)
        {
            string identifier = node.Nodes[0].Token.Text;
            string concatenatedIdentifier = ConcatenateNodes(node);
            bool ignoreSuffixs = false;
            int suffixIndex = 1;
            
            // replace identifiers that look like special values but they are not
            if (_identifierReplacements.ContainsKey(concatenatedIdentifier))
            {
                identifier = _identifierReplacements[concatenatedIdentifier];
                ignoreSuffixs = true;
            }

            if (_context.Locks.Contains(identifier))
            {
                Lock lockObject = _context.Locks.GetLock(identifier);
                AddOpcode(new OpcodeCall(lockObject.PointerIdentifier));
            }
            else if (node.Nodes.Count > 1 &&
                     node.Nodes[1].Token.Type == TokenType.BRACKETOPEN)
            {
                // if a bracket follows an identifier then its a function call
                VisitFunction(node);
                suffixIndex += 3;
            }
            else
            {
                AddOpcode(new OpcodePush("$" + identifier));
            }

            if (!ignoreSuffixs && (suffixIndex < node.Nodes.Count))
            {
                // has suffixes
                int nodeIndex = suffixIndex + 1;
                while (nodeIndex < node.Nodes.Count)
                {
                    VisitNode(node.Nodes[nodeIndex]);

                    // when we are setting a member value we need to leave
                    // the last object and the last suffix in the stack
                    if (!(_compilingSetDestination &&
                          nodeIndex == (node.Nodes.Count - 1)))
                    {
                        AddOpcode(new OpcodeGetMember());
                    }
                                        
                    nodeIndex += 2;
                }
            }
        }

        private void VisitIdentifier(ParseNode node)
        {
            AddOpcode(new OpcodePush(node.Token.Text));
        }

        private void VisitString(ParseNode node)
        {
            AddOpcode(new OpcodePush(node.Token.Text.Trim('"')));
        }

        private bool VarIdentifierHasSuffixes(ParseNode node)
        {
            string concatenatedIdentifier = ConcatenateNodes(node);
            if (!_identifierReplacements.ContainsKey(concatenatedIdentifier))
            {
                foreach (ParseNode child in node.Nodes)
                {
                    if (child.Token.Type == TokenType.COLON)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void VisitSetStatement(ParseNode node)
        {
            bool settingMember = VarIdentifierHasSuffixes(node.Nodes[1]);
            // destination
            _compilingSetDestination = true;
            VisitVarIdentifier(node.Nodes[1]);
            _compilingSetDestination = false;
            // expression
            VisitNode(node.Nodes[3]);

            if (settingMember) AddOpcode(new OpcodeSetMember());
            else AddOpcode(new OpcodeStore());
        }

        private void VisitIfStatement(ParseNode node)
        {
            VisitNode(node.Nodes[1]);
            Opcode branch = AddOpcode(new OpcodeBranchIfFalse());
            VisitNode(node.Nodes[2]);
            branch.DestinationLabel = GetNextLabel(false);
            _addBranchDestination = true;
        }

        private void VisitUntilStatement(ParseNode node)
        {
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
        }

        private void VisitPlusMinus(ParseNode node)
        {
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
            AddOpcode(new OpcodeMathMultiply());
        }

        private void VisitDiv(ParseNode node)
        {
            AddOpcode(new OpcodeMathDivide());
        }

        private void VisitPower(ParseNode node)
        {
            AddOpcode(new OpcodeMathPower());
        }

        private void VisitAnd(ParseNode node)
        {
            AddOpcode(new OpcodeLogicAnd());
        }

        private void VisitOr(ParseNode node)
        {
            AddOpcode(new OpcodeLogicOr());
        }

        private void VisitComparator(ParseNode node)
        {
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
                case "=":
                    AddOpcode(new OpcodeCompareEqual());
                    break;
                default:
                    break;
            }
        }

        private void VisitLockStatement(ParseNode node)
        {
            string lockIdentifier = node.Nodes[1].Token.Text;
            int expressionHash = ConcatenateNodes(node.Nodes[3]).GetHashCode();
            Lock lockObject = _context.Locks.GetLock(lockIdentifier);

            if (lockObject.IsInitialized())
            {
                string functionLabel = lockObject.GetLockFunction(expressionHash)[0].Label;
                // lock variable
                AddOpcode(new OpcodePush(lockObject.PointerIdentifier));
                AddOpcode(new OpcodePush(null)).DestinationLabel = functionLabel;
                AddOpcode(new OpcodeStore());

                if (lockObject.IsSystemLock())
                {
                    // add update trigger
                    string triggerIdentifier = "lock-" + lockIdentifier;
                    if (_context.Triggers.Contains(triggerIdentifier))
                    {
                        Trigger triggerObject = _context.Triggers.GetTrigger(triggerIdentifier);
                        AddOpcode(new OpcodePush(null)).DestinationLabel = triggerObject.GetFunctionLabel();
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
            string lockIdentifier = node.Nodes[1].Token.Text;
            Lock lockObject = _context.Locks.GetLock(lockIdentifier);

            if (lockObject.IsInitialized())
            {
                if (lockObject.IsSystemLock())
                {
                    // disable this FlyByWire parameter
                    AddOpcode(new OpcodePush(lockIdentifier));
                    AddOpcode(new OpcodePush(false));
                    AddOpcode(new OpcodeCall("toggleflybywire()"));

                    // remove update trigger
                    string triggerIdentifier = "lock-" + lockIdentifier;
                    if (_context.Triggers.Contains(triggerIdentifier))
                    {
                        Trigger triggerObject = _context.Triggers.GetTrigger(triggerIdentifier);
                        AddOpcode(new OpcodePush(null)).DestinationLabel = triggerObject.GetFunctionLabel();
                        AddOpcode(new OpcodeRemoveTrigger());
                    }
                }

                // unlock variable
                AddOpcode(new OpcodePush(lockObject.PointerIdentifier));
                AddOpcode(new OpcodePush(null)).DestinationLabel = lockObject.DefaultLabel;
                AddOpcode(new OpcodeStore());
            }
        }

        private void VisitOnStatement(ParseNode node)
        {
            string triggerIdentifier = "on-" + node.Token.StartPos.ToString();
            Trigger triggerObject = _context.Triggers.GetTrigger(triggerIdentifier);

            if (triggerObject.IsInitialized())
            {
                AddOpcode(new OpcodePush(triggerObject.VariableNameOldValue));
                AddOpcode(new OpcodePush(triggerObject.VariableName));
                AddOpcode(new OpcodeStore());
                AddOpcode(new OpcodePush(null)).DestinationLabel = triggerObject.GetFunctionLabel();
                AddOpcode(new OpcodeAddTrigger(false));
            }
        }

        private void VisitWhenStatement(ParseNode node)
        {
            string triggerIdentifier = "when-" + node.Token.StartPos.ToString();
            Trigger triggerObject = _context.Triggers.GetTrigger(triggerIdentifier);

            if (triggerObject.IsInitialized())
            {
                AddOpcode(new OpcodePush(null)).DestinationLabel = triggerObject.GetFunctionLabel();
                AddOpcode(new OpcodeAddTrigger(false));
            }
        }

        private void VisitWaitStatement(ParseNode node)
        {
            if (node.Nodes.Count == 3)
            {
                // wait time
                VisitNode(node.Nodes[1]);
                AddOpcode(new OpcodeWait());
            }
            else
            {
                // wait condition
                string triggerIdentifier = "wait-" + node.Token.StartPos.ToString();
                Trigger triggerObject = _context.Triggers.GetTrigger(triggerIdentifier);

                if (triggerObject.IsInitialized())
                {
                    AddOpcode(new OpcodePush(null)).DestinationLabel = triggerObject.GetFunctionLabel();
                    AddOpcode(new OpcodeAddTrigger(true));
                }
            }
        }

        private void VisitDeclareStatement(ParseNode node)
        {
            if (node.Nodes.Count == 3)
            {
                // standard declare
                VisitNode(node.Nodes[1]);
                AddOpcode(new OpcodePush(0));
                AddOpcode(new OpcodeStore());
            }
            else
            {
                // declare parameters
                for (int index = node.Nodes.Count - 2; index > 1; index -= 2)
                {
                    VisitNode(node.Nodes[index]);
                    AddOpcode(new OpcodeSwap());
                    AddOpcode(new OpcodeStore());
                }
            }
        }

        private void VisitToggleStatement(ParseNode node)
        {
            VisitVarIdentifier(node.Nodes[1]);
            VisitVarIdentifier(node.Nodes[1]);
            AddOpcode(new OpcodeLogicToBool());
            AddOpcode(new OpcodeLogicNot());
            AddOpcode(new OpcodeStore());
        }

        private void VisitOnOffStatement(ParseNode node)
        {
            VisitVarIdentifier(node.Nodes[0]);
            if (node.Nodes[1].Token.Type == TokenType.ON)
                AddOpcode(new OpcodePush(true));
            else
                AddOpcode(new OpcodePush(false));
            AddOpcode(new OpcodeStore());
        }

        private void VisitPrintStatement(ParseNode node)
        {
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
            AddOpcode(new OpcodeCall("stage()"));
        }

        private void VisitAddStatement(ParseNode node)
        {
            VisitNode(node.Nodes[1]);
            AddOpcode(new OpcodeCall("add()"));
        }

        private void VisitRemoveStatement(ParseNode node)
        {
            VisitNode(node.Nodes[1]);
            AddOpcode(new OpcodeCall("remove()"));
        }

        private void VisitClearStatement(ParseNode node)
        {
            AddOpcode(new OpcodeCall("clearscreen()"));
        }

        private void VisitRunStatement(ParseNode node)
        {
            int volumeIndex = 3;

            // process program arguments
            if (node.Nodes.Count > 3 && node.Nodes[3].Token.Type == TokenType.arglist)
            {
                VisitNode(node.Nodes[3]);
                volumeIndex += 3;
            }

            // program name
            VisitNode(node.Nodes[1]);

            // volume where program should be executed (null means local)
            if (volumeIndex < node.Nodes.Count)
                VisitNode(node.Nodes[volumeIndex]);
            else
                AddOpcode(new OpcodePush(null));

            AddOpcode(new OpcodeCall("run()"));
        }

        private void VisitSwitchStatement(ParseNode node)
        {
            VisitNode(node.Nodes[2]);
            AddOpcode(new OpcodeCall("switch()"));
        }

        private void VisitCopyStatement(ParseNode node)
        {
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
            VisitNode(node.Nodes[1]);
            
            if (node.Nodes.Count == 5)
                VisitNode(node.Nodes[3]);
            else
                AddOpcode(new OpcodePush(null));

            AddOpcode(new OpcodeCall("delete()"));
        }

        private void VisitListStatement(ParseNode node)
        {
            if (node.Nodes.Count == 3)
                VisitNode(node.Nodes[1]);
            else
                AddOpcode(new OpcodePush("files"));

            AddOpcode(new OpcodeCall("list()"));
        }

        private void VisitLogStatement(ParseNode node)
        {
            VisitNode(node.Nodes[1]);
            VisitNode(node.Nodes[3]);
            AddOpcode(new OpcodeCall("log()"));
        }

        private void VisitBreakStatement(ParseNode node)
        {
            Opcode jump = AddOpcode(new OpcodeBranchJump());
            AddToBreakList(jump);
        }

        private void VisitRebootStatement(ParseNode node)
        {
            AddOpcode(new OpcodeCall("reboot()"));
        }

        private void VisitShutdownStatement(ParseNode node)
        {
            AddOpcode(new OpcodeCall("shutdown()"));
        }

        private void VisitUnsetStatement(ParseNode node)
        {
            if (node.Nodes[1].Token.Type == TokenType.ALL)
            {
                // null means all variables
                AddOpcode(new OpcodePush(null));
            }
            else
            {
                VisitVarIdentifier(node.Nodes[1]);
            }
        }
    }
}
