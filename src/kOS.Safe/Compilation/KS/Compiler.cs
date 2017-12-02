using System;
using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Exceptions;
using kOS.Safe.Utilities;
using kOS.Safe.Execution;
using kOS.Safe.Encapsulation;
using System.Text;

namespace kOS.Safe.Compilation.KS
{
    class Compiler : IExpressionVisitor
    {
        private CodePart part;
        private Context context;
        private List<Opcode> currentCodeSection;
        private bool addBranchDestination;
        private ParseNode lastNode;
        private int startLineNum;
        private short lastLine;
        private short lastColumn;
        private readonly List<BreakInfo> breakList = new List<BreakInfo>();
        private readonly List<int> returnList = new List<int>();
        private readonly List<string> triggerKeepNames = new List<string>();
        private bool nowCompilingTrigger;
        private bool nowInALoop;
        private bool needImplicitReturn;
        private bool nextBraceIsFunction;
        private bool allowLazyGlobal;
        /// <summary>Used when you want to set the next opcode's label but there's many places the next AddOpcode call might happen in the code</summary>
        private string forcedNextLabel;
        private Int16 braceNestLevel;
        private readonly List<Int16> scopeStack = new List<Int16>();
        private readonly Dictionary<ParseNode, Scope> scopeMap = new Dictionary<ParseNode, Scope>();
        private CompilerOptions options;
        private const bool TRACE_PARSE = false; // set to true to Debug Log each ParseNode as it's visited.
        private string boilerplateLoadAndRunEntryLabel;

        private enum StorageModifier {
            /// <summary>The storage will definitely be at the localmost scope.</summary>
            LOCAL,
            /// <summary>The storage will definitely be at the globalmost scope.</summary>
            GLOBAL,
            /// <summary>The storage will be whatever scope it happens to find the first hit, or global if not found.</summary>
            LAZYGLOBAL
        };
        
        // Because the Compiler object can be re-used, with its Compile()
        // method called a second time, we can't rely on the constructor or C#'s rules about default
        // variable values to guarantee these are all set properly.  They might be leftover values
        // from a previous aborted use of Compiler.Compile().  I've noticed sometimes after an
        // error I end up with the very next command always failing even when it's right, and
        // only the next command after that works right, and I suspect this was why - these
        // weren't being reset after a failed compile.
        private void InitCompileFlags()
        {
            addBranchDestination = false;
            lastNode = null;
            startLineNum = 1;
            lastLine = 0;
            lastColumn = 0;
            breakList.Clear();
            returnList.Clear();
            triggerKeepNames.Clear();
            nowCompilingTrigger = false;
            nowInALoop = false;
            needImplicitReturn = true;
            braceNestLevel = 0;
            nextBraceIsFunction = false;
            allowLazyGlobal = true;
            forcedNextLabel = String.Empty;
            scopeStack.Clear();
            scopeMap.Clear();

            // Zero-th instruction of the loader/runner that will have to be built by something
            // outside of Compiler.cs (probably ProgramBuilder.cs - look there to find it).
            boilerplateLoadAndRunEntryLabel = "@LR00";
        }

        public CodePart Compile(int startLineNum, ParseTree tree, Context context, CompilerOptions options)
        {
            InitCompileFlags();

            part = new CodePart();
            this.context = context;
            this.options = options;
            this.startLineNum = startLineNum;
            
            ++context.NumCompilesSoFar;

            if (tree.Nodes.Count > 0)
            {
                PreProcess(tree);
                CompileProgram(tree);
            }
            return part;
        }

        private void CompileProgram(ParseTree tree)
        {
            currentCodeSection = part.MainCode;
            
            VisitNode(tree.Nodes[0]);
            
            if (addBranchDestination || currentCodeSection.Count == 0)
            {
                AddOpcode(new OpcodeNOP());
            }
        }

        /// <summary>
        /// Set the current line/column info and potentially also make a helpful
        /// debug trace useful when making syntax changes.
        /// </summary>
        private void NodeStartHousekeeping(ParseNode node)
        {
            if (node == null) { throw new ArgumentNullException("node"); }

            if (TRACE_PARSE)
                SafeHouse.Logger.Log("traceParse: visiting node: " + node.Token.Type.ToString() + ", " + node.Token.Text);

            LineCol location = GetLineCol(node);
            lastLine = location.Line;
            lastColumn = location.Column;
        }
        
        /// <summary>
        /// Get a line number and column for a given parse node.  Handles the
        /// fact that TinyPG does not provide line and col information for all
        /// nodes - just the terminals.  This means if you, say, ask for the
        /// line or column of a complex node like an expression, you get the bogus answer 0,0 back
        /// from TinyPG normally.  This method performs a leftmost walk of the
        /// parse tree to get the first instance where a token exists with actual
        /// line and column information populated, and returns that.
        /// </summary>
        /// <param name="node">The node to get the line number for</param>
        /// <returns>line and column pair of the firstmost terminal within the parse node</returns>
        private LineCol GetLineCol(ParseNode node)
        {
            if (node.Token == null || node.Token.Line <= 0)
            {
                foreach (ParseNode child in node.Nodes)
                {
                    LineCol candidate = GetLineCol(child);
                    if (candidate.Line >= 0)
                        return candidate;
                }
            }

            return new LineCol( (node.Token.Line + (startLineNum - 1)), (node.Token.Column) );
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
            Opcode code = AddOpcode(opcode, string.Empty);

            if (! String.IsNullOrEmpty(forcedNextLabel))
            {
                code.Label = forcedNextLabel;
                forcedNextLabel = String.Empty;
            }
            
            return code;
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
            LowercaseConversions(rootNode);
            RearrangeParseNodes(rootNode);
            TraverseScopeBranch(rootNode);
            IterateUserFunctions(rootNode, IdentifyUserFunctions);
            PreProcessStatements(rootNode);
            IterateUserFunctions(rootNode, PreProcessUserFunctionStatement);
        }
        
        /// <summary>
        /// Lowercase every IDENTIFIER and FILEIDENT token in the parse.
        /// </summary>
        /// <param name="node">branch head to start from in the compiler</param>
        private void LowercaseConversions(ParseNode node)
        {
            switch (node.Token.Type)
            {
                case TokenType.IDENTIFIER:
                case TokenType.FILEIDENT:
                    node.Token.Text = node.Token.Text.ToLower();
                    break;
                default:
                    foreach (ParseNode child in node.Nodes)
                        LowercaseConversions(child);
                    break;
            }
        }
        
        /// <summary>
        /// Some of the parse rules in Kerboscript may be implemented on the back
        /// of other rules.  In this case all the compiler really does is just 
        /// re-arrange a more complex parse rule to be expressed in the form of
        /// building blocks made of other simpler rules before continuing the compile that way. 
        /// </summary>
        /// <param name="node">make the transformation from this point downward</param>
        private void RearrangeParseNodes(ParseNode node)
        {
            if (node.Token.Type == TokenType.fromloop_stmt) // change to switch stmt if more such rules get added later.
            {
                RearrangeLoopFromNode(node);
            }

            // Recurse children EVEN IF the node got re-arranged.  If the node got re-arranged, then its children will now look
            // different than they did before, but they still need to be iterated over to look for other rearrangements.
            // (for example, a loopfrom loop nested inside another loopfrom loop).
            foreach (ParseNode child in node.Nodes)
                RearrangeParseNodes(child);
        }
        
        private void IterateUserFunctions(ParseNode node, Action<ParseNode> action)
        {
            bool doChildren = true;
            bool doInvoke = false;
            switch (node.Token.Type)
            {
                // Any statement which might have another statement nested inside
                // it should be recursed into to check if that other statement
                // might be a lock, or a user function (anonymous or named).
                //
                // We assume by default a node should be recursed into unless explicitly
                // told otherwise here.  Doing it this way around is the safer default
                // because of what happens when we fail to mention a part of speech here.
                // If we fail to recurse when we should have, that can be fatal as it makes
                // the compiler produce wrong code.  But if we do recurse when we didn't have
                // to, that just wastes a bit of CPU time, which isn't as bad.
                //
                case TokenType.onoff_trailer:
                case TokenType.stage_stmt:
                case TokenType.clear_stmt:
                case TokenType.break_stmt:
                case TokenType.preserve_stmt:
                case TokenType.run_stmt:
                case TokenType.compile_stmt:
                case TokenType.list_stmt:
                case TokenType.reboot_stmt:
                case TokenType.shutdown_stmt:
                case TokenType.unset_stmt:
                case TokenType.sci_number:
                case TokenType.number:
                case TokenType.INTEGER:
                case TokenType.DOUBLE:
                case TokenType.PLUSMINUS:
                case TokenType.MULT:
                case TokenType.DIV:
                case TokenType.POWER:
                case TokenType.IDENTIFIER:
                case TokenType.FILEIDENT:
                case TokenType.STRING:
                case TokenType.TRUEFALSE:
                case TokenType.COMPARATOR:
                case TokenType.AND:
                case TokenType.OR:
                case TokenType.directive:
                    doChildren = false;
                    break;                

                // These are the statements we're searching for to work on here:
                //
                case TokenType.declare_stmt: // for DECLARE FUNCTION's.
                case TokenType.instruction_block: // just in case it's an anon function's body
                    doInvoke = true;
                    break;
                default:
                    break;

            }
            // for catching functions nested inside functions, or locks nested inside functions:
            // Depth-first: Walk my children first, then iterate through me.  Thus the functions nested inside
            // me have already been compiled before I start compiling my own code.  This allows my code to make
            // forward-calls into my nested functions, because they've been compiled and we know where they live
            // in memory now.
            if (doChildren)
                foreach (ParseNode childNode in node.Nodes)
                    IterateUserFunctions(childNode, action);
            if (doInvoke)
                action.Invoke(node);
        }

