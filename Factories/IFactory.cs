using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using kOS.Screen;

namespace kOS.Factories
{
    public interface IFactory
    {
        Interpreter CreateInterpreter(SharedObjects shared);
    }
}
