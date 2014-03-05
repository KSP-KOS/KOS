using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using kOS.Screen;
using kOS.Persistence;

namespace kOS.Factories
{
    public class StandardFactory : IFactory
    {
        public Interpreter CreateInterpreter(SharedObjects shared)
        {
            return new Interpreter(shared);
        }

        public Archive CreateArchive()
        {
            return new Archive();
        }
    }
}