        /// <summary>
        /// Edit the parse branch for a loopfrom statement, rearranging its component
        /// parts into a simpler unrolled form.<br/>
        /// When given this rule:<br/>
        /// <br/>
        /// FROM {(init statements)} UNTIL expr STEP {(inc statements)} DO {(body statements)} <br/>
        /// <br/>
        /// It will edit its own child nodes and transform them into a new parse tree branch as if this had
        /// been what was in the source code instead:<br/>
        /// <br/>
        /// { (init statements) UNTIL expr { (body statements) (inc statements) } }<br/>
        /// <br/>
        /// Thus any variables declared inside (init statements) are in scope during the body of the loop.<br/>
        /// The actual logic of doing an UNTIL loop will fall upon VisitUntilNode to deal with later in the compile.<br/>
        /// </summary>
        /// <param name="node"></param>
        private void RearrangeLoopFromNode(ParseNode node)
        {
            // Safety check to see if I've already been rearranged into my final form, just in case
            // the recursion logic is messed up and this gets called twice on the same node:
            if (node.Nodes.Count == 1 && node.Nodes[0].Token.Type == TokenType.instruction_block)
                return;
            
            // ReSharper disable RedundantDefaultFieldInitializer
            ParseNode initBlock = null;
            ParseNode checkExpression = null;
            ParseNode untilTokenNode = null;
            ParseNode stepBlock = null;
            ParseNode doBlock = null;
            // ReSharper enable RedundantDefaultFieldInitializer
            
            for( int index = 0 ; index < node.Nodes.Count - 1 ; index += 2 )
            {
                switch (node.Nodes[index].Token.Type)
                {
                    case TokenType.FROM:
                        initBlock = node.Nodes[index+1];
                        break;
                    case TokenType.UNTIL:
                        untilTokenNode = node.Nodes[index];
                        checkExpression = node.Nodes[index+1];
                        break;
                    case TokenType.STEP:
                        stepBlock = node.Nodes[index+1];
                        break;
                    case TokenType.DO:
                        doBlock = node.Nodes[index+1];
                        break;
                    // no default because anything else is a syntax error and it won't even get as far as this method in that case.
                }
            }
            
            // These probably can't happen because the parser would have barfed before it got to this method:
            if (initBlock == null)
                throw new KOSCompileException(node.Token, "Missing FROM block in FROM loop.");
            if (checkExpression == null || untilTokenNode == null)
                throw new KOSCompileException(node.Token, "Missing UNTIL check expression in FROM loop.");
            if (stepBlock == null)
                throw new KOSCompileException(node.Token, "Missing STEP block in FROM loop.");
            if (doBlock == null)
                throw new KOSCompileException(node.Token, "Missing loop body (DO block) in FROM loop.");
            
            // Append the step instructions to the tail end of the body block's instructions:
            foreach (ParseNode child in stepBlock.Nodes)
                doBlock.Nodes.Add(child);
            
            // Make a new empty until loop node, which will get added to the init block eventually:
            var untilStatementTok = new Token
            {
                Type = TokenType.until_stmt,
                Line = untilTokenNode.Token.Line,
                Column = untilTokenNode.Token.Column,
                File = untilTokenNode.Token.File
            };

            ParseNode untilNode = initBlock.CreateNode(untilStatementTok, untilStatementTok.ToString());

            // (The direct manipulation of the tree's parent pointers, seen below, is bad form,
            // but TinyPg doesn't seem to have given us good primitives to append an existing node to the tree to do it for us.
            // CreateNode() makes a brand new empty node attached to the parent, but there seems to be no way to take an
            // existing node and attach it elsewhere without directly changing the Parent property as seen in the lines below:)

            // Populate that until loop node with the parts from this rule:
            untilNode.Nodes.Add(untilTokenNode); untilTokenNode.Parent = untilNode;
            untilNode.Nodes.Add(checkExpression); checkExpression.Parent = untilNode;
            untilNode.Nodes.Add(doBlock); doBlock.Parent = untilNode;
            
            // And now append that until loop to the tail end of the init block:
            initBlock.Nodes.Add(untilNode); // parent already assigned by initBlock.CreateNode() above.
            
            // The init block is now actually the entire loop, having been exploded and unrolled into its
            // new form, make that be our only node:
            node.Nodes.Clear();
            node.Nodes.Add(initBlock);  // initBlock's parent already points at node to begin with.
            
            // The FROM loop node is still in the parent's list, but it contains this new rearranged sub-tree
            // instead of its original.
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
                case TokenType.fromloop_stmt:
                case TokenType.until_stmt:
                case TokenType.for_stmt:
                case TokenType.declare_function_clause:
                case TokenType.declare_stmt:
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
            nextBraceIsFunction = true; // triggers aren't really functions but act like it a lot.

            int expressionHash = ConcatenateNodes(node).GetHashCode();
            string triggerIdentifier = "on-" + expressionHash.ToString();
            Trigger triggerObject = context.Triggers.GetTrigger(triggerIdentifier);

            // - - - - - - - - - - - - 
            // TODO: If we ever implement triggers that can be named and cancelled by name later,
            // then right here we'd need to add some opcodes that implement the same logic as
            // the cooked steering triggers use (check OpcodeTestCancelled and premature return
            // if this trigger has been cancelled elsewhere before we begin running it.)
            // - - - - - - - - - - - - 

            currentCodeSection = triggerObject.Code;
            // Put the old value on top of the stack for equals comparison later:
            AddOpcode(new OpcodePush(triggerObject.OldValueIdentifier));
            AddOpcode(new OpcodeEval());
            // eval the expression for the new value, and leave it on the stack twice.
            VisitExpression(node.Nodes[1]);
            AddOpcode(new OpcodeEval());
            AddOpcode(new OpcodeDup());
            // Put one of those two copies of the new value into the old value identifier for next time:
            AddOpcode(new OpcodeStoreGlobal(triggerObject.OldValueIdentifier));
            // Use the other dup'ed copy of the new value to actually do the equals
            // comparison with the old value that's still under it on the stack:
            AddOpcode(new OpcodeCompareEqual());
            OpcodeBranchIfFalse branchToBody = new OpcodeBranchIfFalse();
            branchToBody.Distance = 3;
            AddOpcode(branchToBody);
            AddOpcode(new OpcodePush(true));       // wasn't triggered yet, so preserve.
            AddOpcode(new OpcodeReturn((short)0)); // premature return because it wasn't triggered

            // make flag that remembers whether to remove trigger:
            // defaults to true = removal should happen.
            string triggerKeepName = "$keep-" + triggerIdentifier;
            PushTriggerKeepName(triggerKeepName);
            AddOpcode(new OpcodePush(false));
            AddOpcode(new OpcodeStoreGlobal(triggerKeepName));

            VisitNode(node.Nodes[2]);

            // PRESERVE will determine whether or not the trigger returns true (true means
            // re-enable the trigger upon exit.)
            PopTriggerKeepName();
            AddOpcode(new OpcodePush(triggerKeepName));
            AddOpcode(new OpcodeReturn((short)0));
            
            nextBraceIsFunction = false;
        }

        private void PreProcessWhenStatement(ParseNode node)
        {
            NodeStartHousekeeping(node);
            nextBraceIsFunction = true; // triggers aren't really functions but act like it a lot.
            
            int expressionHash = ConcatenateNodes(node).GetHashCode();
            string triggerIdentifier = "when-" + expressionHash.ToString();
            Trigger triggerObject = context.Triggers.GetTrigger(triggerIdentifier);

            // - - - - - - - - - - - - 
            // TODO: If we ever implement triggers that can be named and cancelled by name later,
            // then right here we'd need to add some opcodes that implement the same logic as
            // the cooked steering triggers use (check OpcodeTestCancelled and premature return
            // if this trigger has been cancelled elsewhere before we begin running it.)
            // - - - - - - - - - - - - 

            currentCodeSection = triggerObject.Code;
            VisitExpression(node.Nodes[1]);
            OpcodeBranchIfTrue branchToBody = new OpcodeBranchIfTrue();
            branchToBody.Distance = 3;
            AddOpcode(branchToBody);
            AddOpcode(new OpcodePush(true));       // wasn't triggered yet, so preserve.
            AddOpcode(new OpcodeReturn((short)0)); // premature return because it wasn't triggered

            // make flag that remembers whether to remove trigger:
            // defaults to true = removal should happen.
            string triggerKeepName = "$keep-" + triggerIdentifier;
            PushTriggerKeepName(triggerKeepName);
            AddOpcode(new OpcodePush(false));
            AddOpcode(new OpcodeStoreGlobal(triggerKeepName));

            VisitNode(node.Nodes[3]);

            // PRESERVE will determine whether or not the trigger returns true (true means
            // re-enable the trigger upon exit.)
            PopTriggerKeepName();
            AddOpcode(new OpcodePush(triggerKeepName));
            AddOpcode(new OpcodeReturn((short)0));
            nextBraceIsFunction = false;
        }

        /// <summary>
        /// Create a unique string out of a sub-branch of the parse tree that
        /// can be used to uniquely identify it.  The purpose is so that two
        /// sub-branches of the parse tree can be compared to see if they are 
        /// the exact same code as each other.
        /// </summary>
        private string ConcatenateNodes(ParseNode node)
        {
            LineCol whereNodeIs = GetLineCol(node);
            return string.Format("{0}L{1}C{2}{3}{4}",
                                 context.NumCompilesSoFar,
                                 whereNodeIs.Line,
                                 whereNodeIs.Column,
                                 GetContainingScopeId(node),
                                 ConcatenateNodesRecurse(node));
        }
        
        private string ConcatenateNodesRecurse(ParseNode node)
        {
            string concatenated = string.Format("{0}{1}", context.NumCompilesSoFar, node.Token.Text);

            if (node.Nodes.Any())
            {
                return node.Nodes.Aggregate(concatenated, (current, childNode) => current + ConcatenateNodesRecurse(childNode));
            }

            return concatenated;
        }

        private void IdentifyUserFunctions(ParseNode node)
        {
            if (node.Nodes.Count <= 0 )
                return;

            string funcIdentifier;
            StorageModifier storageType = GetStorageModifierFor(node);
            ParseNode bodyNode;
            
            ParseNode lastSubNode = node.Nodes[node.Nodes.Count-1];
            bool isLock = IsLockStatement(node);
            if (isLock)
            {
                funcIdentifier = lastSubNode.Nodes[1].Token.Text;
                bodyNode = lastSubNode.Nodes[3];
            }
            else if (IsDefineFunctionStatement(node))
            {
                funcIdentifier = lastSubNode.Nodes[1].Token.Text;
                bodyNode = lastSubNode.Nodes[2];
            }
            else
                return; // not one of the types of statement we're really meant to run IdentifyUserFunctions on.
            
            UserFunction userFuncObject =
                context.UserFunctions.GetUserFunction(funcIdentifier, storageType == StorageModifier.GLOBAL ? (Int16)0 : GetContainingScopeId(node), node);
            int expressionHash = ConcatenateNodes(bodyNode).GetHashCode();
            userFuncObject.GetUserFunctionOpcodes(expressionHash);
            userFuncObject.IsFunction = !isLock;
            if (userFuncObject.IsSystemLock())
                BuildSystemTrigger(userFuncObject);
        }
        
        /// <summary>
        /// Walk up the parent chain finding the first instance of a
        /// ParseNode for which a scope ID has been assigned to it, and
        /// return that scope ID.  Returns 0 (the global scope) when no
        /// hit was found.
        /// <br/>
        /// That will usually be the containing instruction_block braces,
        /// but might not be, if dealing with file scoping, or the 
        /// hidden extra scopes of FOR loops and so on.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private Int16 GetContainingScopeId(ParseNode node)
        {
            ParseNode current = node;
            while (current != null)
            {
                Scope hitScope;
                if (scopeMap.TryGetValue(current, out hitScope))
                    return hitScope.ScopeId;
                current = current.Parent;
            }
            return 0;
        }
        
        /// <summary>
        /// Much like UserFunctionCollection.GetUserFunction(), except that it won't
        /// generate a function if one doesn't exist.  Instead it will try to walk up
        /// the parent scopes until it finds a func or lock with the given name.  If it 
        /// cannot find an existing function, it will return null rather than make one.
        /// </summary>
        /// <param name="funcIdentifier">identifier for the lock or function</param>
        /// <param name="node">ParseNode to begin looking from (for scope reasons).  It will walk the parents looking for scopes that have the ident.</param>
        /// <returns>The found UserFunction, or null if none found</returns>
        private UserFunction FindExistingUserFunction(string funcIdentifier, ParseNode node)
        {
            for (ParseNode containingNode = node ; containingNode != null ; containingNode = containingNode.Parent)
            {
                Int16 thisNodeScope = GetContainingScopeId(containingNode);
                if (context.UserFunctions.Contains(funcIdentifier, thisNodeScope))
                    return context.UserFunctions.GetUserFunction(funcIdentifier, thisNodeScope, containingNode);
            }
            return null;
        }
        
        private bool IsLockStatement(ParseNode node)
        {
            return
                node.Token.Type == TokenType.declare_stmt &&
                node.Nodes[node.Nodes.Count-1].Token.Type == TokenType.declare_lock_clause;
        }
        
        private bool IsDefineFunctionStatement(ParseNode node)
        {
            if (node.Nodes.Count > 0)
            {
                ParseNode lastSubNode = node.Nodes[node.Nodes.Count-1];
                if (lastSubNode.Token.Type == TokenType.declare_function_clause)
                    return true;
            }
            return false;
        }
        
        private bool IsInsideDefineFunctionStatement(ParseNode node)
        {
            while (node != null)
            {
                if (IsDefineFunctionStatement(node))
                    return true;
                node = node.Parent;
            }
            return false;
        }
        
