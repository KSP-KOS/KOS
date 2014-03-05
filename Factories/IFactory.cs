using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using kOS.Screen;
using kOS.Persistence;

namespace kOS.Factories
{
    public interface IFactory
    {
        Interpreter CreateInterpreter(SharedObjects shared);
        Archive CreateArchive();
    }
}
