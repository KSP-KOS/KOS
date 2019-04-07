using System;
using kOS.Safe.Communication;

namespace kOS.Communication
{
    public class MessageQueue : GenericMessageQueue<Message, PlanetariumTimeProvider>
    {
        // No need to define anything inside this class body.  Everything is
        // defined by what classes are chosen above for the generic fill-ins.

        // Required for all IDumpers for them to work, but can't enforced by the interface because it's static:
        public static MessageQueue CreateFromDump(kOS.Safe.SafeSharedObjects shared, kOS.Safe.Dump d)
        {
            var newObj = new MessageQueue();
            newObj.LoadDump(d);
            return newObj;
        }
    }
}

