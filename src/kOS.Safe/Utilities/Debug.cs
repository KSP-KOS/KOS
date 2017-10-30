using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using kOS.Safe.Compilation;
using kOS.Safe.Persistence;

namespace kOS.Safe.Utilities
{
    public static class Debug
    {
        static Debug ()
        {
            IDGenerator = new ObjectIDGenerator();
        }

        public enum NagType
        {
            /// <summary>Make message go away</summary>
            SHUTUP = 0,
            /// <summary>Report this just once, then revert to SHUTUP after that</summary>
            NAGONCE,
            /// <summary>Always give the nag message every time the terminal welcome is printed</summary>
            NAGFOREVER
        }

        private class NagMessage
        {
            public NagType Type { get; private set; }
            public string Message { get; private set; }
            public NagMessage(NagType n, string msg)
            {
                Type = n;
                Message = msg;
            }
        }
        private static readonly List<NagMessage> nags = new List<NagMessage>();
        /// <summary>
        /// Add a string message that should be shown on the terminal
        /// the next time it shows its Welcome message.
        /// It is possible to chain several of these messages together,
        /// but remember that the terminal window is small.  Keep
        /// the message short so there's room for other nag messages too.
        /// </summary>
        /// <param name="nag">Should the message be shown once, or keep being shown
        /// every time the terminal welcome message appears?</param>
        /// <param name="message">Message to print</param>
        public static void AddNagMessage(NagType nag, string message)
        {
            nags.Add( new NagMessage(nag, message) );
        }
        /// <summary>
        /// Gets a list of all the pending nag messages,
        /// and in the process of doing that it clears out any
        /// that were set to just NAGONCE.
        /// </summary>
        /// <returns></returns>
        public static List<string> GetPendingNags()
        {
            var returnVal = nags.Select(nag => nag.Message).ToList();

            // Only keep the NAGFOREVER ones:
            nags.RemoveAll(nag => nag.Type != NagType.NAGFOREVER);

            return returnVal;
        }

        public static ObjectIDGenerator IDGenerator { get; set; }

        /// <summary>
        /// This is copied almost verbatim from ProgramContext,
        /// It's here to help debug.
        /// </summary>
        public static string GetCodeFragment(List<Opcode> codes)
        {
            var codeFragment = new List<string>();
            
            const string FORMAT_STR = "{0,-20} {1,4}:{2,-3} {3:0000} {4} {5} {6} {7}";
            codeFragment.Add(string.Format(FORMAT_STR, "File", "Line", "Col", "IP  ", "Label  ", "opcode", "operand", "Destination" ));
            codeFragment.Add(string.Format(FORMAT_STR, "----", "----", "---", "----", "-------", "---------------------", "", "" ));

            for (int index = 0; index < codes.Count; index++)
            {
                codeFragment.Add(string.Format(FORMAT_STR,
                                               codes[index].SourcePath ?? GlobalPath.EMPTY,
                                               codes[index].SourceLine,
                                               codes[index].SourceColumn ,
                                               index,
                                               codes[index].Label ?? "null",
                                               codes[index] ?? new OpcodeBogus(),
                                               "DEST: " + (codes[index].DestinationLabel ?? "null" ),
                                               "" ) );
            }

            return codeFragment.Aggregate(string.Empty, (current, s) => current + (s + "\n"));
        }
    }
}
