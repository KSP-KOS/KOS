using System;
using System.Collections.Generic;
using System.Text;
using kOS.Safe.Utilities;
using kOS.Safe.Encapsulation;

namespace kOS.Safe.Execution
{
    public class Stack : IStack
    {
        private const int MAX_ARGUMENT_STACK_SIZE = 3000;
        private const int MAX_SCOPE_STACK_SIZE = 3000;
        private readonly object[] stack = new object[MAX_ARGUMENT_STACK_SIZE];
        private int count = 0;
        private readonly object[] scopeStack = new object[MAX_ARGUMENT_STACK_SIZE];
        private int scopeCount = 0;

        private int triggerContextCount = 0;

        public void Push(object item)
        {
            ThrowIfInvalid(item);
            if (count + scopeCount >= MAX_ARGUMENT_STACK_SIZE) {
                // TODO: make an IKOSException for this:
                throw new Exception("Stack overflow!!");
            }
            stack[count++] = ProcessItem(item);

            checkTrigger(item, +1);
        }

        public object Pop()
        {
            if (count == 0)
            {
                return null;
            }

            object item = stack[--count];
            stack[count] = 0; // remove our reference to the item

            checkTrigger(item, -1);

            return item;
        }

        public void PushScope(object item)
        {
            ThrowIfInvalid(item);
            if (count + scopeCount >= MAX_ARGUMENT_STACK_SIZE)
            {
                // TODO: make an IKOSException for this:
                throw new Exception("Stack overflow!!");
            }
            scopeStack[scopeCount++] = ProcessItem(item);

            checkTrigger(item, +1); 
        }

        public object PopScope()
        {
            if (scopeCount == 0)
            {
                return null;
            }

            object item = scopeStack[--scopeCount];
            scopeStack[scopeCount] = 0;

            checkTrigger(item, -1);

            return item;
        }

        private void checkTrigger(object item, int diff) {
            SubroutineContext sr = item as SubroutineContext;
            if (sr != null && sr.IsTrigger) {
                triggerContextCount += diff;
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
        /// Slightly "cheats" and breaks out of the 'stack' model by allowing you to view the contents of
        /// somewhere on the stack that is underneath the topmost thing.  You can only peek, but not pop
        /// values this way.
        /// </summary>
        /// <param name="digDepth">How far underneath the top to look.  Zero means peek at the top,
        /// 1 means peek at the item just under the top, 2 means peek at the item just under that, and
        /// so on.  Note you CAN peek a negative number, which looks at the secret stack above the
        /// stack - where the subroutine contexts and local variable contexts are.</param>
        /// <returns>The object at that depth.  Returns null when digDepth is too large and the stack isn't
        /// big enough to dig that deep.  Note that this conflates with the case where there really is a
        /// null stored on the stack and makes it impossible to tell the difference between peeking too far
        /// versus actually finding a null.  If you need to know the difference, use PeekCheck.</returns>
        public object Peek(int digDepth)
        {
            object returnVal;
            PeekCheck(digDepth, out returnVal);
            return returnVal;
        }

        /// <summary>
        /// Slightly "cheats" and breaks out of the 'stack' model by allowing you to view the contents of
        /// somewhere on the stack that is underneath the topmost thing.  You can only peek, but not pop
        /// values this way.  It returns both the object found there (as an out parameter) and a boolean for
        /// whether or not your peek attempt went out of bounds of the stack.
        /// </summary>
        /// <param name="digDepth">How far underneath the top to look.  Zero means peek at the top,
        /// 1 means peek at the item just under the top, 2 means peek at the item just under that, and
        /// so on.  Note you CAN peek a negative number, which looks at the secret stack above the
        /// stack - where the subroutine contexts and local variable contexts are.</param>
        /// <param name="item">The object at that depth.  Will be null when digDepth is too large and the stack isn't
        /// big enough to dig that deep, but it also could return null if the actual value stored there on
        /// the stack really is a null.  If you need to be certain of the difference, use the return value.</param>
        /// <returns>Returns true if your peek was within the bounds of the stack, or false if you tried
        /// to peek too far and went past the top or bottom of the stack.</returns>
        public bool PeekCheck(int digDepth, out object item)
        {
            bool returnVal = false;
            item = null;
            if (digDepth < 0)
            {
                int index = scopeCount + digDepth; // -1 means top of stack
                if (index >= 0)
                {
                    returnVal = true;
                    item = scopeStack[index];
                }
            }
            else
            {
                int index = count - digDepth - 1; // 0 means top of stack
                if (index >= 0)
                {
                    returnVal = true;
                    item = stack[index];
                }
            }
            return returnVal;
        }

        /// <summary>
        /// Returns the logical size of the current stack (not its potentially larger storage size).
        /// </summary>
        /// <returns>How many items are currently on the stack.</returns>
        public int GetLogicalSize()
        {
            return count; // This doesn't count the secret stack
        }

        public void Clear()
        {
            while (count > 0)
            {
                count--;
                stack[count] = null; // remove our references to the items
            }
            while (scopeCount > 0)
            {
                scopeCount--;
                scopeStack[scopeCount] = null;
            }
            triggerContextCount = 0;
        }

        public string Dump()
        {
            try
            {
                var builder = new StringBuilder();
                builder.AppendLine("Stack dump:");

                builder.AppendLine("Stack: count = " + count);
                for (int index = count - 1; index >= 0; index--)
                {
                    object item = stack[index];
                    dumpItem(index, index == count - 1, item, builder);
                }

                builder.AppendLine("Scope Stack: count = " + scopeCount);
                for (int index = scopeCount - 1; index >= 0; index--)
                {
                    object item = scopeStack[index];
                    dumpItem(index, false, item, builder);
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
                builder.AppendFormat("          ScopeId={0}, ParentScopeId={1}, ParentSkipLevels={2} IsClosure={3}",
                                             dict.ScopeId, dict.ParentScopeId, dict.ParentSkipLevels, dict.IsClosure);
                builder.AppendLine();
                // Dump the local variable context stored here on the stack:
                foreach (string varName in dict.Variables.Keys)
                {
                    var value = dict.Variables[varName].Value;
                    builder.AppendFormat("            local var {0} is {1} with value = {2}", varName, KOSNomenclature.GetKOSName(value.GetType()), dict.Variables[varName].Value);
                    builder.AppendLine();
                }
            }
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
        /// any still triggers in the stack, this returns true, else false.  
        /// </summary>
        /// <returns>True if the current call stack indicates that either we are
        /// inside a trigger, or are inside code that was in turn indirectly called
        /// from a trigger.  False if we are in mainline code instead.</returns>
        public bool HasTriggerContexts()
        {
            return triggerContextCount > 0;
        }
    }
}