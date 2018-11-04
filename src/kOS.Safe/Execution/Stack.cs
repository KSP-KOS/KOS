using System;
using System.Collections.Generic;
using System.Text;
using kOS.Safe.Utilities;
using kOS.Safe.Encapsulation;
using kOS.Safe.Exceptions;

namespace kOS.Safe.Execution
{
    public class Stack : IStack
    {
        private const int MAX_ARGUMENT_STACK_SIZE = 3000;
        private const int MAX_SCOPE_STACK_SIZE = 3000;
        /// <summary>
        /// The argument stack that holds values passed to functions, and arguments
        /// for pending expressions that are not done evaluating yet.  It should only
        /// get large if you have an expression that contains recursion.  For all non-
        /// recursive expressions, it should remain small.
        /// It is implemented as a fixed capacity array regardless of how much is used,
        /// for speed reasons.
        /// </summary>
        private readonly object[] argumentStack = new object[MAX_ARGUMENT_STACK_SIZE];
        /// <summary>
        /// The count of how much of the argument stack is in use.  It is the index
        /// of where the next push will happen, just above the top of the stack.
        /// </summary>
        private int argumentCount = 0;
        /// <summary>
        /// The scope stack that holds function come-from instruction pointers, and
        /// variable scopes containing dictionaries of local variables.  The size depends
        /// on how nested your function calls and curly braces get.
        /// It is implemented as a fixed capacity array regardless of how much is used,
        /// for speed reasons.
        /// </summary>
        private readonly object[] scopeStack = new object[MAX_SCOPE_STACK_SIZE];
        /// <summary>
        /// The count of how much of the scope stack is in use.  It is the index
        /// of where the next push will happen, just above the top of the stack.
        /// </summary>
        private int scopeCount = 0;

        private int triggerContextCount = 0;
        private int delayingTriggerContextCount = 0;

        /// <summary>
        /// Push to the argument stack.
        /// </summary>
        /// <param name="item">Item that should be derived from Structure or at least convertable to Structure with FromPrimitive</param>
        public void PushArgument(object item)
        {
            ThrowIfInvalid(item);
            if (argumentCount >= MAX_ARGUMENT_STACK_SIZE) {
                throw new KOSStackOverflowException("Argument");
            }
            argumentStack[argumentCount++] = ProcessItem(item);

            AdjustTriggerCountIfNeeded(item, +1);
        }

        /// <summary>
        /// Pop from the argument stack.
        /// </summary>
        /// <returns>The item popped or null if stack is exhausted.</returns>
        public object PopArgument()
        {
            if (argumentCount == 0)
            {
                return null;
            }

            object item = argumentStack[--argumentCount];
            argumentStack[argumentCount] = 0; // remove our reference to the item

            AdjustTriggerCountIfNeeded(item, -1);

            return item;
        }

        /// <summary>
        /// Push to the scope stack
        /// </summary>
        /// <param name="item">Item that should be either a VariableScope or a SubroutineContext.</param>
        public void PushScope(object item)
        {
            ThrowIfInvalid(item);
            if (scopeCount >= MAX_SCOPE_STACK_SIZE)
            {
                throw new KOSStackOverflowException("Scope");
            }
            scopeStack[scopeCount++] = ProcessItem(item);

            AdjustTriggerCountIfNeeded(item, +1); 
        }

        /// <summary>
        /// Pop from the scope stack.  Returns null if stack exhausted.
        /// </summary>
        /// <returns>The object popped, that shouild be either a VariableScope or a SubroutineContext.</returns>
        public object PopScope()
        {
            if (scopeCount == 0)
            {
                return null;
            }

            object item = scopeStack[--scopeCount];
            scopeStack[scopeCount] = 0;

            AdjustTriggerCountIfNeeded(item, -1);

            return item;
        }

