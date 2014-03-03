using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using kOS.Screen;

namespace kOS.Factories
{
    public class StandardFactory : IFactory
    {
        public Interpreter CreateInterpreter(SharedObjects shared)
        {
            return new Interpreter(shared);
        }
    }
}
