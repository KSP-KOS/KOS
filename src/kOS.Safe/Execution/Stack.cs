using System;
using System.Collections.Generic;
using System.Text;
using kOS.Safe.Utilities;
using kOS.Safe.Encapsulation;

namespace kOS.Safe.Execution
{
    public class Stack : IStack
    {
        private const int MAX_STACK_SIZE = 3000;
        private readonly List<object> stack = new List<object>();
        private int stackPointer = -1;

        public void Push(object item)
        {
            ThrowIfInvalid(item);

            stackPointer++;
            if (stack.Count < MAX_STACK_SIZE)
            {
                stack.Insert(stackPointer, ProcessItem(item));
            }
            else
                // TODO: make an IKOSException for this:
                throw new Exception("Stack overflow!!");
        }

        private void ThrowIfInvalid(object item)
        {
            if (!SafeHouse.Config.EnableSafeMode)
                return;
            if (!(item is double || item is float || item is ScalarDoubleValue))
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

        public object Pop()
        {
            object item = null;

            if (stack.Count > 0)
            {
                item = stack[stackPointer];
                stack.RemoveAt(stackPointer);
                stackPointer--;
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
            int index = stackPointer - digDepth;
            bool returnVal = (index >= 0 && index < stack.Count);
            item = returnVal ? stack[index] : null;
            return returnVal;
        }

        /// <summary>
        /// Returns the logical size of the current stack (not its potentially larger storage size).
        /// </summary>
        /// <returns>How many items are currently on the stack.</returns>
        public int GetLogicalSize()
        {
            return stackPointer + 1;
        }

        public void MoveStackPointer(int delta)
        {
            stackPointer += delta;
        }

        public void Clear()
        {
            stack.Clear();
            stackPointer = -1;
        }

        public string Dump()
        {
            var builder = new StringBuilder();
            builder.AppendLine("Stack dump: stackPointer = " + stackPointer);

            // Print in reverse order so the top of the stack is on top of the printout:
            // (actually given the double nature of the stack, one of the two sub-stacks
            // inside it will always be backwardly printed):
            for (int index = stack.Count - 1; index >= 0; --index)
            {
                object item = stack[index];
                builder.AppendLine(string.Format("{0:000} {1,4} {2}", index, (index == stackPointer ? "SP->" : ""), item));
                VariableScope dict = item as VariableScope;
                if (dict != null)
                {
                    builder.AppendFormat("          ScopeId={0}, ParentScopeId={1}, ParentSkipLevels={2} IsClosure={3}",
                                         dict.ScopeId, dict.ParentScopeId, dict.ParentSkipLevels, dict.IsClosure);
                    builder.AppendLine();
                    // Dump the local variable context stored here on the stack:
                    foreach (string varName in dict.Variables.Keys)
                    {
                        builder.AppendFormat("            local var {0} is {1} with value = {2}", varName, varName.GetType().FullName, dict.Variables[varName].Value);
                        builder.AppendLine();
                    }
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// Return the subroutine call trace of how the code got to where it is right now.
        /// </summary>
        /// <returns>The items in the list are the instruction pointers of the Opcodecall instructions
        /// that got us to here.</returns>
        public List<int> GetCallTrace()
        {
            var trace = new List<int>();
            for (int index = stackPointer + 1; index < stack.Count; ++index)
            {
                if (stack[index] is SubroutineContext)
                {
                    trace.Add(((SubroutineContext)(stack[index])).CameFromInstPtr - 1);
                }
            }
            return trace;
        }
    }
}