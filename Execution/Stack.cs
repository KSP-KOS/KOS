using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using kOS.Suffixed;

namespace kOS.Execution
{
    public class Stack
    {
        private const int MAX_STACK_SIZE = 1000;
        private List<object> _stack = new List<object>();
        private int _stackPointer = -1;

        public void Push(object item)
        {
            string message = string.Empty;
            
            if (IsValid(item, ref message))
            {
                _stackPointer++;
                if (_stackPointer < MAX_STACK_SIZE)
                    _stack.Insert(_stackPointer, item);
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
            if (Config.GetInstance().EnableSafeMode)
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

        public object Pop()
        {
            object item = null;

            if (_stack.Count > 0)
            {
                item = _stack[_stackPointer];
                _stack.RemoveAt(_stackPointer);
                _stackPointer--;
            }

            return item;
        }

        public void MoveStackPointer(int delta)
        {
            _stackPointer += delta;
        }

        public void Clear()
        {
            _stack.Clear();
            _stackPointer = -1;
        }
    }
}
