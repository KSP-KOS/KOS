using System;
using kOS.Safe.Communication;

namespace kOS.Communication
{
    public class MessageQueue : GenericMessageQueue<Message, PlanetariumTimeProvider>
    {
        // No need to define anything inside this class body.  Everything is
        // defined by what classes are chosen above for the generic fill-ins.
    }
}