        /// <summary>
        /// Call after just pushing or popping from the scope stack.  Show it the
        /// item that was popped, and if it is a trigger context, then it will adjust
        /// the triggerContextCount by the diff you give it.
        /// </summary>
        /// <param name="item">Item.</param>
        /// <param name="diff">Diff.</param>
        private void AdjustTriggerCountIfNeeded(object item, int diff)
        {
            SubroutineContext sr = item as SubroutineContext;
            if (sr != null && sr.IsTrigger)
            {
                triggerContextCount += diff;
                if (sr.Trigger != null && ! sr.Trigger.IsImmediateTrigger)
                    delayingTriggerContextCount += diff;
            }
        }

        private void ThrowIfInvalid(object item)
        {
            if (!SafeHouse.Config.EnableSafeMode)
                return;
            if (!(item is double || item is float || item is ScalarValue))
                return;

            double unboxed = Convert.ToDouble(item);

            if (double.IsNaN(unboxed))
            {
                // TODO: make an IKOSException for this:
                throw new Exception("Tried to push NaN into the stack.");
            }
            if (double.IsInfinity(unboxed))
            {
                // TODO: make an IKOSException for this:
                throw new Exception("Tried to push Infinity into the stack.");
            }
        }

        /// <summary>
        /// Make any conversion needed before pushing an item onto the stack
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private object ProcessItem(object item)
        {
            if (item is float)
            {
                // promote floats to doubles
                item = Convert.ToDouble(item);
            }
            if (item is double)
            {
                var doubleItem = (double)item;
                var outOfBounds = int.MinValue < doubleItem && doubleItem < int.MaxValue;

                if (!double.IsNaN(doubleItem) && outOfBounds)
                {
                    int intItem = Convert.ToInt32(item);
                    if (doubleItem == intItem)
                        item = intItem;
                }
            }
            return item;
        }

        /// <summary>
        /// Peeks at a value on the argument stack.
        /// Slightly "cheats" and breaks out of the 'stack' model by allowing you to view the contents of
        /// somewhere on the stack that is underneath the topmost thing.  You can only peek, but not pop
        /// values this way.
        /// </summary>
        /// <param name="digDepth">How far underneath the top to look.  Zero means peek at the top,
        /// 1 means peek at the item just under the top, 2 means peek at the item just under that, and
        /// so on.</param>
        /// <returns>The object at that depth.  Returns null when digDepth is too large and the stack isn't
        /// big enough to dig that deep.  Note that this conflates with the case where there really is a
        /// null stored on the stack and makes it impossible to tell the difference between peeking too far
        /// versus actually finding a null.  If you need to know the difference, use PeekCheck.</returns>
        public object PeekArgument(int digDepth)
        {
            object returnVal;
            PeekCheckArgument(digDepth, out returnVal);
            return returnVal;
        }

        /// <summary>
        /// Peeks at a value in the argument stack.
        /// Slightly "cheats" and breaks out of the 'stack' model by allowing you to view the contents of
        /// somewhere on the stack that is underneath the topmost thing.  You can only peek, but not pop
        /// values this way.  It returns both the object found there (as an out parameter) and a boolean for
        /// whether or not your peek attempt went out of bounds of the stack.
        /// </summary>
        /// <param name="digDepth">How far underneath the top to look.  Zero means peek at the top,
        /// 1 means peek at the item just under the top, 2 means peek at the item just under that, and
        /// so on.</param>
        /// <param name="item">The object at that depth.  Will be null when digDepth is too large and the stack isn't
        /// big enough to dig that deep, but it also could return null if the actual value stored there on
        /// the stack really is a null.  If you need to be certain of the difference, use the return value.</param>
        /// <returns>Returns true if your peek was within the bounds of the stack, or false if you tried
        /// to peek too far and went past the top or bottom of the stack.</returns>
        public bool PeekCheckArgument(int digDepth, out object item)
        {
            bool returnVal = false;
            item = null;
            if (digDepth < 0)
            {
                throw new KOSYouShouldNeverSeeThisException("Somewhere the kOS developers are still using a negative stack peek instead of PeekCheckScope");
            }
            else
            {
                int index = argumentCount - digDepth - 1; // 0 means top of stack
                if (index >= 0)
                {
                    returnVal = true;
                    item = argumentStack[index];
                }
            }
            return returnVal;
        }