        // This is actually used for BOTH LOCK expressions and DEFINE FUNCTIONs, as they
        // both end up creating, effectively, a user function.
        private void PreProcessUserFunctionStatement(ParseNode node)
        {
            NodeStartHousekeeping(node);

            // The name of the lock or function to be executed:
            string userFuncIdentifier;
            // The syntax node for the body of the lock or function: the stuff it actually executes.
            ParseNode bodyNode;

            bool isLock = IsLockStatement(node);
            bool isDefFunc = IsDefineFunctionStatement(node);
            bool needImplicitArgBottom = false;
            StorageModifier storageType = GetStorageModifierFor(node);

            ParseNode lastSubNode = node.Nodes[node.Nodes.Count-1];
            if (isLock)
            {
                userFuncIdentifier = lastSubNode.Nodes[1].Token.Text; // The IDENT of: LOCK IDENT TO EXPR.
                bodyNode = lastSubNode.Nodes[3]; // The EXPR of: LOCK IDENT TO EXPR.
                needImplicitArgBottom = true;
            }
            else if (isDefFunc)
            {
                userFuncIdentifier = lastSubNode.Nodes[1].Token.Text; // The IDENT of: DEFINE FUNCTION IDENT INSTRUCTION_BLOCK.
                bodyNode = lastSubNode.Nodes[2]; // The INSTRUCTION_BLOCK of: DEFINE FUNCTION IDENT INSTRUCTION_BLOCK.
            }
            else
                return; // Should only be the case when scanning elements for anonymous functions

            UserFunction userFuncObject = context.UserFunctions.GetUserFunction(
                userFuncIdentifier,
                (storageType == StorageModifier.GLOBAL ? (Int16)0 : GetContainingScopeId(node)),
                node );
            int expressionHash = ConcatenateNodes(bodyNode).GetHashCode();

            needImplicitReturn = true; // Locks always need an implicit return.  Functions might not if all paths have an explicit one.
            
            // Both locks and functions also get an identifier storing their
            // destination instruction pointer, but the means of doing so
            // is slightly different.  Locks always need a dummy do-nothing
            // function to exist at first, which then can get replaced later
            // when the statement containing the lock definition is encountered.
            // Whereas, function bodies don't get overwritten like that.  They
            // exist exactly once, and can be "forward" called from higher up in
            // the same scope so they get assigned when the scope is first opened.
            //
            if (isLock && !userFuncObject.IsInitialized())
            {
                currentCodeSection = userFuncObject.InitializationCode;

                if (userFuncObject.IsSystemLock())
                {
                    AddOpcode(new OpcodePush(userFuncObject.ScopelessPointerIdentifier));
                    AddOpcode(new OpcodeExists());
                    var branch = new OpcodeBranchIfTrue();
                    branch.Distance = 4;
                    AddOpcode(branch);
                    AddOpcode(new OpcodePushRelocateLater(null), userFuncObject.DefaultLabel);
                    AddOpcode(new OpcodeStore(userFuncObject.ScopelessPointerIdentifier));
                }
                else
                {
                    // initialization code - unfortunately the lock implementation presumed global namespace
                    // and insisted on inserting an initialization block in front of the entire program to set up
                    // the GLOBAL lock value.  This assumption was thorny to remove, so for now, we'll make the init
                    // code consist of a dummy NOP until a better solution can be found.  Note this does put a NOP
                    // into the code PER LOCK.  Which is silly.  It's because lockObject.IsInitialized() doesn't
                    // know how to tell the difference between initialization code that's deliberately empty versus
                    // initialization code being empty because the lock has never been set up properly yet.
                    AddOpcode(new OpcodeNOP());
                }

                // build default dummy function to be used when this is a LOCK:
                currentCodeSection = userFuncObject.GetUserFunctionOpcodes(0);
                AddOpcode(new OpcodeArgBottom()).Label = userFuncObject.DefaultLabel;;
                AddOpcode(new OpcodePush("$" + userFuncObject.ScopelessIdentifier));
                AddOpcode(new OpcodeReturn(0));
            }

            // lock expression's or function body's code
            currentCodeSection = userFuncObject.GetUserFunctionOpcodes(expressionHash);
            bool secondInstanceSameLock = currentCodeSection.Count > 0;
            if (! secondInstanceSameLock)
            {
                forcedNextLabel = userFuncObject.GetUserFunctionLabel(expressionHash);

                if (isLock) // locks need to behave as if they had braces even though they don't - so they get lexical scope ids for closure reasons:
                    BeginScope(bodyNode);
                if (isDefFunc)
                    nextBraceIsFunction = true;
                if (needImplicitArgBottom)
                    AddOpcode(new OpcodeArgBottom());

                if (isLock)
                {
                    VisitExpression(bodyNode);
                }
                else
                {
                    VisitNode(bodyNode);
                }

                Int16 implicitReturnScopeDepth = 0;
                
                if (isDefFunc)
                    nextBraceIsFunction = false;
                if (isLock) // locks need to behave as if they had braces even though they don't - so they get lexical scope ids for closure reasons:
                {
                    EndScope(bodyNode, false);
                    implicitReturnScopeDepth = 1;
                }

                if (needImplicitReturn)
                {
                    if (isDefFunc)
                        AddOpcode(new OpcodePush(0)); // Functions must push a dummy return val when making implicit returns. Locks already leave an expr atop the stack.
                    AddOpcode(new OpcodeReturn(implicitReturnScopeDepth));
                }
                userFuncObject.ScopeNode = GetContainingBlockNode(node); // This limits the scope of the function to the instruction_block the DEFINE was in.
                userFuncObject.IsFunction = !(isLock);
            }
        }
        
        
        /// <summary>
        /// Build the system trigger to go with a user function (lock)
        /// such as LOCK STEERING or LOCK THROTTLE
        /// </summary>
        /// <param name="func">Represents the lock object, which might not be fully populated yet.</param>
        private void BuildSystemTrigger(UserFunction func)
        {
            string triggerIdentifier = "lock-" + func.ScopelessIdentifier;
            Trigger triggerObject = context.Triggers.GetTrigger(triggerIdentifier);
            
            if (triggerObject.IsInitialized())
                return;

            short rememberLastLine = lastLine;
            lastLine = -1; // special flag telling the error handler that these opcodes came from the system itself, when reporting the error
            List<Opcode> rememberCurrentCodeSection = currentCodeSection;
            currentCodeSection = triggerObject.Code;

            // Premature return if someone cancelled this trigger from
            // within another trigger that fired in the same physics tick:
            AddOpcode(new OpcodeTestCancelled());
            OpcodeBranchIfFalse proceedOkay = new OpcodeBranchIfFalse();
            proceedOkay.Distance = 3;
            AddOpcode(proceedOkay);
            AddOpcode(new OpcodePush(false)); // Return false. Don't preserve this trigger.
            AddOpcode(new OpcodeReturn((short)0));

            // Main body:
            AddOpcode(new OpcodePush(new KOSArgMarkerType())); // need these for all locks now.
            AddOpcode(new OpcodeCall(func.ScopelessPointerIdentifier));
            AddOpcode(new OpcodeStoreGlobal("$" + func.ScopelessIdentifier));
            AddOpcode(new OpcodePush(true)); // always preserve this particular kind of trigger.
            AddOpcode(new OpcodeReturn((short)0));
            lastLine = rememberLastLine;
            currentCodeSection = rememberCurrentCodeSection;
        }

        /// <summary>
        /// Get the instruction_block (or file Start block at outermost level) this node is immediately inside of.
        /// Gives a null if the node isn't in one (it's global).
        /// </summary>
        private ParseNode GetContainingBlockNode(ParseNode node)
        {
            while (node != null && 
                   node.Token.Type != TokenType.instruction_block &&
                   node.Token.Type != TokenType.Start)
                node = node.Parent;
            return node;
        }

        private void PushTriggerKeepName(string newLabel)
        {
            triggerKeepNames.Add(newLabel);
            nowCompilingTrigger = true;
        }

        private string PeekTriggerKeepName()
        {
            return nowCompilingTrigger ? triggerKeepNames[triggerKeepNames.Count - 1] : string.Empty;
        }

        private string PopTriggerKeepName()
        {
            // Will throw exception if list is empty, but that "should
            // never happen" as pushes and pops should be balanced in
            // the compiler's code.  If it throws exception we want to
            // let the exception happen to highlight the bug
            string returnVal = triggerKeepNames[triggerKeepNames.Count - 1];
            triggerKeepNames.RemoveAt(triggerKeepNames.Count - 1);
            nowCompilingTrigger = (triggerKeepNames.Count > 0);
            return returnVal;
        }

        private void PushBreakList(int nestLevel)
        {
            breakList.Add(new BreakInfo(nestLevel));
        }

        private void AddToBreakList(Opcode opcode)
        {
            if (breakList.Count > 0)
            {
                BreakInfo list = breakList[breakList.Count - 1];
                list.Opcodes.Add(opcode);
            }
        }
        
        private void PopBreakList(string label)
        {
            if (breakList.Count > 0)
            {
                BreakInfo list = breakList[breakList.Count - 1];
                if (list == null) return;

                breakList.Remove(list);
                foreach (Opcode opcode in list.Opcodes)
                {
                    OpcodePopScope popScopeOp = opcode as OpcodePopScope;
                    if (popScopeOp != null)
                        // calculate how many nesting levels it needs to really pop
                        // by comparing the nest level where the break statement was to
                        // the nest level where the break context started:
                        popScopeOp.NumLevels = (Int16)(popScopeOp.NumLevels - list.NestLevel);

                    else // assume all others are branch opcodes of some sort:
                        opcode.DestinationLabel = label;
                }
            }
        }
        
        private void PushReturnList()
        {
            returnList.Add(braceNestLevel);
        }
        
        private void PopReturnList()
        {
            if (returnList.Count > 0)
                returnList.RemoveAt(returnList.Count-1);
        }
        
        private int GetReturnNestLevel()
        {
            return (returnList.Count > 0) ? returnList.Last() : -1;
        }
        
        /// <summary>
        /// Insert the Opcode to start a new lexical scope, handling the parent id mapping.
        /// Call upon every open brace "{"
        /// </summary>
        private void BeginScope(ParseNode node)
        {
            // walk up parse tree until a node with a scope is found:
            while (node != null && !scopeMap.ContainsKey(node))
                node = node.Parent;

            // defaults if the node isn't found:
            Int16 scopeId = 0;
            Int16 parentScopeId = 0;
            braceNestLevel = 0;
            
            if (node != null)
            {
                Scope thisScope = scopeMap[node];
                scopeId = thisScope.ScopeId;
                parentScopeId = thisScope.ParentScopeId;
                braceNestLevel = thisScope.NestDepth;
            }
            AddOpcode(new OpcodePushScope(scopeId, parentScopeId));
        }
        
        /// <summary>
        /// Insert the Opcode to finish a lexical scope
        /// Call upon every close brace "}"
        /// <param name="withPopScope">Should this code insert its own popscope.  Only say false when
        /// you intend to immediately do a return statement and have the return statement be
        /// responsible for the popscope itself.</param>
        /// </summary>
        private void EndScope(ParseNode node, bool withPopScope = true)
        {
            node = node.Parent;

            // Walk up parse tree starting with my parent, until a node with a scope is found.
            // The goal here is to get the scope one level outside the current scope.
            while (node != null && ! scopeMap.ContainsKey(node))
                node = node.Parent;
            if (node != null)
            {
                Scope thisScope = scopeMap[node];
                braceNestLevel = thisScope.NestDepth;
            }
            else
            {
                braceNestLevel = 0;
            }

            if (withPopScope)
                AddOpcode(new OpcodePopScope());
        }
        
        /// <summary>
        /// Because the compile occurs a bit out of order (doing the most deeply nested function
        /// first, then working out from there) it walks the scope nesting in the wrong order. 
        /// Therefore before doing the compile, run through in one pass just recording the nesting
        /// levels and lexical parent tree of the scoping before we begin, so we can
        /// use that information later in the parse:
        /// </summary>
        /// <param name="node"></param>
        private void TraverseScopeBranch(ParseNode node)
        {
            switch (node.Token.Type)
            {
                // List all the types of parse node that open a new variable scope here:
                // ---------------------------------------------------------------------
                case TokenType.Start: // Here because all programs start with an outer scope block
                case TokenType.for_stmt: // Here because it wraps the body inside an outer scope that holds the for-iterator variable.
                case TokenType.declare_lock_clause: // here because the lock body needs a scope in order to work with closures.  The scope remembers the lexical id.
                case TokenType.instruction_block:

                    ++braceNestLevel;
                    Int16 parentId = ( (scopeStack.Count == 0) ? (Int16)0 : scopeStack.Last() );
                    scopeStack.Add(++context.MaxScopeIdSoFar);
                    ParseNode mapNode = node;
                    if (node.Token.Type == TokenType.declare_lock_clause)
                    {
                        // use the expression of: LOCK foo TO expr EOI as the holder of the scope,
                        // not the lock statement itself.  Thus the foo being locked is in the outer
                        // scope and only the expression is in a nested scope:
                        mapNode = node.Nodes[node.Nodes.Count - 2];
                    }
                    scopeMap[mapNode] = new Scope(context.MaxScopeIdSoFar, parentId, braceNestLevel);
                    
                    foreach (ParseNode childNode in mapNode.Nodes)
                        TraverseScopeBranch(childNode);
                    
                    --braceNestLevel;
                    if (scopeStack.Count > 0)
                        scopeStack.RemoveAt(scopeStack.Count-1);
                    break;
                    
                // Some Compiler directives affect variable scope rules:
                case TokenType.lazyglobal_directive:
                    VisitLazyGlobalDirective(node);
                    break;
                    
                default:
                    foreach (ParseNode childNode in node.Nodes)
                          TraverseScopeBranch(childNode);
                    break;                    
            }
        }
        
