using System;
using System.Collections.Generic;
using kOS.Suffixed;
using kOS.Safe.Exceptions;

namespace kOS
{
    public class Logger
    {
        protected SharedObjects Shared;

        // It's not implemented yet, but the intent of this is to give the user the ability
        // to see help data about old exceptions other than just the current one, perhaps
        // in a help log window or something.  At the moment nothing uses it yet.
        protected List<Exception> exceptionHistory = new List<Exception>();
        
        public Logger()
        {
        }

        public Logger(SharedObjects shared)
        {
            Shared = shared;
        }

        public virtual void Log(string text)
        {
            LogToScreen(text);
        }

        public virtual void Log(Exception e)
        {
            string lineRule = "__________________________________________\n";

            string message = e.Message;
            
            if (Config.Instance.VerboseExceptions && e is IKOSException )
            {
                // As a first primitive attempt at excercising the verbose exceptions,
                // Just use a CONFIG setting for how verbose to be.  This will need
                // to be replaced with something more sophisticated later, most likely.

                message += "\n" + lineRule + "           VERBOSE DESCRIPTION\n";
                
                message += ((IKOSException)e).VerboseMessage;
                
                // Fallback if there was no verbose message defined:
                if (message == String.Empty)
                    message += e.Message;
                message += lineRule;
                
                // Take on the URL if there is one:
                string url = ((IKOSException)e).HelpURL;
                if (url != String.Empty)
                    message += "\n\nMore Information at:\n" + url + "\n";
                message += lineRule;
            }
            LogToScreen(message);
            
            exceptionHistory.Add(e);
        }

        protected void LogToScreen(string text)
        {
            if (Shared != null && Shared.Screen != null)
            {
                Shared.Screen.Print(text);
            }
        }
        
        // TODO: Provide a user interface that will dig into the exceptionHistory and let
        // players see the exceptions with the option to get their VerboseMessages if they
        // happen to be IKOSExceptions (remember the history will also contain System
        // exceptions too, so be careful to check first that it is an IKOSException before
        // trying to get its VerboseMessage).
        //
        // Ideas: a "HELP ERROR" command that just prints the verbose message of the
        // most recent error would be a simple start, but a more complex version might
        // pop open a window and let you scroll a list of them and click them for the
        // verbose message.  At any rate, that's a design decision for later, and it
        // probably won't live here in the code, but be elsewhere.  Just leave this
        // comment here as a reminder to come back to it.
        //
    }
}
