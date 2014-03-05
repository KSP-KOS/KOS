using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using kOS.Factories;
using kOS.Screen;
using kOS.Persistence;

namespace kOS.AddOns.RemoteTech2
{
    public class RemoteTechFactory : IFactory
    {
        public Interpreter CreateInterpreter(SharedObjects shared)
        {
            return new RemoteTechInterpreter(shared);
        }

        public Archive CreateArchive()
        {
            return new RemoteTechArchive();
        }
    }
}
