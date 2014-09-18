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
