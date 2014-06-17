using System;
using System.Collections.Generic;
using System.Text;
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
            
            if (IsValid(item, ref message))
            {
                stackPointer++;
                if (stackPointer < MAX_STACK_SIZE)
                    stack.Insert(stackPointer, ProcessItem(item));
                else
                    throw new Exception("Stack overflow!!");
            }
            else
            {
                throw new ArgumentException(message);
            }
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

            int startIndex = Math.Max(0, stack.Count - lineCount);
            
            for(int index = startIndex; index < stack.Count; index++)
                builder.AppendLine(string.Format("{0:000}    {1}", index, stack[index]));

            return builder.ToString();
        }
    }
}
