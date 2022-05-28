using System.Collections.Generic;
using System.Text;
using System.Linq;
using System;

namespace kOS.Safe.Compilation.KS
{
    public class UserFunction
    {
        private static readonly List<string> systemLocks = new List<string> { "throttle", "steering", "wheelthrottle", "wheelsteering" };
        
        private readonly CodePart codePart;
        private readonly Dictionary<int, UserFunctionCodeFragment> functions;
        private readonly List<UserFunctionCodeFragment> newFunctions;

        public string Identifier { get; private set; }
        public string PointerIdentifier{ get; private set; }
        public string DefaultLabel{ get; private set; }

        /// <summary>The node from the parse tree in which this function or lock was defined. </summary>
        public ParseNode OriginalNode{ get; private set; }
        
        /// <summary>
        /// To help store the difference between the same function name at different scope
        /// levels, the scope number is appened to the end of the identifier name when storing
        /// things.  This sanitizes that by removing the number again to get the actual variable
        /// name as used by the script code:
        /// </summary>
        public string ScopelessDefaultLabel
        {
            get
            {
                int backTickIndex = DefaultLabel.IndexOf('`');
                return backTickIndex < 0 ? DefaultLabel : (DefaultLabel.Remove(backTickIndex) + "-default"); // reattach the "-default" on the end.
            }
        }
        /// <summary>
        /// To help store the difference between the same function name at different scope
        /// levels, the scope number is appened to the end of the identifier name when storing
        /// things.  This sanitizes that by removing the number again to get the actual variable
        /// name as used by the script code:
        /// </summary>
        public string ScopelessPointerIdentifier
        {
            get
            {
                int backTickIndex = PointerIdentifier.IndexOf('`');
                return backTickIndex < 0 ? PointerIdentifier : (PointerIdentifier.Remove(backTickIndex) + "*"); // reattach the '*' on the end.
            }
        }
        /// <summary>
        /// To help store the difference between the same function name at different scope
        /// levels, the scope number is appened to the end of the identifier name when storing
        /// things.  This sanitizes that by removing the number again to get the actual variable
        /// name as used by the script code:
        /// </summary>
        public string ScopelessIdentifier
        {
            get
            {
                int backTickIndex = Identifier.IndexOf('`');
                return backTickIndex < 0 ? Identifier : Identifier.Remove(backTickIndex);
            }
        }

        /// <summary>
        /// A the thing this was defined in.  (it will be part of the parse tree of the compiler).
        /// null = global
        /// </summary>
        public ParseNode ScopeNode {get; set;}
        
        public bool IsFunction {get; set; }

        public List<Opcode> InitializationCode
        {
            get { return codePart.InitializationCode; }
        }

        public List<Opcode> MainCode
        {
            get { return codePart.MainCode; }
        }

        public UserFunction(ParseNode originalNode)
        {
            codePart = new CodePart();
            functions = new Dictionary<int, UserFunctionCodeFragment>();
            newFunctions = new List<UserFunctionCodeFragment>();
            OriginalNode = originalNode;
        }

        public UserFunction(string userFuncIdentifier, ParseNode originalNode)
            : this(originalNode)
        {
            Identifier = userFuncIdentifier;
            PointerIdentifier = "$" + Identifier + "*";
            DefaultLabel = Identifier + "-default";
        }

        public bool IsInitialized()
        {
            return (IsFunction || codePart.InitializationCode.Count > 0);
        }
        
        /// <summary>
        /// Get the label of the function body entry point,
        /// in other words the label of the very first Opcode
        /// instruction at the start of the function body.
        /// </summary>
        /// <returns>string label, i.e. K00001 </returns>
        public string GetFuncLabel()
        {
            if (functions.Count <= 0)
                return "undefined";
            else if (! IsFunction)
                return "not-a-function";
            else
                return functions.Values.FirstOrDefault().Code[0].Label;
        }
        
        public List<Opcode> GetUserFunctionOpcodes(int expressionHash)
        {
            if (functions.ContainsKey(expressionHash))
            {
                return functions[expressionHash].Code;
            }
            var newUserFuncFragment = new UserFunctionCodeFragment();
            functions.Add(expressionHash, newUserFuncFragment);
            newFunctions.Add(newUserFuncFragment);
            return newUserFuncFragment.Code;
        }
        
        public string GetUserFunctionLabel(int expressionHash)
        {
            return String.Format("{0}-{1}", Identifier, expressionHash.ToString("x"));
        }
        
        public CodePart GetCodePart()
        {
            var mergedPart = new CodePart
            {
                InitializationCode = codePart.InitializationCode,
                MainCode = codePart.MainCode
            };

            foreach (UserFunctionCodeFragment function in functions.Values)
            {
                mergedPart.FunctionsCode.AddRange(function.Code);
            }

            return mergedPart;
        }

        public bool HasNewFunctions()
        {
            return (newFunctions.Count > 0);
        }

        public void ClearNewFunctions()
        {
            newFunctions.Clear();
        }

        public CodePart GetNewFunctionsCodePart()
        {
            var newFunctionsPart = new CodePart();

            foreach (UserFunctionCodeFragment function in newFunctions)
            {
                newFunctionsPart.FunctionsCode.AddRange(function.Code);
            }

            ClearNewFunctions();
            return newFunctionsPart;
        }

        public bool IsSystemLock()
        {
            return systemLocks.Contains(ScopelessIdentifier.ToLower());
        }

        static public bool IsSystemLock(string name)
        {
            return systemLocks.Contains(name.ToLower());
        }
    }
}