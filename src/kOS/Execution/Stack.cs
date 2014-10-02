using System;
using System.Collections.Generic;
using System.Text;
using kOS.Safe.Execution;
using kOS.Suffixed;

namespace kOS.Execution
{
    public class Stack
    {
        private const int MAX_STACK_SIZE = 1000;
        private readonly List<object> stack = new List<object>();
        private int stackPointer = -1;

        public void Push(object item)
        {
            string message = string.Empty;

            if (!IsValid(item, ref message))
            {
                throw new ArgumentException(message);
            }

            stackPointer++;
            if (stackPointer < MAX_STACK_SIZE)
            {
                stack.Insert(stackPointer, ProcessItem(item));
            }
            else
                throw new Exception("Stack overflow!!");
        }

        private bool IsValid(object item, ref string message)
        {
            if (Config.Instance.EnableSafeMode)
            {
                if (item is double)
                {
                    if (Double.IsNaN((double)item))
                    {
                        message = "Tried to push NaN into the stack.";
                        return false;
                    }
                    if (Double.IsInfinity((double)item))
                    {
                        message = "Tried to push Infinity into the stack.";
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Make any conversion needed before pushing an item onto the stack
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private object ProcessItem(object item)
        {
            if (item is float)
                // promote floats to doubles
                return Convert.ToDouble(item);
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
        /// so on.</param>
        /// <returns>The object at that depth.  Returns null when digDepth is too large and the stack isn't
        /// big enough to dig that deep.</returns>
        public object Peek(int digDepth)
        {
            if (digDepth > stackPointer)
                return null;
            else
                return stack[stackPointer - digDepth];
        }
        
        /// <summary>
        /// Returns the logical size of the curent stack (not its potentially larger storage size).
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

        public string Dump(int lineCount)
        {
            var builder = new StringBuilder();
            builder.AppendLine("Stack dump:");

            // Print in reverse order so the top of the stack is on top of the printout:
            // (actually given the double nature of the stack, one of the two sub-stacks
            // inside it will always be backwardly printed):
            for (int index = stack.Count-1 ; index >= 0 ; --index)
            {
                builder.AppendLine(string.Format("{0:000} {1,4} {2}", index, (index==stackPointer ? "SP->" : "" ), stack[index]));
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
            for (int index = stackPointer+1 ; index < stack.Count ; ++index)
            {
                if (stack[index] is SubroutineContext)
                {
                    trace.Add( ((SubroutineContext)(stack[index])).CameFromInstPtr - 1 );
                }
            }
            return trace;
        }
    }
}
