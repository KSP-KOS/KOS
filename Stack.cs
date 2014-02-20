using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace kOS
{
    public class Stack
    {
        private List<object> _stack = new List<object>();
        private int _stackPointer = -1;

        public void Push(object item)
        {
            _stackPointer++;
            _stack.Insert(_stackPointer, item);
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