        /// <summary>
        /// Peeks at a value in the scope stack.
        /// Slightly "cheats" and breaks out of the 'stack' model by allowing you to view the contents of
        /// somewhere on the stack that is underneath the topmost thing.  You can only peek, but not pop
        /// values this way.
        /// </summary>
        /// <param name="digDepth">How far underneath the top to look.  Zero means peek at the top,
        /// 1 means peek at the item just under the top, 2 means peek at the item just under that, and
        /// so on.</param>
        /// <returns>The callstack subroutine context or variable scope dictionary at that depth.
        /// Returns null when digDepth is too large and the stack isn't big enough to dig that deep.</returns>
        public object PeekScope(int digDepth)
        {
            object returnVal;
            PeekCheckScope(digDepth, out returnVal);
            return returnVal;
        }

        /// <summary>
        /// Peeks at a value in the scope stack.
        /// Slightly "cheats" and breaks out of the 'stack' model by allowing you to view the contents of
        /// somewhere on the stack that is underneath the topmost thing.  You can only peek, but not pop
        /// values this way.
        /// </summary>
        /// <param name="digDepth">How far underneath the top to look.  Zero means peek at the top,
        /// 1 means peek at the item just under the top, 2 means peek at the item just under that, and
        /// so on.</param>
        /// <param name="item">The callstack subroutine context or variable scope dictionary at that depth.
        /// </param>
        /// <returns>False if the digDepth was too big and there's not that much stack there.</returns>
        public bool PeekCheckScope(int digDepth, out object item)
        {
            bool returnVal = false;
            item = null;
            int index = scopeCount - digDepth - 1;
            if (index >= 0)
            {
                returnVal = true;
                item = scopeStack[index];
            }
            return returnVal;
        }

        /// <summary>
        /// Returns the logical size of the current stack (not its potentially larger storage size).
        /// </summary>
        /// <returns>How many items are currently on the stack.</returns>
        public int GetArgumentStackSize()
        {
            return argumentCount; // This doesn't count the secret stack
        }

        public void Clear()
        {
            while (argumentCount > 0)
            {
                argumentCount--;
                argumentStack[argumentCount] = null; // remove our references to the items
            }
            while (scopeCount > 0)
            {
                scopeCount--;
                scopeStack[scopeCount] = null;
            }
            triggerContextCount = 0;
            delayingTriggerContextCount = 0;
        }

        public string Dump()
        {
            try
            {
                var builder = new StringBuilder();
                builder.AppendLine("Argument Stack dump:");

                builder.AppendLine("Argument Stack: count = " + argumentCount);
                for (int index = argumentCount - 1; index >= 0; index--)
                {
                    object item = argumentStack[index];
                    dumpItem(index, index == argumentCount - 1, item, builder);
                }

                builder.AppendLine("Scope Stack: count = " + scopeCount);
                for (int index = scopeCount - 1; index >= 0; index--)
                {
                    object item = scopeStack[index];
                    dumpItem(index, index == scopeCount - 1, item, builder);
                }


                return builder.ToString();
            }
            catch (Exception ex)
            {
                return string.Format("Error creating stack dump, contact kOS devs.\n{0}\n\n{1}", ex.Message, ex.StackTrace);
            }
        }

        private void dumpItem(int index, bool isSP, object item, StringBuilder builder)
        {
            builder.AppendLine(string.Format("{0:000} {1,4} {2} (type: {3})", index, (isSP ? "SP->" : ""),
                                             (item == null ? "<null>" : item.ToString()),
                                             (item == null ? "<n/a>" : KOSNomenclature.GetKOSName(item.GetType()))));
            builder.AppendLine();
            VariableScope dict = item as VariableScope;
            if (dict != null)
            {
                Int16 parentScopeId = -1;
                if (dict.ParentScope != null)
                {
                    parentScopeId = dict.ParentScope.ScopeId;
                }

                builder.AppendFormat("          ScopeId={0}, ParentScopeId={1}, IsClosure={2}",
                                             dict.ScopeId, parentScopeId, dict.IsClosure);
                builder.AppendLine();
                // Dump the local variable context stored here on the stack:
                foreach (var entry in dict.Locals)
                {
                    builder.AppendFormat("            local var {0} is {1} with value = {2}", entry.Key, KOSNomenclature.GetKOSName(entry.Value.GetType()), entry.Value);
                    builder.AppendLine();
                }
            }
        }

