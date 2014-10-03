using System;

namespace kOS.Safe.Exceptions
{
    /// <summary>
    /// All exceptions that kOS throws deliberately should implement
    /// this interface (as well as being subclasses of System.Exception
    /// of course)
    /// </summary>
    public interface IKOSException
    {
        /// <summary>
        /// The long verbose version of the message. (the terse version
        /// should just be the normal message inherited from Exception)
        /// </summary>
        string VerboseMessage {get;set;}
        
        /// <summary>
        /// A location where there might be even more information about
        /// the exception.
        /// </summary>
        string HelpURL {get;set;}
    }
}