        private void VisitNode(ParseNode node)
        {
            lastNode = node;

            NodeStartHousekeeping(node);

            switch (node.Token.Type)
            {
                case TokenType.Start:
                    VisitStartStatement(node);
                    break;
                case TokenType.instruction:
                    VisitChildNodes(node);
                    break;
                case TokenType.instruction_block:
                    VisitInstructionBlock(node);
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
                case TokenType.fromloop_stmt:
                    VisitChildNodes(node); // The loopfrom should have been altered by now, in RearrangeParseNodes().
                    break;
                case TokenType.return_stmt:
                    VisitReturnStatement(node);
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
                case TokenType.runpath_stmt:
                case TokenType.runoncepath_stmt:
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
                case TokenType.identifier_led_stmt:
                    VisitIdentifierLedStatement(node);
                    break;
                case TokenType.identifier_led_expr:
                    VisitIdentifierLedExpression(node);
                    break;
                case TokenType.directive:
                    VisitDirective(node);
                    break;
                case TokenType.EOF:
                    break;
                default:
                    throw new KOSYouShouldNeverSeeThisException("Unknown token: " + node.Token.Type);
            }
        }
        
        private void VisitStartStatement(ParseNode node)
        {
            int argbottomSpot = (options.IsCalledFromRun) ? FindArgBottomSpot(node) : -1;

            NodeStartHousekeeping(node);
            BeginScope(node);

            AddFunctionJumpVars(node, true);

            // For each child node, but interrupting for the spot
            // where to insert the argbottom opcode:
            for (int i = 0 ; i < node.Nodes.Count ; ++i)
            {
                if (i == argbottomSpot)
                    AddOpcode(new OpcodeArgBottom());
                
                VisitNode(node.Nodes[i]); // nextBraceIsFunction state would get incorrectly inherited by my children here if it wasn't turned off up above.
            }

            EndScope(node);
        }

        private void VisitChildNodes(ParseNode node)
        {
            NodeStartHousekeeping(node);
            foreach (ParseNode childNode in node.Nodes)
            {
                VisitNode(childNode);
            }
        }

        // For the case where you wish to eval the args lastmost-first, such
        // that they'll push onto the stack like so:
        //   arg1 <-- top
        //   arg2
        //   arg3 <-- bottom
        //
        // instead of the usual stack order of:
        //   arg3 <-- top
        //   arg2
        //   arg1 <-- bottom
        private void VisitArgListReversed(ParseNode node)
        {
            NodeStartHousekeeping(node);
            int nodeIndex = node.Nodes.Count - 1;
            while (nodeIndex >= 0)
            {
                VisitExpression(node.Nodes[nodeIndex]);
                nodeIndex -= 2;
            }
        }