        /// <summary>
        /// Finds a scope with the given ID number, from the top down.
        /// If no such scope is found, it returns null.
        /// </summary>
        /// <returns>The scope.</returns>
        /// <param name="ScopeId">Scope identifier.</param>
        public VariableScope FindScope(Int16 ScopeId)
        {
            for (int index = scopeCount - 1; index >= 0; --index)
            {
                var scope = scopeStack[index] as VariableScope;
                if (scope != null && scope.ScopeId == ScopeId)
                {
                    return scope;
                }
            }
            return null;
        }

        public VariableScope GetCurrentScope()
        {
            for (int index = scopeCount - 1; index >= 0; --index)
            {
                var scope = scopeStack[index] as VariableScope;
                if (scope != null)
                {
                    return scope;
                }
            }
            return null;
        }

        /// <summary>
        /// Gets the subroutine context of the currently executing routine from the stack,
        /// or returns null if we are not inside a subroutine.
        /// </summary>
        /// <returns>The current subroutine context.</returns>
        public SubroutineContext GetCurrentSubroutineContext()
        {
            for (int index = scopeCount - 1; index >= 0; --index)
            {
                var context = scopeStack[index] as SubroutineContext;
                if (context != null)
                {
                    return context;
                }
            }
            return null;
        }

        /// <summary>
        /// Gets the subroutine contexts of any calls matching the trigger given
        /// which have already been pushed onto the call stack for execution.
        /// </summary>
        /// <returns>List of matches, zero length if no matches.</returns>
        public List<SubroutineContext> GetTriggerCallContexts(TriggerInfo trigger)
        {
            List<SubroutineContext> returnList = new List<SubroutineContext>();
            for (int index = scopeCount - 1; index >= 0; --index)
            {
                var context = scopeStack[index] as SubroutineContext;

                // Note must use .Equals(), not == below:  For TriggerInfo, == still means
                // reference-equals because there's places we need that.  For this case, we
                // want to match all equivalent triggers (ones that execute the same subroutine):
                if (context != null && context.IsTrigger && context.Trigger.Equals(trigger))
                    returnList.Add(context);
            }
            return returnList;
        }

        /// <summary>
        /// Return the subroutine call trace of how the code got to where it is right now.
        /// </summary>
        /// <returns>The items in the list are the instruction pointers of the Opcodecall instructions
        /// that got us to here.</returns>
        public List<int> GetCallTrace()
        {
            var trace = new List<int>();
            for (int index = scopeCount - 1; index >= 0; --index)
            {
                if (scopeStack[index] is SubroutineContext)
                {
                    trace.Add(((SubroutineContext)(scopeStack[index])).CameFromInstPtr - 1);
                }
            }
            return trace;
        }
        
        /// <summary>
        /// This stack tracks all its pushes and pops to know whether or not it
        /// contains subroutine contexts which are from triggers.  If there are
        /// still any such triggers in the stack, this returns true, else false.  
        /// </summary>
        /// <returns>True if the current call stack indicates that either we are
        /// inside a trigger, or are inside code that was in turn indirectly called
        /// from a trigger.  False if we are in mainline code.</returns>
        public bool HasTriggerContexts()
        {
            return triggerContextCount > 0;
        }

        /// <summary>
        /// This stack tracks all its pushes and pops to know whether or not it
        /// contains subroutine contexts which are from "delaying" triggers.  If there are
        /// still any such "delaying" triggers in the stack, this returns true, else false.
        /// A "delaying" trigger is one that isn't immediate - meaning one that nicely waits
        /// for the next KOSFixedUpdate before it will fire off.
        /// </summary>
        /// <returns>True if the current call stack indicates that either we are
        /// inside a "delaying" trigger, or are inside code that was in turn indirectly called
        /// from a "delaying" trigger.  False if we are in mainline code or triggers
        /// that are immediate.</returns>
        public bool HasDelayingTriggerContexts()
        {
            return delayingTriggerContextCount > 0;
        }
    }
}