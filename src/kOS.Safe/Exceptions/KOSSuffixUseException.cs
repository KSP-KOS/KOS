using System;

namespace kOS.Safe.Exceptions
{
    /// <summary>
    /// Indicates that a script tried referring to a suffix for an object when
    /// the object does not have that suffix, or that the suffix does exist,
    /// but not for being used the way it's used (i.e. trying to SET using a GET-only
    /// suffix.)
    /// </summary>
    public class KOSSuffixUseException: KOSException
    {
        private const string TERSE_MSG_FMT = "{0} Suffix '{1}' not found on object {2}";

        public override string VerboseMessage { get{ return BuildVerboseMessage(); } }

        public override string HelpURL { get{ return "";} }

        private string operation;
        private string suffixName;
        private object obj;
        
        /// <summary>
        /// Make an exception describing improper suffix usage.
        /// </summary>
        /// <param name="operation">"get" or "set"</param>
        /// <param name="sufixName">name of suffix the operation was attempted on</param>
        /// <param name="obj">ref to the object on the left of the suffix colon.</param>
        public KOSSuffixUseException(string operation, string suffixName, object obj) :
            base(String.Format(TERSE_MSG_FMT, operation.ToUpper(), suffixName.ToUpper(), obj.ToString()))
        {
            this.operation = operation;
            this.suffixName = suffixName;
            this.obj = obj;
            
            // FUTURE FEATURE TODO:
            //   Depending on the object type being passed in, change the HelpURL to
            //   the document page where the suffixes for THAT particular object type
            //   are listed.  This would have to just be a long list of hardcoded
            //   cases in a switch or if/else ladder, most likely.
        }
        
        private string BuildVerboseMessage()
        {
            const string VERBOSE_TEXT =
                "{0}\n" +
                "An attempt was made to {1} a suffix called:\n"+
                "    {2}\n" +
                "from an object of type:\n" +
                "    {3}\n" +
                "when that object does not have that\n" +
                "suffix usable in that way.\n" +
                "\n" +
                "Possible causes are:\n" +
                "  - No {3} ever has a {2} suffix.\n" +
                "  - There is such a suffix, but it can't be used\n" +
                "      with a {1} operation.\n" +
                "  - There is such a suffix on some instances\n" +
                "      of {3}, but not this particular one.\n";

            return String.Format(VERBOSE_TEXT, Message, operation, suffixName, obj.GetType().Name);

            // TODO: In the above line, replace obj.GetType().Name with Utilities.Utils.KOSType(obj).
            // The reason this wasn't done yet is that Utilities.Utils is not moved into kOS.Safe and can't
            // be seen from here.  The project of splitting Utilities.Utils into the parts that are KSP
            // dependant and the parts that aren't is a large enough project to wait for another time.  It's
            // out of scope for the current project I'm working on and it causes a lot of cascading changes
            // to other parts of the code that aren't part of the suffix system.
        }
    }
}
