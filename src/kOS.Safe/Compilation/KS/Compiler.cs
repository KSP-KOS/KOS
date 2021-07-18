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
    class Compiler
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
        private bool compilingSetDestination;
        private bool identifierIsVariable;
        private bool identifierIsSuffix;
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
            compilingSetDestination = false;
            identifierIsSuffix = false;
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

#pragma warning disable CS0162
            if (TRACE_PARSE)
                SafeHouse.Logger.Log("traceParse: visiting node: " + node.Token.Type.ToString() + ", " + node.Token.Text);
#pragma warning restore CS0162

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
                case TokenType.CHOOSE:
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
                case TokenType.on_stmt:
                    PreProcessChildNodes(node);
                    PreProcessOnStatement(node);
                    break;
                case TokenType.when_stmt:
                    PreProcessChildNodes(node);
                    PreProcessWhenStatement(node);
                    break;
                default:
                    PreProcessChildNodes(node);
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
            VisitNode(node.Nodes[1]);
            AddOpcode(new OpcodeEval());
            AddOpcode(new OpcodeDup());
            // Put one of those two copies of the new value into the old value identifier for next time.
            // This is local because triggers have scope and this will keep multiple instances of the
            // same ON trigger (i.e. executing the ON statement in a loop) to each have thier own copy
            // of thier own OldValue.
            AddOpcode(new OpcodeStoreLocal(triggerObject.OldValueIdentifier));
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
            AddOpcode(new OpcodeStoreLocal(triggerKeepName));

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
            VisitNode(node.Nodes[1]);
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
            AddOpcode(new OpcodeStoreLocal(triggerKeepName));

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
                    branch.Distance = 3;
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

                VisitNode(bodyNode);

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
                case TokenType.arglist:
                    VisitArgList(node);
                    break;
                case TokenType.compare_expr: // for issue #20
                case TokenType.arith_expr:
                case TokenType.multdiv_expr:
                case TokenType.factor:
                    VisitExpressionChain(node);
                    break;
                case TokenType.expr:
                    VisitExpr(node);
                    break;
                case TokenType.ternary_expr:
                    VisitTernary(node);
                    break;
                case TokenType.or_expr:
                case TokenType.and_expr:
                    VisitShortCircuitBoolean(node);
                    break;
                case TokenType.suffix:
                    VisitSuffix(node);
                    break;
                case TokenType.unary_expr:
                    VisitUnaryExpression(node);
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
                // This never gets called anymore, but it's left here as
                // a comment so future programmers who search for it will
                // find this comment and realized that it's not an error
                // for it to be missing.  It's missing-ness is deliberate:
                // case TokenType.suffixterm:
                //    VisitSuffixTerm(node);
                //    break;
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
                case TokenType.directive:
                    VisitDirective(node);
                    break;
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

        private void VisitVariableNode(ParseNode node)
        {
            NodeStartHousekeeping(node);
            identifierIsVariable = true;
            VisitNode(node);
            identifierIsVariable = false;
        }

        /// <summary>
        /// Performs the work for a number of different expressions that all
        /// share the following universal basic properties:<br/>
        /// - They contain optional binary operators.<br/>
        /// - The terms are all at the same precedence level.<br/>
        /// - Because of the tie of precedence level, the terms are to be evaluated left-to-right.<br/>
        /// - No special extra work is needed, such that simply doing "push expr1, push expr2, then do operator" is all that's needed.<br/>
        /// <br/>
        /// Examples:<br/>
        ///   5 + 4 - x + 2 // because + and - are in the same parse rule, these all get the same flat precedence.<br/>
        ///   x * y * z<br/>
        /// In cases like that where all the operators "tie", the entire chain of terms lives in the same ParseNode,<br/>
        /// and we have to unroll those terms and presume left-to-right precedence.  That is what this method does.<br/>
        /// </summary>
        /// <param name="node"></param>
        private void VisitExpressionChain(ParseNode node)
        {
            NodeStartHousekeeping(node);
            if (node.Nodes.Count > 1)
            {
                // it should always be odd, two arguments and one operator
                if ((node.Nodes.Count % 2) != 1) return;

                VisitNode(node.Nodes[0]); // pushes lefthand side on stack.

                int nodeIndex = 2;
                while (nodeIndex < node.Nodes.Count)
                {
                    VisitNode(node.Nodes[nodeIndex]); // pushes righthand side on stack.
                    nodeIndex -= 1;
                    VisitNode(node.Nodes[nodeIndex]); // operator, i.e '*', '+', '-', '/', etc.
                    nodeIndex += 3; // Move to the next term over (if there's more than 2 terms in the chain).

                    // If there are more terms to process, then the value that the operation leaves behind on the stack
                    // from operating on these two terms will become the 'lefthand side' for the next iteration of this loop.
                }
            }
            else if (node.Nodes.Count == 1)
            {
                VisitNode(node.Nodes[0]); // This ParseNode isn't *realy* an expression of binary operators, because
                                          // the regex chain of "zero or more" righthand terms.. had zero such terms.
                                          // So just delve in deeper to compile whatever part of speech it is further down.
            }
        }

        /// <summary>
        /// The outermost expression level, which may be a normal expression,
        /// or may be an anonymous function, depending of if it's got braces.
        /// </summary>
        /// <param name="node"></param>
        private void VisitExpr(ParseNode node)
        {
            NodeStartHousekeeping(node);
            
            // If it's an instruction block then it's an anonymous function, so
            // compile the function body right here, while branching around it so
            // it won't execute just yet, and instead just push a UserDelegate of it
            // onto the stack as the value of this expression:
            if (node.Nodes[0].Token.Type == TokenType.instruction_block)
            {
                Opcode skipPastFunctionBody = AddOpcode(new OpcodeBranchJump());
                string functionStartLabel = GetNextLabel(false);
                
                needImplicitReturn = true;
                nextBraceIsFunction = true;
                VisitNode(node.Nodes[0]); // the braces of the anonymous function and its contents get compiled in-line here.
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
            else // ordinary expression - just descend to the next level of the tree and eval the expression as normal:
            {
                VisitNode(node.Nodes[0]);
            }
        }

        private void VisitTernary(ParseNode node)
        {
            NodeStartHousekeeping(node);

            // Syntax pattern is:
            // [0] = keyword CHOOSE
            // [1] = expression returned if true
            // [2] = keyword IF
            // [3] = expression with boolean value
            // [4] = keyword ELSE
            // [5] = expression returned if false

            VisitNode(node.Nodes[3]); // eval the boolean clause, put on stack.

            Opcode bypassTrue = AddOpcode(new OpcodeBranchIfFalse());

            VisitNode(node.Nodes[1]); // expression if true.
            Opcode bypassFalse = AddOpcode(new OpcodeBranchJump());

            bypassTrue.DestinationLabel = GetNextLabel(false);

            VisitNode(node.Nodes[5]); // expression if false.

            bypassFalse.DestinationLabel = GetNextLabel(false);
            addBranchDestination = true;
        }

        /// <summary>
        /// Handles the short-circuit logic of boolean OR and boolean AND
        /// chains.  It is like VisitExpressionChain (see elsewhere) but
        /// in this case it has the special logic to short circuit and skip
        /// executing the righthand expression if it can.  (The generic VisitExpressionChain
        /// always evaluates both the left and right sides of the operator first, then
        /// does the operation).
        /// </summary>
        /// <param name="node"></param>
        private void VisitShortCircuitBoolean(ParseNode node)
        {
            NodeStartHousekeeping(node);
            
            if (node.Nodes.Count > 1)
            {
                // it should always be odd, two arguments and one operator
                if ((node.Nodes.Count % 2) != 1) return;

                // Determine if this is a chain of ANDs or a chain or ORs.  The parser will
                // never mix ANDs and ORs into the same ParseNode level.  We are guaranteed
                // that all the operators in this chain match the first operator in the chain:
                // That guarantee is important.  Without it, we can't do short-circuiting like this
                // because you can't short-circuit a mix of AND and OR at the same precedence.
                TokenType operation = node.Nodes[1].Token.Type; // Guaranteed to be either TokenType.AND or TokenType.OR
                
                // For remembering the instruction pointers from which short-circuit branch jumps came:
                List<int> shortCircuitFromIndeces = new List<int>();
                
                int nodeIndex = 0;
                while (nodeIndex < node.Nodes.Count)
                {
                    if (nodeIndex > 0) // After each term, insert the branch test (which consumes the expr from the stack regardless of if it branches):
                    {
                       shortCircuitFromIndeces.Add(currentCodeSection.Count());
                       if (operation == TokenType.AND)
                           AddOpcode(new OpcodeBranchIfFalse());
                       else if (operation == TokenType.OR)
                           AddOpcode(new OpcodeBranchIfTrue());
                       else
                           throw new KOSException("Assertion check:  Broken kerboscript compiler (VisitShortCircuitBoolean).  See kOS devs");
                    }
                    
                    VisitNode(node.Nodes[nodeIndex]); // pushes the next term onto the stack.
                    nodeIndex += 2; // Skip the operator, moving to the next term over.
                }
                // If it gets to the end of all that and it still hasn't aborted, then the whole expression's
                // Boolean value is just the value of its lastmost term, that's already gotten pushed atop the stack.
                // Leave the lastmost term there, and just skip ahead past the short-circuit landing target:
                OpcodeBranchJump skipShortCircuitTarget = new OpcodeBranchJump();
                skipShortCircuitTarget.Distance = 2; // Hardcoded +2 jump distance skips the upcoming OpcodePush and just lands on
                                                     // whatever comes next after this VisitNode.  Avoids using DestinationLabel
                                                     // for later relocation because it would be messy to reassign this label later
                                                     // in whatever VisitNode happens to come up next, when that could be anything.
                AddOpcode(skipShortCircuitTarget);
                
                // Build the instruction all the short circuit checks will jump to if aborting partway through.
                // (AND's abort when they're false.  OR's abort when they're true.)
                AddOpcode(operation == TokenType.AND ? new OpcodePush(false) : new OpcodePush(true));
                string shortCircuitTargetLabel = currentCodeSection[currentCodeSection.Count()-1].Label;
                
                // Retroactively re-assign the jump labels of all the short circuit branch operations:
                foreach (int index in shortCircuitFromIndeces)
                {
                    currentCodeSection[index].DestinationLabel = shortCircuitTargetLabel;
                }
            }
            else if (node.Nodes.Count == 1)
            {
                VisitNode(node.Nodes[0]); // This ParseNode isn't *realy* an expression of AND or OR operators, because
                                          // the regex chain of "zero or more" righthand terms.. had zero such terms.
                                          // So just delve in deeper to compile whatever part of speech it is further down.
            }
        }
        
        private void VisitUnaryExpression(ParseNode node)
        {
            NodeStartHousekeeping(node);
            if (node.Nodes.Count <= 0) return;

            bool addNegation = false;
            bool addNot = false;
            bool addDefined = false;
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
            else if (node.Nodes[0].Token.Type == TokenType.DEFINED)
            {
                nodeIndex++;
                addDefined = true;
            }
            
            VisitNode(node.Nodes[nodeIndex]);

            if (addNegation)
            {
                AddOpcode(new OpcodeMathNegate());
            }
            if (addNot)
            {
                AddOpcode(new OpcodeLogicNot());
            }
            if (addDefined)
            {
                AddOpcode(new OpcodeExists());
            }
        }

        private void VisitAtom(ParseNode node)
        {
            NodeStartHousekeeping(node);

            if (node.Nodes[0].Token.Type == TokenType.BRACKETOPEN)
            {
                VisitNode(node.Nodes[1]);
            }
            else
            {
                VisitNode(node.Nodes[0]);
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
                StringBuilder sb = new StringBuilder();
                sb.Append(node.Nodes[0].Nodes[0].Token.Text); // have to use the sub-node of double or integer
                for (int i = 1; i < node.Nodes.Count; ++i)
                {
                    sb.Append(node.Nodes[i].Token.Text);
                }
                string parseText = sb.ToString();
                ScalarValue val;
                if (ScalarValue.TryParse(parseText, out val))
                {
                    AddOpcode(new OpcodePush(val));
                }
                else
                    throw new KOSCompileException(node.Token, string.Format(KOSNumberParseException.TERSE_MSG_FMT, parseText));
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
            ScalarValue val;
            if (ScalarValue.TryParseInt(node.Token.Text.Replace("_", ""), out val) ||
                ScalarValue.TryParseDouble(node.Token.Text.Replace("_", ""), out val) // fallback if number is too big for an integer.
               )
            {
                AddOpcode(new OpcodePush(val));
            }
            else
                throw new KOSCompileException(node.Token, string.Format(KOSNumberParseException.TERSE_MSG_FMT, node.Token.Text));
        }

        private void VisitDouble(ParseNode node)
        {
            NodeStartHousekeeping(node);
            ScalarValue val;
            if (ScalarValue.TryParseDouble(node.Token.Text.Replace("_",""), out val))
            {
                AddOpcode(new OpcodePush(val));
            }
            else
                throw new KOSCompileException(node.Token, string.Format(KOSNumberParseException.TERSE_MSG_FMT, node.Token.Text));
        }

        private void VisitTrueFalse(ParseNode node)
        {
            NodeStartHousekeeping(node);
            bool boolValue;
            if (bool.TryParse(node.Token.Text, out boolValue))
            {
                AddOpcode(new OpcodePush(new BooleanValue(boolValue)));
            }
        }

        private void VisitOnOffTrailer(ParseNode node)
        {
            NodeStartHousekeeping(node);
            AddOpcode(new OpcodePush((node.Nodes[0].Token.Type == TokenType.ON)));
        }

        /// <summary>
        /// Do the work for function calls.
        /// </summary>
        /// <param name="node">parse node for the function term of the parse tree.</param>
        /// <param name="isDirect">true if it should make an OpcodeCall that is Direct, false if it should make an indirect one.
        /// See the documentation for OpcodeCall.Direct for the full explanation of the difference.  If isDirect is true, then
        /// the name to the left of the parentheses will be the name of the function call or the name of the
        /// identifier variable that holds the function's jump address in the case of user functions.  But in either case
        /// the important thing is that when isDirect is true, that means the OpcodeCall uses the Opcode's argument to
        /// decide where to call.  On the other hand, if isDirect is false, then it will
        /// presume the function name, delegate, or branch index was
        /// already placed atop the stack by other parts of this compiler, rather than encoding it into the
        /// OpcodeCall's argument itself.</param>
        /// <param name="directName">In the case where it's a direct function, what's the name of it?  In the case
        /// where it's not direct, this argument doesn't matter.</param>
        private void VisitActualFunction(ParseNode node, bool isDirect, string directName = "")
        {
            NodeStartHousekeeping(node);

            ParseNode trailerNode = node; // the function_trailer rule is here.

            if (trailerNode.Nodes.Count > 0 && trailerNode.Nodes[0].Token.Type == TokenType.ATSIGN)
            {
                BuildFunctionDelegate(isDirect, directName);
                return;
            }

            // Need to tell OpcodeCall where in the stack the bottom of the arg list is.
            // Even if there are no arguments, it still has to be TOLD that by showing
            // it the marker atop the stack with nothing above it.
            AddOpcode(new OpcodePush(new KOSArgMarkerType()));

            if (trailerNode.Nodes[1].Token.Type == TokenType.arglist)
            {
                // Some of the flags remembering the context of
                // what we were inside of in the parse tree aren't
                // appropriate to be using while evaluating the function's
                // argument terms in the list:
                bool rememberIsSuffix = identifierIsSuffix;
                identifierIsSuffix = false;
                bool rememberCompilingSetDestination = compilingSetDestination;
                compilingSetDestination = false;

                // Now compile the arguments in the list:
                VisitNode(trailerNode.Nodes[1]);

                // And then return the flags to their original condition:
                compilingSetDestination = rememberCompilingSetDestination;
                identifierIsSuffix = rememberIsSuffix;
            }

            if (isDirect)
            {
                if (options.FuncManager.Exists(directName)) // if the name is a built-in, then add the "()" after it.
                    directName += "()";
                AddOpcode(new OpcodeCall(directName));
            }
            else
            {
                var op = new OpcodeCall(string.Empty) { Direct = false };
                AddOpcode(op);
            }

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
                VisitNode(node.Nodes[nodeIndex]);
                nodeIndex -= 2;
            }
        }
        
        /// <summary>
        /// When a function identifier or suffix ends in '@' where parentheses could have gone,
        /// then its not really being called like a function.  Instead it's being asked to generate
        /// a delegate of itself to be put atop the stack.
        /// This builds the code that does that.
        /// </summary>
        /// <param name="isDirect">If true, then the directName is the name of the function being called or the user
        /// variable holding the function delegate to be called.  If false, then the compiler should have built code
        /// that will have left a suffix or function reference atop the stack already.</param>
        /// <param name="directName">only needed when isDirect is true</param>
        private void BuildFunctionDelegate(bool isDirect, string directName = "")
        {
            if (isDirect)
            {
                if (options.FuncManager.Exists(directName)) // if the name is a built-in, then make a BuiltInDelegate
                {
                    AddOpcode(new OpcodePush(new KOSArgMarkerType()));
                    AddOpcode(new OpcodePush(directName));
                    AddOpcode(new OpcodeCall("makebuiltindelegate()"));
                }
                else
                {
                    // It is not a built-in, so instead get its value as a user function pointer variable, despite 
                    // the fact that it's being called AS IF it was direct.
                    if (!directName.EndsWith("*")) directName = directName + "*";
                    if (!directName.StartsWith("$")) directName = "$" + directName;
                    AddOpcode(new OpcodePush(directName));
                }
            }
            // Else we shouldn't have to do any work because the thing atop the stack will already
            // be a suffix delegate.
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
                bool isUserFunc = false;
                if (nodeIndex == 0)
                {
                    firstIdentifier = GetIdentifierText(suffixTerm);
                    UserFunction userFuncObject = GetUserFunctionWithScopeWalk(firstIdentifier, node);
                    if (userFuncObject != null && !compilingSetDestination)
                    {
                        firstIdentifier = userFuncObject.ScopelessPointerIdentifier;
                        isUserFunc = true;
                    }
                }
                // The term starts with either an identifier or an expression.  If it's the start, then parse
                // it as a variable, else parse it as a raw identifier:
                bool rememberIsV = identifierIsVariable;
                identifierIsVariable = (!startsWithFunc) && nodeIndex == 0;

                // when we are setting a member value we need to leave
                // the last object and the last suffix in the stack
                bool usingSetMember = (suffixTerm.Nodes.Count > 0) && (compilingSetDestination && nodeIndex == (node.Nodes.Count - 1));

                // Push this term on the stack unless it's the name of the user function or built-in function or a suffix:
                bool isDirect = true;

                if (usingSetMember && suffixTerm.Nodes.Count == 1)
                {
                    // If this is the name of a suffix that we are setting, don't do anything with it.
                    // ProcessSetOperation will handle putting the suffix name into the opcode.
                }
                else if (nodeIndex != 0 && !usingSetMember)
                {
                    string suffixName = GetIdentifierText(suffixTerm.Nodes[0]);
                    AddOpcode(startsWithFunc ? new OpcodeGetMethod(suffixName) : new OpcodeGetMember(suffixName));
                    isDirect = false;
                }
                else if (!isUserFunc && (nodeIndex > 0 || !startsWithFunc))
                {
                    VisitNode(suffixTerm.Nodes[0]);
                    isDirect = false;
                }
                identifierIsVariable = rememberIsV;

                // The remaining terms are a chain of function_trailers "(...)" and array_trailers "[...]" or "#.." in any arbitrary order:
                for (int trailerIndex = 1; trailerIndex < suffixTerm.Nodes.Count; ++trailerIndex)
                {
                    // suffixterm_trailer is always a wrapper around either function_trailer or array_trailer,
                    // so delve down one level to get which of them it is:
                    ParseNode trailerTerm = suffixTerm.Nodes[trailerIndex].Nodes[0];
                    bool isFunc = (trailerTerm.Token.Type == TokenType.function_trailer);
                    bool isArray = (trailerTerm.Token.Type == TokenType.array_trailer);
                    bool thisTermIsDirect = (isDirect && trailerIndex == 1); // only the firstmost term in a chain can be direct.

                    if (isFunc || isUserFunc)
                    {
                        // direct if it's just one term like foo(aaa) but indirect
                        // if it's a list of suffixes like foo:bar(aaa):
                        VisitActualFunction(trailerTerm, thisTermIsDirect, firstIdentifier);
                    }
                    if (isArray)
                    {
                        VisitActualArray(trailerTerm);
                    }
                }
                
                // In the case of a lock function without parentheses, it needs this special case:
                if (suffixTerm.Nodes.Count <= 1)
                {
                    if (isDirect && isUserFunc)
                    {
                        AddOpcode(new OpcodePush(new KOSArgMarkerType()));
                        AddOpcode(new OpcodeCall(firstIdentifier));
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

                // Temporarily turn off these flags while evaluating the expression inside
                // the array index square brackets.  These flags apply to this outer containing
                // thing, the array access, not to the expression in the index brackets:
                bool rememberIdentIsSuffix = identifierIsSuffix;
                identifierIsSuffix = false;
                bool rememberCompSetDest = compilingSetDestination;
                compilingSetDestination = false;
                
                VisitNode(trailerNode.Nodes[nodeIndex]); // pushes the result of expression inside square brackets.

                compilingSetDestination = rememberCompSetDest;
                identifierIsSuffix = rememberIdentIsSuffix;

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
        
        // The fact that there is no VisitSuffixTerm method is not an omission or mistake.
        // All the logic of this node of the parse tree is now handled by the parent nodes
        // that come above this one instead.  I'm leaving this comment here so that future programmers
        // searching this code don't attempt to fix this "mistake" by adding this method back in:
        // private void VisitSuffixTerm(ParseNode node)
        // {
        //     // nothing here anymore.
        // }
        
        private void VisitIdentifier(ParseNode node)
        {
            NodeStartHousekeeping(node);
            bool isVariable = (identifierIsVariable && !identifierIsSuffix);
            string prefix = isVariable ? "$" : String.Empty;
            string identifier = GetIdentifierText(node);
            
            // Special case when the identifier is known to be a lock.
            // Note that this only works when the lock is defined in the SAME
            // file.  When one script calls another one, the compiler won't know
            // that the identifier is a lock, and you'll have to use empty parens
            // to make it a real function call like var():
            UserFunction userFuncObject = GetUserFunctionWithScopeWalk(identifier, node);
            if (isVariable && userFuncObject != null)
            {
                AddOpcode(new OpcodeCall(userFuncObject.ScopelessPointerIdentifier));
            }
            else
            {
                AddOpcode(new OpcodePush(prefix + identifier));
            }
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

        private void VisitFileIdent(ParseNode node)
        {
            NodeStartHousekeeping(node);
            string identifier = GetIdentifierText(node);
            AddOpcode(new OpcodePush(identifier));
        }

        private void VisitString(ParseNode node)
        {
            NodeStartHousekeeping(node);
            string value = node.Token.Text;
            bool shouldEscape = true;
            if (value[0] == '@')
            {
                value = value.Substring(1);
                shouldEscape = false;
            }
            // Can't use Trim('"') because that cuts ALL trailing and leading quotes,
            // while we want to only cut the first and last quote char:
            if (value.EndsWith("\""))
                value = value.Substring(0, value.Length - 1);
            if (value.StartsWith("\""))
                value = value.Substring(1);
            if (shouldEscape)
                value = value.Replace("\"\"", "\"");
            
            AddOpcode(new OpcodePush(new StringValue(value)));
        }

        /// <summary>
        /// Given a ParseNode that should be somewhere in the Suffix term portion of a parse tree,
        /// return the Suffix trailer or suffix term at the end of it that has the final suffix identifier.
        /// If the term doesn't end with a suffix identifier, then this returns null.
        /// </summary>
        /// <returns>The variable identifier end suffix.</returns>
        /// <param name="node">Node.</param>
        private ParseNode FindVarIdentifierEndNode(ParseNode node)
        {
            // If it's a var_identifier being worked on, drop down one level first
            // to get into the actual meat of the syntax tree it represents:
            if (node.Token.Type == TokenType.varidentifier)
                return FindVarIdentifierEndNode(node.Nodes.First());

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
            if (descendedThroughAColon &&
                (prevChild.Token.Type == TokenType.suffix_trailer ||
                 prevChild.Token.Type == TokenType.suffixterm))
            {
                return prevChild;
            }
            return null;
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
            return FindVarIdentifierEndNode(node) != null;
        }

        ///<summary>
        /// Get the rightmost suffix in a var_identifier.
        /// i.e. return "BBB" if the var_identifier is:<br/>
        ///    AAA:BBB, or<br/>
        ///    AAA[0]:BBB,<br/>
        /// </summary>
        private string GetVarIdentifierEndSuffix(ParseNode node)
        {
            ParseNode endingNode = FindVarIdentifierEndNode(node);
            if (endingNode == null)
                throw new KOSYouShouldNeverSeeThisException("VarIdentifier didn't end in a suffix");
            return GetIdentifierText(endingNode);
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
        /// point given to find the first occurrence of a node of the given token type.<br/>
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

            for (int i = 1; i < node.Nodes.Count; i += 4)
            {
                ProcessSetOperation(node.Nodes[i], node.Nodes[i + 2]);
            }
        }

        /// <summary>
        /// For any statement of the form "SET THIS TO THAT", or "THIS ON" or "THIS OFF".
        /// </summary>
        /// <param name="setThis">The lefthand-side expression to be set</param>
        /// <param name="toThis">The righthand-side expression to set it to</param>
        private void ProcessSetOperation(ParseNode setThis, ParseNode toThis)
        {
            bool isSuffix = VarIdentifierEndsWithSuffix(setThis);
            bool isIndex = VarIdentifierEndsWithIndex(setThis);
            if (isSuffix || isIndex)
            {
                // destination
                compilingSetDestination = true;
                VisitNode(setThis);
                compilingSetDestination = false;

                // expression
                VisitNode(toThis);

                if (isSuffix)
                {
                    string suffixName = GetVarIdentifierEndSuffix(setThis);
                    AddOpcode(new OpcodeSetMember(suffixName));
                }
                else
                {
                    AddOpcode(new OpcodeSetIndex());
                }
            }
            else
            {
                // normal variable set
                VisitNode(toThis);

                string identifier = GetIdentifierText(setThis);

                UserFunction userFuncObject = GetUserFunctionWithScopeWalk(identifier, setThis);
                if (userFuncObject != null)
                {
                    UnlockIdentifier(userFuncObject);
                }

                string varName = "$" + identifier;

                if (allowLazyGlobal)
                    AddOpcode(new OpcodeStore(varName));
                else
                    AddOpcode(new OpcodeStoreExist(varName));
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
            for (int i = 0 ; i < node.Nodes.Count ; ++i)
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
                    AddOpcode(new OpcodeAddTrigger(false, InterruptPriority.RecurringControl));
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
                // remove update trigger
                string triggerIdentifier = "lock-" + lockObject.ScopelessIdentifier;
                if (context.Triggers.Contains(triggerIdentifier))
                {
                    Trigger triggerObject = context.Triggers.GetTrigger(triggerIdentifier);
                    AddOpcode(new OpcodePushRelocateLater(null), triggerObject.GetFunctionLabel());
                    AddOpcode(new OpcodeRemoveTrigger());
                }
                // disable this FlyByWire parameter
                AddOpcode(new OpcodePush(new KOSArgMarkerType()));
                AddOpcode(new OpcodePush(lockObject.ScopelessIdentifier));
                AddOpcode(new OpcodePush(false));
                AddOpcode(new OpcodeCall("toggleflybywire()"));
                // add a pop to clear out the dummy return value from toggleflybywire()
                AddOpcode(new OpcodePop());

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
                VisitNode(node.Nodes[1]); // the expression in the on statement.
                AddOpcode(new OpcodeStore(triggerObject.OldValueIdentifier));
                AddOpcode(new OpcodePushRelocateLater(null), triggerObject.GetFunctionLabel());
                AddOpcode(new OpcodeAddTrigger(InterruptPriority.Recurring));
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
                AddOpcode(new OpcodeAddTrigger(InterruptPriority.Recurring));
            }
        }

        private void VisitWaitStatement(ParseNode node)
        {
            NodeStartHousekeeping(node);

            if (node.Nodes.Count == 3)
            {
                // For commands of the form:  WAIT N. where N is a number:
                VisitNode(node.Nodes[1]);
                AddOpcode(new OpcodeWait());
            }
            else
            {
                // For commands of the form:  WAIT UNTIL expr. where expr is any boolean expression:
                Opcode waitLoopStart = AddOpcode(new OpcodePush(0));       // Loop start: Gives OpcodeWait an argument of zero.
                AddOpcode(new OpcodeWait());                               // Avoid busy polling.  Even a WAIT 0 still forces 1 fixedupdate 'tick'.
                VisitNode(node.Nodes[2]);                                  // Inserts instructions here to evaluate the expression
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
                for (int i = 0; i < lastSubNode.Nodes.Count; i += 4) {
                    VisitNode(lastSubNode.Nodes[i + 2]);
                    AddOpcode(CreateAppropriateStoreCode(whereToStore, true, "$" + GetIdentifierText(lastSubNode.Nodes[i])));
                }
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
                
                VisitNode(expressionNode); // evals init expression on the top of the stack where the arg would have been

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
            StorageModifier modifier = StorageModifier.LOCAL;
            // locks are default global:
            if (lastSubNode.Token.Type == TokenType.declare_lock_clause)
            {
                modifier = StorageModifier.GLOBAL;
            }
            // functions declared at file scope are default global. inner functions are default local:
            else if (lastSubNode.Token.Type == TokenType.declare_function_clause)
            {
                ParseNode containingNode = GetContainingBlockNode(node);
                if (containingNode != null && containingNode.Token.Type == TokenType.Start) // file scope
                    modifier = StorageModifier.GLOBAL;
                else
                    modifier = StorageModifier.LOCAL;
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
            string varName = "$" + GetIdentifierText(node.Nodes[1]);
            VisitVarIdentifier(node.Nodes[1]);
            AddOpcode(new OpcodeLogicToBool());
            AddOpcode(new OpcodeLogicNot());
            if (allowLazyGlobal)
                AddOpcode(new OpcodeStore(varName));
            else
                AddOpcode(new OpcodeStoreExist(varName));
        }

        private void VisitPrintStatement(ParseNode node)
        {
            NodeStartHousekeeping(node);
            if (node.Nodes.Count == 3)
            {
                AddOpcode(new OpcodePush(new KOSArgMarkerType()));
                VisitNode(node.Nodes[1]);
                AddOpcode(new OpcodeCall("print()"));
                AddOpcode(new OpcodePop()); // all functions now return a value even if it's a dummy we ignore.
            }
            else
            {
                AddOpcode(new OpcodePush(new KOSArgMarkerType()));
                VisitNode(node.Nodes[1]);
                VisitNode(node.Nodes[4]);
                VisitNode(node.Nodes[6]);
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
            VisitNode(node.Nodes[1]);
            AddOpcode(new OpcodeCall("add()"));
            AddOpcode(new OpcodePop()); // all functions now return a value even if it's a dummy we ignore.
        }

        private void VisitRemoveStatement(ParseNode node)
        {
            NodeStartHousekeeping(node);
            AddOpcode(new OpcodePush(new KOSArgMarkerType()));
            VisitNode(node.Nodes[1]);
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
            VisitNode(node.Nodes[1]);
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
                VisitNode(node.Nodes[progNameIndex]); // put program name on stack.
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
                VisitNode(node.Nodes[progNameIndex]);

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
            VisitNode(node.Nodes[1]);
            AddOpcode(new OpcodePush(false));
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
            AddOpcode(new OpcodePop()); // all functions now return a value even if it's a dummy we ignore.
        }

        private void VisitSwitchStatement(ParseNode node)
        {
            NodeStartHousekeeping(node);
            AddOpcode(new OpcodePush(new KOSArgMarkerType()));
            VisitNode(node.Nodes[2]);
            AddOpcode(new OpcodeCall("switch()"));
            AddOpcode(new OpcodePop()); // all functions now return a value even if it's a dummy we ignore.
        }

        private void VisitCopyStatement(ParseNode node)
        {
            NodeStartHousekeeping(node);
            AddOpcode(new OpcodePush(new KOSArgMarkerType()));
            VisitNode(node.Nodes[1]);

            AddOpcode(new OpcodePush(node.Nodes[2].Token.Type == TokenType.FROM ? "from" : "to"));

            VisitNode(node.Nodes[3]);
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

            VisitNode(node.Nodes[oldNameIndex]);
            VisitNode(node.Nodes[newNameIndex]);

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
            VisitNode(node.Nodes[1]);

            if (node.Nodes.Count == 5)
                VisitNode(node.Nodes[3]);
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
                VisitNode(node.Nodes[1]);
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
                    VisitNode(node.Nodes[1]);
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
            VisitNode(node.Nodes[1]);
            VisitNode(node.Nodes[3]);
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
                VisitNode(node.Nodes[1]);
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

            VisitNode(node.Nodes[3]);
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
                VisitVariableNode(node.Nodes[1]);
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
    }
}
