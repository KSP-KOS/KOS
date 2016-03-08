using System;
using kOS.Safe.Utilities;

namespace kOS.Safe.Exceptions
{
    /// <summary>
    /// Indicates that KOS attempted to convert between two types and
    /// the system refused to allow the conversion.
    /// </summary>
    public class KOSCastException: KOSException
    {
        private const string TERSE_MSG_FMT = "Cannot use {0} where {1} is needed";

        public override string VerboseMessage { get{ return BuildVerboseMessage(); } }

        public override string HelpURL { get{ return "";} }

        private readonly string typeFrom;
        private readonly string typeTo;
        
        /// <summary>
        /// Make an exception when an attempt to convert from one type to another failed.
        /// </summary>
        public KOSCastException(Type typeFrom, Type typeTo)
            : this(KOSNomenclature.GetKOSName(typeFrom), KOSNomenclature.GetKOSName(typeTo))
        {

        }

        public KOSCastException(string typeFrom, string typeTo) :
            base(String.Format(TERSE_MSG_FMT, typeFrom, typeTo))
        {
            this.typeFrom = typeFrom;
            this.typeTo = typeTo;
        }
        
        private string BuildVerboseMessage()
        {
            const string VERBOSE_TEXT = "kOS attempts to do as much type conversion as{0}" +
                               "possible behind the scenes so you don't have to{0}" +
                               "worry about messy issues like the difference{0}" +
                               "between integers, 32-bit numbers, 64-bit numbers,{0}" +
                               "and so on.  But there are some conversions that{0}" +
                               "it does not automatically do and apparently you{0}" +
                               "have encountered one of them.{0}" +
                               "{0}" +
                               "When you get this message it means some part of{0}" +
                               "your program script has tried to use a value of{0}" +
                               "the wrong type for what it was being used for.{0}" +
                               "(For example, trying to take the cosine of the{0}" +
                               "string \"hello\".){0}" +
                               "{0}" +
                               "In this specific instance, the script was trying{0}" +
                               "to use some type of:{0}" +
                               "    {1}{0}" +
                               "in a place where it needed to use some type of:{0}" +
                               "    {2}{0}";

            return String.Format(VERBOSE_TEXT, Environment.NewLine, typeFrom, typeTo);
        }
    }
}
