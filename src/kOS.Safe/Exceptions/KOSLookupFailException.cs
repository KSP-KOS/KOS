using System;

namespace kOS.Safe.Exceptions
{
    /// <summary>
    /// Indicates that some sort of lookup, such as is done with
    /// part:GETMODULE("name"), or partModule:GETFIELD("name") didn't find
    /// any hits.
    /// </summary>
    public class KOSLookupFailException: KOSException
    {
        private const string TERSE_MSG_FMT = "No {0} called {1} was found on {2}";

        public override string VerboseMessage { get{ return BuildVerboseMessage(); } }

        public override string HelpURL { get{ return "";} }

        private string category;
        private string lookupName;
        private object obj;
        
        /// <summary>
        /// Make an exception describing improper suffix usage.
        /// </summary>
        /// <param name="category">category of thing being looked up, i.e "field" or "module"</param>
        /// <param name="lookupName">thing being looked up</param>
        /// <param name="obj">ref to the object on the left of the suffix colon.</param>
        public KOSLookupFailException(string category, string lookupName, object obj) :
            base(String.Format(TERSE_MSG_FMT, category, lookupName.ToUpper(), obj.ToString()))
        {
            this.category = category;
            this.lookupName = lookupName;
            this.obj = obj;            
        }
        
        private string BuildVerboseMessage()
        {
            const string VERBOSE_TEXT =
                "{0}\n" +
                "An attempt was made to retrive a {1} called:\n"+
                "    {2}\n" +
                "from an object of type:\n" +
                "    {3}\n" +
                "when that object does not have that\n" +
                "{1} on it.\n" +
                "\n" +
                "A full list of all {1}S on the object can be\n" +
                "found by using its :ALL{1}s suffix.\n";

            return String.Format(VERBOSE_TEXT, Message, category.ToUpper(), lookupName, obj.GetType().Name);

        }
    }
}