        private string GetIdentifierText(ParseNode node)
        {
            //Prevent recursing through parenthesized sub-expressions:
            if (node.Token.Type == TokenType.expr)
                return string.Empty;

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
        /// Get the User function with the given the identifier, performing a
        /// scope walk from here up to the root of the parse tree until a hit
        /// is seen.  If none are seen, then return null.  This can only "see" 
        /// the functions that are defined in this same compile, not ones from
        /// other scripts this script ran.
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="node"></param>
        /// <returns></returns>
        private UserFunction GetUserFunctionWithScopeWalk(string identifier, ParseNode node)
        {
            ParseNode current = node;
            while (current != null)
            {
                Scope nodeScope;
                if (scopeMap.TryGetValue(current, out nodeScope))
                    if (context.UserFunctions.Contains(identifier, nodeScope.ScopeId))
                        return context.UserFunctions.GetUserFunction(identifier, nodeScope.ScopeId, current);
                current = current.Parent;
            }
            
            // One more try at global scope:
            if (context.UserFunctions.Contains(identifier, 0))
                return context.UserFunctions.GetUserFunction(identifier, 0, node);

            // Okay give up then:
            return null;
        }

        private void VisitSetStatement(ParseNode node)
        {
            NodeStartHousekeeping(node);
            ProcessSetOperation(
                ExpressionBuilder.BuildExpression(node.Nodes[1]),
                ExpressionBuilder.BuildExpression(node.Nodes[3])
            );
        }

        /// <summary>
        /// For any statement of the form "SET THIS TO THAT", or "THIS ON" or "THIS OFF".
        /// </summary>
        /// <param name="setThis">The lefthand-side expression to be set</param>
        /// <param name="toThis">The righthand-side expression to set it to</param>
        private void ProcessSetOperation(ExpressionNode setThis, ExpressionNode toThis)
        {
            if (setThis is IdentifierAtomNode)
            {
                // set identifier to expr.

                string identifier = ((IdentifierAtomNode)setThis).Identifier;

                // if this is a locked value, unlock it
                UserFunction userFunc = GetUserFunctionWithScopeWalk(identifier, setThis.ParseNode);
                if (userFunc != null)
                {
                    UnlockIdentifier(userFunc);
                }

                toThis.Accept(this);

                if (allowLazyGlobal)
                    AddOpcode(new OpcodeStore("$" + identifier));
                else
                    AddOpcode(new OpcodeStoreExist("$" + identifier));
            }
            else if (setThis is GetIndexNode)
            {
                // set base[index] to expr.

                GetIndexNode getIndex = (GetIndexNode)setThis;

                getIndex.Base.Accept(this);
                getIndex.Index.Accept(this);
                toThis.Accept(this);

                AddOpcode(new OpcodeSetIndex());
            }
            else if (setThis is GetSuffixNode)
            {
                // set base:suffix to expr.

                GetSuffixNode getSuffix = (GetSuffixNode)setThis;

                getSuffix.Base.Accept(this);
                toThis.Accept(this);

                AddOpcode(new OpcodeSetMember(getSuffix.Suffix));
            }
            else
            {
                throw new KOSCompileException(setThis.ParseNode.Token, "Invalid set destination");
            }
        }

        private void VisitIfStatement(ParseNode node)
        {
            NodeStartHousekeeping(node);
            // The IF check:
            VisitExpression(node.Nodes[1]);
            Opcode branchToFalse = AddOpcode(new OpcodeBranchIfFalse());
            // The IF BODY:
            VisitNode(node.Nodes[2]);
            if (node.Nodes.Count < 5)
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
                // The else body might be at index 4 or 5 depending on if there was an optional EOI.
                int elseBodyIndex = node.Nodes[4].Token.Type == TokenType.ELSE ? 5 : 4;
                VisitNode(node.Nodes[elseBodyIndex]);
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
            PushBreakList(braceNestLevel);
            VisitExpression(node.Nodes[1]);
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

        private void VisitInstructionBlock(ParseNode node)
        {
            NodeStartHousekeeping(node);
            BeginScope(node);

            // Ensure the flag doesn't stay on for inner braces unless they too are really functions and turn it back on:
            bool nextBraceWasFunction = nextBraceIsFunction;
            nextBraceIsFunction = false;

            if (nextBraceWasFunction)
                PushReturnList();
            
            AddFunctionJumpVars(node, false);
            
            int argbottomSpot = -1;
            if (nextBraceWasFunction)
                argbottomSpot = FindArgBottomSpot(node);

            // For each child node, but interrupting for the spot
            // where to insert the argbottom opcode:
            for (int i = 1 ; i < node.Nodes.Count - 1 ; ++i)
            {
                if (i == argbottomSpot)
                    AddOpcode(new OpcodeArgBottom());
                
                VisitNode(node.Nodes[i]); // nextBraceIsFunction state would get incorrectly inherited by my children here if it wasn't turned off up above.
            }

            if (nextBraceWasFunction)
                PopReturnList();

            EndScope(node);
        }
        
        /// <summary>
        /// Checks whether or not the given ParseNode contains a declare parameter statment
        /// inside it that pertains to this function.  Note, parameters inside functions
        /// nested inside the current one don't count.  This method performs a recursive
        /// walk.
        /// <br/><br/>
        /// While it does this walk, it also tests for whether or not there exists a
        /// non-defaulted parameter after a defaulted one, which is illegal and throws an error.
        /// </summary>
        /// <param name="node">The node to check</param>
        /// <param name="sawMandatoryParam">true once a parameter without a default clause occurs.</param>
        private bool HasParameterStmtNested(ParseNode node, ref bool sawMandatoryParam)
        {
            // Base case:
            if (node.Token.Type == TokenType.declare_parameter_clause)
            {
                // found one, double check that we don't have an undefaulted param after a defaulted one while we're at it:
                // The logic counts backward here.
                for (int i = node.Nodes.Count-2 ; i > 0 ; i -= 2)
                {
                    // If this is an expression, then we have a defaultable optional arg.
                    // else we have a mandatory arg.
                    TokenType tType = node.Nodes[i].Token.Type;
                    bool isOptionalParam = (tType == TokenType.expr);
                    if (isOptionalParam)
                    {
                        if (sawMandatoryParam)
                        {
                            LineCol location = GetLineCol(node);
                            throw new KOSDefaultParamNotAtEndException(location);
                        }

                        i -= 2; // skip back a bit further to pass over the extra terms a defaulter has.
                    }
                    else
                    {
                        sawMandatoryParam = true;
                    }
                }
                
                return true;
            }


            // Recursive case - make sure to walk backward, and don't abort the scan when a thing is found:
            bool rememberReturnVal = false;
            for (int i = node.Nodes.Count - 1 ; i >= 0 ; --i)
            {
                ParseNode child = node.Nodes[i];

                // functions nested in functions don't count, nor do anonymous delegates
                if (child.Token.Type != TokenType.declare_function_clause && child.Token.Type != TokenType.expr)
                {
                    if (HasParameterStmtNested(child, ref sawMandatoryParam))
                        rememberReturnVal = true;
                }
            }
                
            return rememberReturnVal;
        }
        
        /// <summary>
        /// When parsing a function, we need to mark the spot where the lastmost PARAMETER
        /// statement occurred, so it knows that that's the point during runtime where it
        /// should assert all the arguments passed in have been consumed by the function.
        /// <br/><br/>
        /// This is done by inserting a fake extra __ARGBOTTOM "statement" into the parse tree just after
        /// the last parameter, or if no parameters existed then right at the very top.
        /// </summary>
        /// <param name="node"></param>
        private int FindArgBottomSpot(ParseNode node)
        {
            int lastmostDefParamStmt = -1;
            bool sawMandatoryParam = false;
            for (int i = node.Nodes.Count-1 ; i >= 0 ; --i)
            {
                if (HasParameterStmtNested(node.Nodes[i], ref sawMandatoryParam))
                {
                    // Only set this the very fist time a hit is seen - counting backward from the
                    // end that will be the lastmost parameter statement:
                    if (lastmostDefParamStmt == -1)
                        lastmostDefParamStmt = i;
                    
                    // Would break here but instead need to keep checking for defaulted params prior to
                    // mandatory ones for throwing KOSDefaultParamNotAtEndException
                }
            }
            
            return lastmostDefParamStmt+1;
        }

        /// <summary>
        /// Add all the variables at this local scope for holding the jump addresses to go to
        /// for the given function names defined in this scope.  Pass a NULL to mean global scope.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="isFileScope">A few special exceptions are needed for functions at the outermost file scope.
        /// If a function is at the outermost file scope level, then it needs to default to global identifier,
        /// else it needs to default to local identifier.  This weird rule is needed for backward compatibility.</param>
        private void AddFunctionJumpVars(ParseNode node, bool isFileScope)
        {
            // All the functions for which this scope is where they live, and this file is where they live:
            IEnumerable<UserFunction> theseFuncs =
                context.UserFunctions.GetUserFunctionList().Where(
                    item =>
                        item.IsFunction &&                                     // This might be redundant?
                        item.ScopeNode == node &&                              // Preprocessing found this function here in this set of scope braces.
                        ((! isFileScope) || context.UserFunctions.IsNew(item)));  // If global, ensure it's not from a previously compiled script's global scope.

            // That last check deliberately takes advantage of short-circuiting to avoid the expense of IsNew() if it can.

            foreach (UserFunction func in theseFuncs)
            {
                // Populate the LOCAL name with the function jump location.
                // By storing the mapping from identifier name to instruction jump point in
                // a local variable, we're masking the function from view when its variable
                // identifier is out of scope.  (For example if a function's body of Opcodes
                // is stored at locations K100_021 through K100_141, then those 100 opcodes
                // are compiled statically and are always present in memory in a way that ignores scope,
                // but the variable "$MyFunc*" that contains the value K100_021 to tell you where
                // to jump to start the function will stop existing once MyFunc is out of scope).
                // This is typical of an OOP language.  The physical code is always static for all
                // methods and functions, and always has exactly one copy in memory whether there are
                // one, many, or zero "instances" of it present in scope at the moment.
                //
                // Note that because we now allow the capture of UserDelegates in user-land variables
                // to be called later perhaps from a different scope, we now store a closure context
                // for all functions just in case they may get used this way.  It's unneded overhead
                // to do so most of the time, but it makes the algorithm simple for the few cases
                // where it is needed.

                AddOpcode(new OpcodePushDelegateRelocateLater(null,true), func.GetFuncLabel());

                // Where the function should go, according to the rules of GLOBAL, LOCAL, and LAZYGLOBAL:
                StorageModifier whereToPut = GetStorageModifierFor(func.OriginalNode);

                // But make a weird exception for file scope - they are always global unless explicitly stated to be local:
                if (isFileScope && whereToPut != StorageModifier.LOCAL)
                    AddOpcode(new OpcodeStore(func.ScopelessPointerIdentifier));
                else
                    AddOpcode(CreateAppropriateStoreCode(whereToPut, true, func.ScopelessPointerIdentifier));
            }
        }

        // This is no longer called directly from parse because it now is called from
        // VisitDeclareStatement, which reads the storage modifier keywords and 
        // passes them on to here.
        private void VisitLockStatement(ParseNode node, StorageModifier whereToStore)
        {
            NodeStartHousekeeping(node);
            string lockIdentifier = node.Nodes[1].Token.Text;
            int expressionHash = ConcatenateNodes(node.Nodes[3]).GetHashCode();
            UserFunction lockObject = context.UserFunctions.GetUserFunction(
                lockIdentifier, 
                whereToStore == StorageModifier.GLOBAL ? (Int16)0 : GetContainingScopeId(node),
                node);

            string functionLabel = lockObject.GetUserFunctionLabel(expressionHash);
            // lock variable
            AddOpcode(new OpcodePushDelegateRelocateLater(null,true), functionLabel);
            AddOpcode(CreateAppropriateStoreCode(whereToStore, allowLazyGlobal, lockObject.ScopelessPointerIdentifier));

            if (lockObject.IsSystemLock())
            {
                // add update trigger
                string triggerIdentifier = "lock-" + lockObject.ScopelessIdentifier;
                if (context.Triggers.Contains(triggerIdentifier))
                {
                    Trigger triggerObject = context.Triggers.GetTrigger(triggerIdentifier);
                    AddOpcode(new OpcodePushRelocateLater(null), triggerObject.GetFunctionLabel());
                    AddOpcode(new OpcodeAddTrigger());
                }
                    
                // enable this FlyByWire parameter
                AddOpcode(new OpcodePush(new KOSArgMarkerType()));
                AddOpcode(new OpcodePush(lockIdentifier));
                AddOpcode(new OpcodePush(true));
                AddOpcode(new OpcodeCall("toggleflybywire()"));
                // add a pop to clear out the dummy return value from toggleflybywire()
                AddOpcode(new OpcodePop());
            }
        }
        
        private void VisitUnlockStatement(ParseNode node)
        {
            NodeStartHousekeeping(node);
            if (node.Nodes[1].Token.Type == TokenType.ALL)
            {
                // unlock all locks
                foreach (UserFunction userFuncObject in context.UserFunctions.GetUserFunctionList())
                    if (! userFuncObject.IsFunction)
                        UnlockIdentifier(userFuncObject);
            }
            else
            {
                string lockIdentifier = node.Nodes[1].Token.Text;
                UserFunction lockObject = FindExistingUserFunction(lockIdentifier, node);
                if (lockObject == null)
                {
                    // If it is null, it's okay to silently do nothing.  It just means someone tried to unlock
                    // an identifier that was never locked in the first place, at least not in this scope or a parent scope.
                    return;
                }
                UnlockIdentifier(lockObject);
            }
        }

        private void UnlockIdentifier(UserFunction lockObject)
        {
            if (lockObject.IsSystemLock())
            {
                // disable this FlyByWire parameter
                AddOpcode(new OpcodePush(new KOSArgMarkerType()));
                AddOpcode(new OpcodePush(lockObject.ScopelessIdentifier));
                AddOpcode(new OpcodePush(false));
                AddOpcode(new OpcodeCall("toggleflybywire()"));
                // add a pop to clear out the dummy return value from toggleflybywire()
                AddOpcode(new OpcodePop());

                // remove update trigger
                string triggerIdentifier = "lock-" + lockObject.ScopelessIdentifier;
                if (context.Triggers.Contains(triggerIdentifier))
                {
                    Trigger triggerObject = context.Triggers.GetTrigger(triggerIdentifier);
                    AddOpcode(new OpcodePushRelocateLater(null), triggerObject.GetFunctionLabel());
                    AddOpcode(new OpcodeRemoveTrigger());
                }
            }

            // unlock variable
            // Really, we should unlock a variable by unsetting it's pointer var so it's an error to use it:
            AddOpcode(new OpcodePushRelocateLater(null), lockObject.DefaultLabel);
            if (allowLazyGlobal)
                AddOpcode(new OpcodeStore(lockObject.ScopelessPointerIdentifier));
            else
                AddOpcode(new OpcodeStoreExist(lockObject.ScopelessPointerIdentifier));
        }

        private void VisitOnStatement(ParseNode node)
        {
            NodeStartHousekeeping(node);
            int expressionHash = ConcatenateNodes(node).GetHashCode();
            string triggerIdentifier = "on-" + expressionHash.ToString();
            Trigger triggerObject = context.Triggers.GetTrigger(triggerIdentifier);

            if (triggerObject.IsInitialized())
            {
                // Store the current value into the old value to prep for the first use of the ON trigger:
                VisitExpression(node.Nodes[1]); // the expression in the on statement.
                AddOpcode(new OpcodeStore(triggerObject.OldValueIdentifier));
                AddOpcode(new OpcodePushRelocateLater(null), triggerObject.GetFunctionLabel());
                AddOpcode(new OpcodeAddTrigger());
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
                AddOpcode(new OpcodeAddTrigger());
            }
        }

        private void VisitWaitStatement(ParseNode node)
        {
            NodeStartHousekeeping(node);

            if (node.Nodes.Count == 3)
            {
                // For commands of the form:  WAIT N. where N is a number:
                VisitExpression(node.Nodes[1]);
                AddOpcode(new OpcodeWait());
            }
            else
            {
                // For commands of the form:  WAIT UNTIL expr. where expr is any boolean expression:
                Opcode waitLoopStart = AddOpcode(new OpcodePush(0));       // Loop start: Gives OpcodeWait an argument of zero.
                AddOpcode(new OpcodeWait());                               // Avoid busy polling.  Even a WAIT 0 still forces 1 fixedupdate 'tick'.
                VisitExpression(node.Nodes[2]);                            // Inserts instructions here to evaluate the expression
                AddOpcode(new OpcodeBranchIfFalse(), waitLoopStart.Label); // Repeat the loop as long as expression is false.
                // Falls through to whatever comes next when expression is true.
            }
        }

        private void VisitDeclareStatement(ParseNode node)
        {
            NodeStartHousekeeping(node);
            ParseNode lastSubNode = node.Nodes[node.Nodes.Count-1];
            
            StorageModifier whereToStore = GetStorageModifierFor(node);
            
            // If the declare statement is of the form:
            //    DECLARE [GLOBAL|LOCAL] identifier TO expr.
            if (lastSubNode.Token.Type == TokenType.declare_identifier_clause)
            {
                VisitExpression(lastSubNode.Nodes[2]);
                AddOpcode(CreateAppropriateStoreCode(whereToStore, true, "$" + GetIdentifierText(lastSubNode.Nodes[0])));
            }
            
            // If the declare statement is of the form:
            //    DECLARE PARAMETER ident.
            // or
            //    DECLARE PARAMETER ident IS expr.
            // or
            //    DECLARE PARAMETER ident,ident,ident...
            // or
            //    DECLARE PARAMETER ident,ident,ident IS expr, ident IS EXPR...
            else if (lastSubNode.Token.Type == TokenType.declare_parameter_clause)
            {
                for (int i = 1 ; i < lastSubNode.Nodes.Count ; i += 2)
                {
                    bool hasInit = ( i < lastSubNode.Nodes.Count - 2 &&
                                    ( lastSubNode.Nodes[i+1].Token.Type == TokenType.IS ||
                                     lastSubNode.Nodes[i+1].Token.Type == TokenType.TO )
                                   );
                    ParseNode initExpressionNode = hasInit ? lastSubNode.Nodes[i+2] : null;
                    VisitDeclareOneParameter(whereToStore, lastSubNode.Nodes[i], initExpressionNode);
                    if (hasInit)
                    {
                        i += 2; // skip the "TO expr" part when looking for the next param.
                    }
                }
            }
            
            // If the declare statement is of the form:
            //    DECLARE [GLOBAL|LOCAL] LOCK FOO TO expr.
            else if (lastSubNode.Token.Type == TokenType.declare_lock_clause)
            {
                VisitLockStatement(lastSubNode, whereToStore);
            }
            
            // Note: DECLARE FUNCTION is dealt with entirely during
            // PreprocessDeclareStatement, with nothing for VisitNode to do.
        }
        
        /// <summary>
        /// Process a single parameter from the parameter list for a
        /// function or program.  i.e. if encountering the statement
        /// "DECLARE PARAMETER AA, BB, CC is 0." , then this method needs to be
        /// called 3 times, once for AA, once for BB, and once for "CC is 0":
        /// </summary>
        /// <param name="whereToStore">is it local or global or lazyglobal</param>
        /// <param name="identifierNode">Parse node holding the identifier of the param</param>
        /// <param name="expressionNode">Parse node holding the expression to initialize to if
        /// this is a defaultable parameter.  If it is not a defaultable parameter, pass null here</param>
        private void VisitDeclareOneParameter(StorageModifier whereToStore, ParseNode identifierNode, ParseNode expressionNode)
        {
            if (expressionNode != null)
            {
                // This tests each defaultable parameter to see if it's at arg bottom.
                // The test must be repeated for each parameter rather than optimizing by
                // falling through to all subsequent defaulter expressions for the rest of
                // the parameters once the first one finds arg bottom.
                // This is because kerboscript does not require the declare parameters to
                // be contiguous statements so there may be code in between them you're
                // not supposed to skip over.

                AddOpcode(new OpcodeTestArgBottom());
                OpcodeBranchIfFalse branchSkippingInit = new OpcodeBranchIfFalse();
                AddOpcode(branchSkippingInit);
                
                VisitExpression(expressionNode); // evals init expression on the top of the stack where the arg would have been

                branchSkippingInit.DestinationLabel = GetNextLabel(false);
            }
            AddOpcode(CreateAppropriateStoreCode(whereToStore, true, "$" + GetIdentifierText(identifierNode)));
        }
                
        /// <summary>
        /// Make the right sort of opcodestore-ish opcode for what storage
        /// mode we're in.
        /// </summary>
        /// <param name="kind">which sort of storage is this</param>
        /// <param name="lazyGlobal">true if store should make a lazyglobal,
        /// false if it should not.  NOTE that it should always be true when
        /// doing DECLARE operations and only vary when doing SET operations.</param>
        /// <returns>the new opcode you should add</returns>
        private Opcode CreateAppropriateStoreCode(StorageModifier kind, bool lazyGlobal, string identifier)
        {
            switch (kind)
            {
                case StorageModifier.LOCAL:
                    return new OpcodeStoreLocal(identifier);
                case StorageModifier.GLOBAL:
                    return new OpcodeStoreGlobal(identifier);
                default:
                    if (lazyGlobal)
                        return new OpcodeStore(identifier);
                    else
                        return new OpcodeStoreExist(identifier);
            }
        }
        
        // Return the storage modifier enum to go with this declare or set statement.
        // i.e. "global, local, any".
        // Will throw a syntax error of the storage type is invalid for this
        // variety of declare statement.
        private StorageModifier GetStorageModifierFor(ParseNode node)
        {
            // The default case for anything not explicitly mentioned below.
            // i.e. if you call this on a SET statement, you'll get this:
            var modifier = StorageModifier.LAZYGLOBAL;
            
            if (node.Nodes.Count == 0) // sanity check - really should never be called on terminal nodes like this.
                return modifier;
            
            // It may look weird to do this as a switch when there's only one condition and it
            // looks like it should be an if.  It's leaving room for expansion later if the need
            // arises:
            switch (node.Token.Type)
            {
                case TokenType.declare_stmt:
                    modifier = GetStorageModifierForDeclare(node);
                    break;
                default:
                    break;
            }

            return modifier;
        }

        private StorageModifier GetStorageModifierForDeclare(ParseNode node)
        {
            ParseNode lastSubNode = node.Nodes[node.Nodes.Count-1];

            // Default varies depending on which kind of statement it is.
            // locks are default global, and functions declared at file
            // scope are default global, while everything else is default local:
            StorageModifier modifier = StorageModifier.LOCAL;
            if (lastSubNode.Token.Type == TokenType.declare_lock_clause ||
                lastSubNode.Token.Type == TokenType.declare_function_clause)
            {
                modifier = StorageModifier.GLOBAL;
            }

            bool storageKeywordMissing = true;
            
            foreach (ParseNode t in node.Nodes)
            {
                switch (t.Token.Type)
                {
                    case TokenType.GLOBAL:
                        modifier = StorageModifier.GLOBAL;
                        storageKeywordMissing = false;
                        break;
                    case TokenType.LOCAL:
                        modifier = StorageModifier.LOCAL;
                        storageKeywordMissing = false;
                        break;
                    default:
                        break;
                }
            }
            if (storageKeywordMissing &&
                lastSubNode.Token.Type == TokenType.declare_identifier_clause &&
                !allowLazyGlobal)
            {
                LineCol location = GetLineCol(node);
                throw new KOSCommandInvalidHereException(location, 
                                                         "a bare DECLARE identifier, without a GLOBAL or LOCAL keyword",
                                                         "in an identifier initialization while under a @LAZYGLOBAL OFF directive",
                                                         "in a file where the default @LAZYGLOBAL behavior is on");
            }
            if (modifier == StorageModifier.GLOBAL && lastSubNode.Token.Type == TokenType.declare_parameter_clause)
            {
                LineCol location = GetLineCol(node);
                throw new KOSCommandInvalidHereException(location, "GLOBAL", "in a parameter declaration", "in a variable declaration");
            }

            return modifier;
        }

        private void VisitToggleStatement(ParseNode node)
        {
            NodeStartHousekeeping(node);
            // process this as
            // SET foo TO NOT foo
            ExpressionNode identifier = ExpressionBuilder.BuildExpression(node.Nodes[1]);
            ExpressionNode target = new NegateExpressionNode() {
                ParseNode = node,
                Target = identifier
            };

            ProcessSetOperation(identifier, target);
        }

        private void VisitPrintStatement(ParseNode node)
        {
            NodeStartHousekeeping(node);
            if (node.Nodes.Count == 3)
            {
                AddOpcode(new OpcodePush(new KOSArgMarkerType()));
                VisitExpression(node.Nodes[1]);
                AddOpcode(new OpcodeCall("print()"));
                AddOpcode(new OpcodePop()); // all functions now return a value even if it's a dummy we ignore.
            }
            else
            {
                AddOpcode(new OpcodePush(new KOSArgMarkerType()));
                VisitExpression(node.Nodes[1]);
                VisitExpression(node.Nodes[4]);
                VisitExpression(node.Nodes[6]);
                AddOpcode(new OpcodeCall("printat()"));
                AddOpcode(new OpcodePop()); // all functions now return a value even if it's a dummy we ignore.
            }
        }

        private void VisitStageStatement(ParseNode node)
        {
            NodeStartHousekeeping(node);
            AddOpcode(new OpcodePush(new KOSArgMarkerType()));
            AddOpcode(new OpcodeCall("stage()"));
            AddOpcode(new OpcodePop()); // all functions now return a value even if it's a dummy we ignore.
        }

        private void VisitAddStatement(ParseNode node)
        {
            NodeStartHousekeeping(node);
            AddOpcode(new OpcodePush(new KOSArgMarkerType()));
            VisitExpression(node.Nodes[1]);
            AddOpcode(new OpcodeCall("add()"));
            AddOpcode(new OpcodePop()); // all functions now return a value even if it's a dummy we ignore.
        }

        private void VisitRemoveStatement(ParseNode node)
        {
            NodeStartHousekeeping(node);
            AddOpcode(new OpcodePush(new KOSArgMarkerType()));
            VisitExpression(node.Nodes[1]);
            AddOpcode(new OpcodeCall("remove()"));
            AddOpcode(new OpcodePop()); // all functions now return a value even if it's a dummy we ignore.
        }

        private void VisitClearStatement(ParseNode node)
        {
            NodeStartHousekeeping(node);
            AddOpcode(new OpcodePush(new KOSArgMarkerType()));
            AddOpcode(new OpcodeCall("clearscreen()"));
            AddOpcode(new OpcodePop()); // all functions now return a value even if it's a dummy we ignore.
        }

        private void VisitEditStatement(ParseNode node)
        {
            NodeStartHousekeeping(node);
            AddOpcode(new OpcodePush(new KOSArgMarkerType()));
            VisitExpression(node.Nodes[1]);
            AddOpcode(new OpcodeCall("edit()"));
            AddOpcode(new OpcodePop()); // all functions now return a value even if it's a dummy we ignore.
        }

        /// <summary>
        /// This one rule handles run_stmt, runpath_stmt, and runoncepath_stmt's.
        /// They're all nearly the same thing, but with slightly different syntaces.
        /// </summary>
        /// <param name="node"></param>
        private void VisitRunStatement(ParseNode node)
        {
            NodeStartHousekeeping(node);
            TokenType stmt_type = node.Nodes[0].Token.Type;
            
            // The slightly different versions of this same statememnt have the important
            // nodes at different indeces.  Other than that, they're pretty much the same.
            // So start by just getting the indeces of where the important parts are:
            int volumeIndex;
            int argListIndex;
            int progNameIndex;
            bool hasOnce;
            if (stmt_type == TokenType.RUN)
            {
                // RUN FILEIDENT BRACKETOPEN arglist? BRACKETCLOSE (ON expr)? EOI
                // 0th 1st       2nd         3rd      4th          5th 6th    7th 
                hasOnce = false;  // for now.  Will change later if it's present.              
                progNameIndex = 1;
                argListIndex = 3;
                volumeIndex = 3; // if argList is missing.  Will increment later if argList is present.
            }
            else if (stmt_type == TokenType.RUNPATH)
            {
                // RUNPATH BRACKETOPEN expr (COMMA arglist)? BRACKETCLOSE EOI
                // 0th     1st         2nd   3rd   4th       5th          6th
                hasOnce = false;
                progNameIndex = 2;
                argListIndex = 4;
                volumeIndex = -99; //archaic syntax we're not supporting anymore.
            }
            else if (stmt_type == TokenType.RUNONCEPATH)
            {
                // RUNONCEPATH BRACKETOPEN expr (COMMA arglist)? BRACKETCLOSE EOI
                // 0th         1st         2nd   3rd   4th       5th          6th
                hasOnce = true;
                progNameIndex = 2;
                argListIndex = 4;
                volumeIndex = -99; //archaic syntax we're not supporting anymore.
            }
            else
            {
                // This "cannot happen".  It's here to remove warnings about using unintialized values.
                throw new KOSYouShouldNeverSeeThisException("kRISC.tpg file does not agree with VisitRunStatement() in Compiler.cs.");
            }

            if (node.Nodes[1].Token.Type == TokenType.ONCE)
            {
                // If a ONCE keyword is inserted after the RUN, then shift everything one step forward:
                hasOnce = true;
                ++volumeIndex;
                ++argListIndex;
                ++progNameIndex;
            }
            if (hasOnce && ! options.LoadProgramsInSameAddressSpace)
                throw new KOSOnceInvalidHereException(new LineCol(lastLine, lastColumn));

            // process program arguments
            AddOpcode(new OpcodePush(new KOSArgMarkerType())); // regardless of whether it's called directly or indirectly, we still need at least one.
            bool hasOn = node.Nodes.Any(cn => cn.Token.Type == TokenType.ON);
            if (!hasOn && options.LoadProgramsInSameAddressSpace)
            {
                // When running in the same address space, we need an extra arg marker under the args, because
                // of the double-indirect call where we call the subroutine that was built in PreProcessRunStatement,
                // and IT in turn calls the actual subprogram (after deciding whether or not it needs to compile it
                // into existence).
                AddOpcode(new OpcodePush(new KOSArgMarkerType()));
            }

            // Needs to push a true/false onto the stack to record whether or not there was a "once"
            // associated with this run (i.e. "run" versus "run once").  This has to go
            // underneath the actual args.  This arg, and them, will get reversed by OpcodeCall, thus this
            // boolean will end up atop the stack by the time the 'once' checker built in
            // PreprocessRunStatement() gets to it:
            if (!hasOn && options.LoadProgramsInSameAddressSpace)
            {
                AddOpcode(new OpcodePush(hasOnce));
                VisitExpression(node.Nodes[progNameIndex]); // put program name on stack.
                AddOpcode(new OpcodeEval(true));
            }

            if (node.Nodes.Count > argListIndex && node.Nodes[argListIndex].Token.Type == TokenType.arglist)
            {
                // Run args need to get pushed to the stack in the opposite order to how
                // function args do, because they pass through two levels of OpcodeCall, and
                // thus get reversed twice, whereas function args only get reversed once:
                VisitArgListReversed(node.Nodes[argListIndex]);
                volumeIndex += 3;
            }

            if (!hasOn && options.LoadProgramsInSameAddressSpace)
            {
                AddOpcode(new OpcodeCall(null)).DestinationLabel = boilerplateLoadAndRunEntryLabel;
                AddOpcode(new OpcodePop()); // ditch the dummy return value for now - maybe we can use it in a later version.
            }
            else
            {
                // When running in a new address space, we also need a second arg marker, but in this
                // case it has to go over the top of the other args, not under them, to tell the RUN
                // builtin function where its arguments end and the progs arguments start:
                AddOpcode(new OpcodePush(new KOSArgMarkerType()));

                // program name
                VisitExpression(node.Nodes[progNameIndex]);

                // volume where program should be executed (null means local)
                if (volumeIndex >= 0 && volumeIndex < node.Nodes.Count)
                    VisitNode(node.Nodes[volumeIndex]);
                else
                    AddOpcode(new OpcodePush(null));

                AddOpcode(new OpcodeCall("run()"));
                
                // Note: it is not an error that there are two Pop's here:  There are two levels of return value - one from the program run
                // and one from the function call run():
                AddOpcode(new OpcodePop()); // ditch the program exit's dummy return value for now - maybe we can use it in a later version.
                AddOpcode(new OpcodePop()); // ditch the run()'s dummy return value for now - maybe we can use it in a later version.
            }
        }

        private void VisitCompileStatement(ParseNode node)
        {
            NodeStartHousekeeping(node);
            AddOpcode(new OpcodePush(new KOSArgMarkerType())); // for the load() function.
            VisitExpression(node.Nodes[1]);
            AddOpcode(new OpcodePush(false));
            if (node.Nodes.Count > 3)
            {
                // It has a "TO outputfile" clause:
                VisitExpression(node.Nodes[3]);
            }
            else
            {
                // It lacks a "TO outputfile" clause, so put a dummy string there for it to
                // detect later during the "load()" function as a flag telling it to
                // calculate the output name:
                AddOpcode(new OpcodePush("-default-compile-out-"));
            }
            AddOpcode(new OpcodeCall("load()"));
            AddOpcode(new OpcodePop()); // all functions now return a value even if it's a dummy we ignore.
        }

        private void VisitSwitchStatement(ParseNode node)
        {
            NodeStartHousekeeping(node);
            AddOpcode(new OpcodePush(new KOSArgMarkerType()));
            VisitExpression(node.Nodes[2]);
            AddOpcode(new OpcodeCall("switch()"));
            AddOpcode(new OpcodePop()); // all functions now return a value even if it's a dummy we ignore.
        }

        private void VisitCopyStatement(ParseNode node)
        {
            NodeStartHousekeeping(node);
            AddOpcode(new OpcodePush(new KOSArgMarkerType()));
            VisitExpression(node.Nodes[1]);

            AddOpcode(new OpcodePush(node.Nodes[2].Token.Type == TokenType.FROM ? "from" : "to"));

            VisitExpression(node.Nodes[3]);
            AddOpcode(new OpcodeCall("copy_deprecated()"));
            AddOpcode(new OpcodePop()); // all functions now return a value even if it's a dummy we ignore.
        }

        private void VisitRenameStatement(ParseNode node)
        {
            NodeStartHousekeeping(node);
            int oldNameIndex = 2;
            int newNameIndex = 4;

            bool renameFile = false;

            AddOpcode(new OpcodePush(new KOSArgMarkerType()));
            if (node.Nodes.Count == 5)
            {
                oldNameIndex--;
                newNameIndex--;
                AddOpcode(new OpcodePush("file"));
                renameFile = true;
            }
            else
            {
                renameFile = node.Nodes[1].Token.Type == TokenType.FILE;
                AddOpcode(new OpcodePush(renameFile ? "file" : "volume"));
            }

            VisitExpression(node.Nodes[oldNameIndex]);
            VisitExpression(node.Nodes[newNameIndex]);

            if (renameFile)
            {
                AddOpcode(new OpcodeCall("rename_file_deprecated()"));
            }
            else
            {
                AddOpcode(new OpcodeCall("rename_volume_deprecated()"));
            }
            AddOpcode(new OpcodePop()); // all functions now return a value even if it's a dummy we ignore.
        }

        private void VisitDeleteStatement(ParseNode node)
        {
            NodeStartHousekeeping(node);
            AddOpcode(new OpcodePush(new KOSArgMarkerType()));
            VisitExpression(node.Nodes[1]);

            if (node.Nodes.Count == 5)
                VisitExpression(node.Nodes[3]);
            else
                AddOpcode(new OpcodePush(null));

            AddOpcode(new OpcodeCall("delete_deprecated()"));
            AddOpcode(new OpcodePop()); // all functions now return a value even if it's a dummy we ignore.
        }

        private void VisitListStatement(ParseNode node)
        {
            NodeStartHousekeeping(node);
            bool hasIdentifier = (node.Nodes[1].Token.Type == TokenType.IDENTIFIER);
            bool hasIn = hasIdentifier && (node.Nodes[2].Token.Type == TokenType.IN);

            if (hasIn)
            {
                // destination variable
                string varName = "$" + GetIdentifierText(node.Nodes[3]);
                // list type
                AddOpcode(new OpcodePush(new KOSArgMarkerType()));
                VisitExpression(node.Nodes[1]);
                // build list
                AddOpcode(new OpcodeCall("buildlist()"));
                if (allowLazyGlobal)
                    AddOpcode(new OpcodeStore(varName));
                else
                    AddOpcode(new OpcodeStoreExist(varName));
            }
            else
            {
                AddOpcode(new OpcodePush(new KOSArgMarkerType()));
                // list type
                if (hasIdentifier)
                    VisitExpression(node.Nodes[1]);
                else
                    AddOpcode(new OpcodePush("files"));
                // print list
                AddOpcode(new OpcodeCall("printlist()"));
                AddOpcode(new OpcodePop()); // all functions now return a value even if it's a dummy we ignore.
            }
        }

        private void VisitLogStatement(ParseNode node)
        {
            NodeStartHousekeeping(node);
            AddOpcode(new OpcodePush(new KOSArgMarkerType()));
            VisitExpression(node.Nodes[1]);
            VisitExpression(node.Nodes[3]);
            AddOpcode(new OpcodeCall("logfile()"));
            AddOpcode(new OpcodePop()); // all functions now return a value even if it's a dummy we ignore.
        }

        private void VisitBreakStatement(ParseNode node)
        {
            NodeStartHousekeeping(node);

            if (!nowInALoop)
                throw new KOSBreakInvalidHereException(new LineCol(lastLine, lastColumn));

            // Will need to pop out the number of variables scopes equal to the
            // number of braces we're skipping out of.  For now just record the
            // current nest level in the opcode.  Later, during PopBreakList(),
            // the nest argument gets replaced with the real value it will have
            // in the final program.  The reason for not just doing it now is
            // that we have to wait until the bottom of the nested braces to
            // find out where to jump to anyway, so this opcode will have to be
            // revisited then anyway:
            Opcode popScope = AddOpcode(new OpcodePopScope(braceNestLevel));
            AddToBreakList(popScope);
 
            // Jump to the bottom of the loop. Since we don't know where that
            // is yet, put a placeholder jump opcode here, to be filled in later
            // during PopBreakList() when the real value becomes known:
            Opcode jump = AddOpcode(new OpcodeBranchJump());
            AddToBreakList(jump);
        }

        private void VisitReturnStatement(ParseNode node)
        {
            NodeStartHousekeeping(node);

            var nestLevelOfFuncBraces = (Int16)GetReturnNestLevel();

            if (nestLevelOfFuncBraces < 0)
                throw new KOSReturnInvalidHereException(new LineCol(lastLine, lastColumn));
            
            // Push the return expression onto the stack, or if it was a naked RETURN
            // keyword with no expression, then push a secret dummy return value of zero:
            if (node.Nodes.Count > 2)
            {
                VisitExpression(node.Nodes[1]);
            }
            else
            {
                AddOpcode(new OpcodePush(0));
            }

            // Pop the correct number of scoping levels and return.  This is much
            // simpler than the BREAK case because RETURN already knows to use the function
            // call stack to figure out where to return to, so we don't have to wait until
            // later to decide where to jump to like we do in BREAK:
            int depth = 1 + braceNestLevel - nestLevelOfFuncBraces;
            AddOpcode(new OpcodeReturn((Int16)depth));
        }

        private void VisitPreserveStatement(ParseNode node)
        {
            NodeStartHousekeeping(node);

            if (!nowCompilingTrigger)
                throw new KOSPreserveInvalidHereException(new LineCol(lastLine, lastColumn));

            string flagName = PeekTriggerKeepName();
            AddOpcode(new OpcodePush(true));
            AddOpcode(new OpcodeStore(flagName));
        }

        private void VisitRebootStatement(ParseNode node)
        {
            NodeStartHousekeeping(node);
            AddOpcode(new OpcodePush(new KOSArgMarkerType()));
            AddOpcode(new OpcodeCall("reboot()"));
            AddOpcode(new OpcodePop()); // all functions now return a value even if we ignore it.  Not sure it matters in the case of reboot() though.
        }

        private void VisitShutdownStatement(ParseNode node)
        {
            NodeStartHousekeeping(node);
            AddOpcode(new OpcodePush(new KOSArgMarkerType()));
            AddOpcode(new OpcodeCall("shutdown()"));
            AddOpcode(new OpcodePop()); // all functions now return a value even if we ignore it.  Not sure it matters in the case of shutdown() though.
        }

        private void VisitForStatement(ParseNode node)
        {
            NodeStartHousekeeping(node);

            bool remember = nowInALoop;
            nowInALoop = true;

            string iteratorIdentifier = "$" + GetIdentifierText(node.Nodes[3]) + "-iterator";

            // Add a scope level to hold the iterator variable.  This will live just "outside" the
            // brace scope of the function body.
            BeginScope(node);

            PushBreakList(braceNestLevel);

            VisitExpression(node.Nodes[3]);
            AddOpcode(new OpcodeGetMember("iterator"));
            AddOpcode(new OpcodeStoreLocal(iteratorIdentifier));
            // loop condition
            Opcode condition = AddOpcode(new OpcodePush(iteratorIdentifier));
            string conditionLabel = condition.Label;
            AddOpcode(new OpcodeGetMember("next"));
            // branch
            Opcode branch = AddOpcode(new OpcodeBranchIfFalse());
            AddToBreakList(branch);
            // assign value to iteration variable
            string varName = "$" + GetIdentifierText(node.Nodes[1]);
            AddOpcode(new OpcodePush(iteratorIdentifier));
            AddOpcode(new OpcodeGetMember("value"));
            AddOpcode(new OpcodeStoreLocal(varName));
            // instructions in FOR body
            VisitNode(node.Nodes[4]);
            // jump to condition
            Opcode jump = AddOpcode(new OpcodeBranchJump());
            jump.DestinationLabel = conditionLabel;

            // end of loop, give NOP destination to land at for breaks and end-loop condition:
            Opcode endLoop = AddOpcode(new OpcodeNOP());
            PopBreakList(endLoop.Label);

            // End the scope level holding the iterator variable:
            EndScope(node);


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
                VisitExpression(node.Nodes[1]);
            }

            AddOpcode(new OpcodeUnset());
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
                ProcessSetOperation(
                    ExpressionBuilder.BuildExpression(node.Nodes[0]), 
                    ExpressionBuilder.BuildExpression(node.Nodes[1])
                );
            }
            else
            {
                // In the case of anything other than an on/off statement:
                // Just do the normal stuff to parse the suffix rule expression, and then throw
                // the result away off the top of the stack.
                // To keep this code simple, we just have the rule that there will unconditionally always
                // be something atop the stack that all function calls leave behind, even if it's a dummy.)
                VisitExpression(node.Nodes[0]);
                AddOpcode(new OpcodePop());
            }
        }
        
