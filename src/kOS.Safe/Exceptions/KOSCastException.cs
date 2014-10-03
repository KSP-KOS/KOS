using System;

namespace kOS.Safe.Exceptions
{
    /// <summary>
    /// Indicates that KOS attempted to convert between two types and
    /// the system refused to allow the conversion.
    /// </summary>
    public class KOSCastException: Exception, IKOSException
    {
        private static string terseMsgFmt = "Cannot use {0} where {1} is needed";
        
        public string VerboseMessage { get{ return BuildVerboseMessage(); } set{} }

        public string HelpURL { get{ return "";} set{} }
        
        private Type typeFrom;
        private Type typeTo;
        
        /// <summary>
        /// Make an exception when an attempt to convert from one type to another failed.
        /// </summary>
        /// <param name="verb">present-tense singular conjugation of the operation's verb, i.e "negate"</param>
        /// <param name="operand">operand object being worked on</param>
        public KOSCastException(Type typeFrom, Type typeTo) :
            base(String.Format(terseMsgFmt, typeFrom.Name, typeTo.Name))
        {
            this.typeFrom = typeFrom;
            this.typeTo = typeTo;
        }
        
        private string BuildVerboseMessage()
        {
            string fmt =
                "kOS attempts to do as much type conversion as\n" +
                "possible behind the scenes so you don't have to\n" +
                "worry about messy issues like the difference\n" +
                "between integers, 32-bit numbers, 64-bit numbers,\n" +
                "and so on.  But there are some conversions that\n" +
                "it does not automatically do and apparently you\n" +
                "have encountered one of them.\n" +
                "\n" +
                "When you get this message it means some part of\n" +
                "your program script has tried to use a value of\n" +
                "the wrong type for what it was being used for.\n" +
                "(For example, trying to take the cosine of the\n" +
                "string \"hello\".)\n" +
                "\n" +
                "In this specific instance, the script was trying\n" +
                "to use some type of:\n" +
                "    {0}\n" +
                "in a place where it needed to use some type of:\n" +
                "    {1}\n";
            
            return String.Format(fmt, typeFrom.Name, typeTo.Name);
        }
    }
}
