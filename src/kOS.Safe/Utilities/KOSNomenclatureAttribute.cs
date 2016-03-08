using System;

namespace kOS.Safe.Utilities
{
    /// <summary>
    /// Holds the KOS naming information for a Structure.  Used in place of
    /// a static name field because you cannot override static fields in C#.
    /// <br/>
    /// You are allowed to attach more than one instance of this attribute
    /// to a class to declare multiple alias names for it, but only if you
    /// set the one way properties such that there isn't a name clash.  When
    /// going from a CSHarp name to a KOS name there should be exactly 1 such
    /// mapping.  When going from a kOS name to a CSharp name there can be
    /// more than one such mapping.
    /// </summary>
    [
        System.AttributeUsage((System.AttributeTargets.Class |
                               System.AttributeTargets.Struct),
                              Inherited = false,
                              AllowMultiple = true)
                              
    ]
    public class KOSNomenclatureAttribute : Attribute
    {
        public string KOSName { get; set;}
        
        /// <summary>
        /// Set this to false to cause this to be a one-way mapping only, for aliasing purposes
        /// </summary>
        public bool CSharpToKOS { get; set;}

        /// <summary>
        /// Set this to false to cause this to be a one-way mapping only, for aliasing purposes
        /// </summary>
        public bool KOSToCSharp { get; set;}

        public KOSNomenclatureAttribute(string kOSName)
        {
            this.KOSName = kOSName;
            CSharpToKOS = true;
            KOSToCSharp = true;
        }
    }
}