        public void VisitDirective(ParseNode node)
        {
            NodeStartHousekeeping(node);
            
            // For now, let the compiler decide if the compiler directive is in the wrong place, 
            // not the parser.  Therefore the parser treats it like a normal statement and here in
            // the compiler we'll decide per-directive which directives can go where:

            ParseNode directiveNode = node.Nodes[0]; // a directive contains the exact directive node nested one step inside it.
            
            if (directiveNode.Nodes.Count < 2)
                throw new KOSCompileException(new LineCol(lastLine, lastColumn), "Kerboscript compiler directive ('@') without a keyword after it.");
            
            
            switch (directiveNode.Nodes[1].Token.Type)
            {
                case TokenType.LAZYGLOBAL:
                    VisitLazyGlobalDirective(directiveNode);
                    break;
                    
                // There is room for expansion here if we want to add more compiler directives.
                
                default:
                    throw new KOSCompileException(new LineCol(lastLine, lastColumn), "Kerboscript compiler directive @"+directiveNode.Nodes[1].Text+" is unknown.");
            }
        }
        
        public void VisitLazyGlobalDirective(ParseNode node)
        {
            if (node.Nodes.Count < 3 || node.Nodes[2].Token.Type != TokenType.onoff_trailer)
                throw new KOSCompileException(new LineCol(lastLine, lastColumn), "Kerboscript compiler directive @LAZYGLOBAL requires an ON or an OFF keyword.");
            
            // This particular directive is only allowed up at the top of a file, prior to any other non-directive statements.
            // ---------------------------------------------------------------------------------------------------------------
            
            bool validLocation = true; // will change to false if this isn't where a LazyGlobalDirective is allowed.

            // Check 1 - see if I'm nested in anything other than the outermost list of statements:
            ParseNode ancestor = node.Parent;
            ParseNode myInstructionContainer = node.Parent;
            while( ancestor != null && ancestor.Token.Type != TokenType.Start)
            {
                switch (ancestor.Token.Type)
                {
                    case TokenType.instruction_block:
                    case TokenType.if_stmt:
                    case TokenType.until_stmt:
                    case TokenType.when_stmt:
                    case TokenType.for_stmt:
                    case TokenType.on_stmt:
                        validLocation = false;
                        break;
                    case TokenType.instruction:
                        myInstructionContainer = ancestor;
                        break;
                    default:
                        break;
                }
                ancestor = ancestor.Parent;
            }
            // Check 2 - see if I am at the top.  The only statements allowed to precede me are other directives:
            if (validLocation && ancestor != null && ancestor.Token.Type == TokenType.Start)
            {
                // ancestor is now the Start node for the compile:
                int myInstructionIndex = ancestor.Nodes.IndexOf(myInstructionContainer); // would be an expensive walk - except this should only exist once, near the top.
                for (int i = 0; validLocation && i < myInstructionIndex; ++i)
                {
                    // if a statement preceding me is anything other than another directive, it's wrong:
                    if (ancestor.Nodes[i].Token.Type != TokenType.directive ||
                            (ancestor.Nodes[i].Token.Type == TokenType.instruction &&
                             ancestor.Nodes[i].Nodes[0].Token.Type != TokenType.directive)
                       )
                        validLocation = false;
                }
            }
            if (!validLocation)
                throw new KOSCommandInvalidHereException(new LineCol(node.Token.Line, node.Token.Column), "@LAZYGLOBAL",
                                                "after the first command in the file",
                                                "at the start of a script file, prior to any other statements");

            // Okay the location is fine - do the work:
            ParseNode onOffValue = node.Nodes[2].Nodes[0];
            if (onOffValue.Token.Type == TokenType.ON)
                allowLazyGlobal = true; // this is the default anyway, so this is just here for completeness in case we change the default.
            else if (onOffValue.Token.Type == TokenType.OFF)
                allowLazyGlobal = false;
            // else do nothing, which really should be an impossible case.
        }

