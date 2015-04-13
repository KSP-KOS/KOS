using System;
using System.Collections.Generic;
using System.Linq;

namespace kOS.Safe.Compilation.KS
{
    public class UserFunctionCollection
    {
        private readonly Dictionary<string, UserFunction> userFuncs;
        private readonly List<UserFunction> newUserFuncs;

        public UserFunctionCollection()
        {
            userFuncs = new Dictionary<string, UserFunction>(StringComparer.OrdinalIgnoreCase);
            newUserFuncs = new List<UserFunction>();
        }

        public bool Contains(string userFuncIdentifier)
        {
            return userFuncs.ContainsKey(userFuncIdentifier);
        }

        public UserFunction GetUserFunction(string userFuncIdentifier)
        {
            if (userFuncs.ContainsKey(userFuncIdentifier))
            {
                return userFuncs[userFuncIdentifier];
            }
            var userFuncObject = new UserFunction(userFuncIdentifier);
            userFuncs.Add(userFuncIdentifier, userFuncObject);
            newUserFuncs.Add(userFuncObject);
            return userFuncObject;
        }

        public IEnumerable<UserFunction> GetUserFunctionList()
        {
            return userFuncs.Values.ToList();
        }

        public List<CodePart> GetParts(IEnumerable<UserFunction> userFuncList)
        {
            return userFuncList.Select(userFunctionObject => userFunctionObject.GetCodePart()).ToList();
        }

        public List<CodePart> GetParts()
        {
            return GetParts(userFuncs.Values.ToList());
        }

        public IEnumerable<CodePart> GetNewParts()
        {
            // new locks or functions
            List<CodePart> parts = GetParts(newUserFuncs);

            // updated locks or functions
            foreach (UserFunction userFuncObject in userFuncs.Values)
            {
                // if the lock or function is new then clear the new functions list
                if (newUserFuncs.Contains(userFuncObject))
                {
                    userFuncObject.ClearNewFunctions();
                }
                else if (userFuncObject.HasNewFunctions())
                {
                    // if the lock has new functions then create a new code part for them
                    parts.Add(userFuncObject.GetNewFunctionsCodePart());
                }
            }

            newUserFuncs.Clear();

            return parts;
        }
    }
}