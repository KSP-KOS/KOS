using System;
using kOS.Safe.Communication;

namespace kOS.Communication
{
    public class MessageQueue : GenericMessageQueue<Message>
    {
        public MessageQueue() : base(new PlanetariumTimeProvider())
        {
        }
    }
}

