using System;

namespace kOS.Safe.Compilation.KS
{
    /// <summary>
    /// Nothing more than a simple set of data for remembering what scope level a part of the parse
    /// tree is at, so that data can be used in a later pass of the compiler.
    /// </summary>
    public class Scope
    {
        public Int16 ScopeId {get;set;}
        public Int16 ParentScopeId {get;set;}
        public Int16 NestDepth {get;set;}
        public Scope( Int16 scopeId, Int16 parentScopeId, Int16 nestDepth)
        {
            ScopeId = scopeId;
            ParentScopeId = parentScopeId;
            NestDepth = nestDepth;  
        }
    }
}