        public void VisitExpression(ParseNode node)
        {
            NodeStartHousekeeping(node);

            ExpressionNode expr = ExpressionBuilder.BuildExpression(node);
            expr.Accept(this);
        }

        public void VisitExpression(OrExpressionNode node)
        {
            NodeStartHousekeeping(node.ParseNode);

            List<Opcode> skipOps = new List<Opcode>();

            for (int i = 0; i < node.Expressions.Length; i++)
            {
                node.Expressions[i].Accept(this);
                if (i != node.Expressions.Length - 1)
                {
                    // for all but the last expression, we short circuit when true
                    skipOps.Add(AddOpcode(new OpcodeBranchIfTrue()));
                }
            }

            // jump over the short circuit value
            AddOpcode(new OpcodeBranchJump() { Distance = 2 });

            // if we short circuited, it was true
            string shortCircuitLabel = AddOpcode(new OpcodePush(true)).Label;

            // make all the skips jump to the short circuit value
            foreach (var skipOp in skipOps)
            {
                skipOp.DestinationLabel = shortCircuitLabel;
            }
        }

        public void VisitExpression(AndExpressionNode node)
        {
            NodeStartHousekeeping(node.ParseNode);

            List<Opcode> skipOps = new List<Opcode>();

            for (int i = 0; i < node.Expressions.Length; i++)
            {
                node.Expressions[i].Accept(this);
                if (i != node.Expressions.Length - 1)
                {
                    // for all but the last expression, we short circuit when false
                    skipOps.Add(AddOpcode(new OpcodeBranchIfFalse()));
                }
            }

            // jump over the short circuit value
            AddOpcode(new OpcodeBranchJump() { Distance = 2 });

            // if we short circuited, it was false
            string shortCircuitLabel = AddOpcode(new OpcodePush(false)).Label;

            // make all the skips jump to the short circuit value
            foreach (var skipOp in skipOps)
            {
                skipOp.DestinationLabel = shortCircuitLabel;
            }
        }

        public void VisitExpression(CompareExpressionNode node)
        {
            NodeStartHousekeeping(node.ParseNode);

            node.Left.Accept(this);
            node.Right.Accept(this);

            switch (node.Comparator)
            {
            case "<":
                AddOpcode(new OpcodeCompareLT());
                break;
            case "<=":
                AddOpcode(new OpcodeCompareLTE());
                break;
            case ">=":
                AddOpcode(new OpcodeCompareGTE());
                break;
            case ">":
                AddOpcode(new OpcodeCompareGT());
                break;
            case "=":
                AddOpcode(new OpcodeCompareEqual());
                break;
            case "<>":
                AddOpcode(new OpcodeCompareNE());
                break;
            default:
                throw new KOSYouShouldNeverSeeThisException("Unknown comparator: " + node.Comparator);
            }
        }

        public void VisitExpression(AddExpressionNode node)
        {
            NodeStartHousekeeping(node.ParseNode);

            node.Left.Accept(this);
            node.Right.Accept(this);
            AddOpcode(new OpcodeMathAdd());
        }

        public void VisitExpression(SubtractExpressionNode node)
        {
            NodeStartHousekeeping(node.ParseNode);

            node.Left.Accept(this);
            node.Right.Accept(this);
            AddOpcode(new OpcodeMathSubtract());
        }

        public void VisitExpression(MultiplyExpressionNode node)
        {
            NodeStartHousekeeping(node.ParseNode);

            node.Left.Accept(this);
            node.Right.Accept(this);
            AddOpcode(new OpcodeMathMultiply());
        }

        public void VisitExpression(DivideExpressionNode node)
        {
            NodeStartHousekeeping(node.ParseNode);

            node.Left.Accept(this);
            node.Right.Accept(this);
            AddOpcode(new OpcodeMathDivide());
        }

        public void VisitExpression(PowerExpressionNode node)
        {
            NodeStartHousekeeping(node.ParseNode);

            node.Left.Accept(this);
            node.Right.Accept(this);
            AddOpcode(new OpcodeMathPower());
        }

        public void VisitExpression(NegateExpressionNode node)
        {
            NodeStartHousekeeping(node.ParseNode);

            node.Target.Accept(this);
            AddOpcode(new OpcodeMathNegate());
        }

        public void VisitExpression(NotExpressionNode node)
        {
            NodeStartHousekeeping(node.ParseNode);

            node.Target.Accept(this);
            AddOpcode(new OpcodeLogicNot());
        }

        public void VisitExpression(DefinedExpressionNode node)
        {
            NodeStartHousekeeping(node.ParseNode);

            AddOpcode(new OpcodePush("$" + node.Identifier));
            AddOpcode(new OpcodeExists());
        }

        public void VisitExpression(GetSuffixNode node)
        {
            NodeStartHousekeeping(node.ParseNode);

            node.Base.Accept(this);
            AddOpcode(new OpcodeGetMember(node.Suffix));
        }

        public void VisitExpression(CallSuffixNode node)
        {
            NodeStartHousekeeping(node.ParseNode);

            node.Base.Accept(this);
            AddOpcode(new OpcodeGetMethod(node.Suffix));

            AddOpcode(new OpcodePush(new KOSArgMarkerType()));
            foreach (var arg in node.Arguments)
            {
                arg.Accept(this);
            }

            AddOpcode(new OpcodeCall(string.Empty) { Direct = false });
        }

        public void VisitExpression(GetIndexNode node)
        {
            NodeStartHousekeeping(node.ParseNode);

            node.Base.Accept(this);
            node.Index.Accept(this);
            AddOpcode(new OpcodeGetIndex());
        }

        public void VisitExpression(DirectCallNode node)
        {
            NodeStartHousekeeping(node.ParseNode);

            AddOpcode(new OpcodePush(new KOSArgMarkerType()));
            foreach (var arg in node.Arguments)
            {
                arg.Accept(this);
            }

            string directName = node.Identifier;
            if (options.FuncManager.Exists(directName)) // if the name is a built-in, then add the "()" after it.
                directName += "()";
            AddOpcode(new OpcodeCall(directName));
        }

        public void VisitExpression(IndirectCallNode node)
        {
            NodeStartHousekeeping(node.ParseNode);

            node.Base.Accept(this);

            AddOpcode(new OpcodePush(new KOSArgMarkerType()));
            foreach (var arg in node.Arguments)
            {
                arg.Accept(this);
            }

            AddOpcode(new OpcodeCall(string.Empty) { Direct = false });
        }

        public void VisitExpression(FunctionAddressNode node)
        {
            NodeStartHousekeeping(node.ParseNode);


            if (options.FuncManager.Exists(node.Identifier)) // if the name is a built-in, then make a BuiltInDelegate
            {
                AddOpcode(new OpcodePush(new KOSArgMarkerType()));
                AddOpcode(new OpcodePush(node.Identifier));
                AddOpcode(new OpcodeCall("makebuiltindelegate()"));
            }
            else
            {
                // It is not a built-in, so instead get its value as a user function pointer variable, despite 
                // the fact that it's being called AS IF it was direct.
                AddOpcode(new OpcodePush("$" + node.Identifier + "*"));
            }
        }

        public void VisitExpression(LambdaNode node)
        {
            Opcode skipPastFunctionBody = AddOpcode(new OpcodeBranchJump());
            string functionStartLabel = GetNextLabel(false);

            needImplicitReturn = true;
            nextBraceIsFunction = true;
            VisitNode(node.ParseNode); // the braces of the anonymous function and its contents get compiled in-line here.
            nextBraceIsFunction = false;
            if (needImplicitReturn)
                // needImplicitReturn is unconditionally true here, but it's being used anyway so we'll find this block
                // of code later when we search for "all the places using needImplicitReturn" and perform a refactor
                // of the logic for adding implicit returns.
            {
                AddOpcode(new OpcodePush(0)); // Functions must push a dummy return val when making implicit returns. Locks already leave an expr atop the stack.
                AddOpcode(new OpcodeReturn(0));
            }
            Opcode afterFunctionBody = AddOpcode(new OpcodePushDelegateRelocateLater(null,true), functionStartLabel);
            skipPastFunctionBody.DestinationLabel = afterFunctionBody.Label;
        }

        public void VisitExpression(ScalarAtomNode node)
        {
            NodeStartHousekeeping(node.ParseNode);

            AddOpcode(new OpcodePush(node.Value));
        }

        public void VisitExpression(StringAtomNode node)
        {
            NodeStartHousekeeping(node.ParseNode);

            AddOpcode(new OpcodePush(new StringValue(node.Value)));
        }

        public void VisitExpression(BooleanAtomNode node)
        {
            NodeStartHousekeeping(node.ParseNode);

            AddOpcode(new OpcodePush(new BooleanValue(node.Value)));
        }

        public void VisitExpression(IdentifierAtomNode node)
        {
            NodeStartHousekeeping(node.ParseNode);

            // Special case when the identifier is known to be a lock.
            // Note that this only works when the lock is defined in the SAME
            // file.  When one script calls another one, the compiler won't know
            // that the identifier is a lock, and you'll have to use empty parens
            // to make it a real function call like var():
            UserFunction userFuncObject = GetUserFunctionWithScopeWalk(node.Identifier, node.ParseNode);
            if (userFuncObject != null)
            {
                AddOpcode(new OpcodePush(new KOSArgMarkerType()));
                AddOpcode(new OpcodeCall(userFuncObject.ScopelessPointerIdentifier));
            }
            else
            {
                AddOpcode(new OpcodePush("$" + node.Identifier));
            }
        }
    }
}
