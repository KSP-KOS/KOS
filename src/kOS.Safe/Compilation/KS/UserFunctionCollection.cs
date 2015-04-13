using System;
using System.Collections.Generic;
using System.Linq;

namespace kOS.Safe.Compilation.KS
{
    public class UserFunctionCollection
    {
        /// <summary>
        /// A list of all the UserFuncs the compiler knows about, from either
        /// this compile or a previous compile:
        /// </summary>
        private readonly Dictionary<string, UserFunction> userFuncs;
        
        /// <summary>
        /// A subset (a true venn-diagram subset) of userFuncs that only lists
        /// the functions that have been added recently since the last call
        /// to GetNewParts.  Used to differentiate the user functions that
        /// were added during the current compile from the ones that 
        /// existed in previous compiles (from other scripts, basically).
        /// </summary>
        private readonly List<UserFunction> newUserFuncs;

        public UserFunctionCollection()
        {
            userFuncs = new Dictionary<string, UserFunction>(StringComparer.OrdinalIgnoreCase);
            newUserFuncs = new List<UserFunction>();
        }

        public bool Contains(string userFuncIdentifier, Int16 scopeId)
        {
            // uses backticks to differentiate different scope Id's using the same lock identifier name:
            string ident = String.Format("{0}`{1}", userFuncIdentifier, scopeId);
            return Contains(ident);
        }
        
        private bool Contains(string userFuncIdentifier)
        {
            return userFuncs.ContainsKey(userFuncIdentifier);
        }
        
        /// <summary>
        /// Get or create the user function give its identifying criteria
        /// </summary>
        /// <param name="userFuncIdentifier">the script id the function or lock has</param>
        /// <param name="scopeId">the integer id of the containing scope it is declared in</param>
        /// <param name="declaredWith">the parse tree branch of the declaration.  This is only
        /// important when it has to construct a new user function object.  It will not be used
        /// to narrow down the search for an existing one.</param>
        /// <returns></returns>
        public UserFunction GetUserFunction(string userFuncIdentifier, Int16 scopeId, ParseNode declaredWith)
        {
            // uses backticks to differentiate different scope Id's using the same lock identifier name:
            string ident = String.Format("{0}`{1}", userFuncIdentifier, scopeId);
            return GetUserFunction(ident, declaredWith);
        }

        private UserFunction GetUserFunction(string userFuncIdentifier, ParseNode declaredWith)
        {
            if (userFuncs.ContainsKey(userFuncIdentifier))
            {
                return userFuncs[userFuncIdentifier];
            }
            if (declaredWith != null)
            {
                var userFuncObject = new UserFunction(userFuncIdentifier, declaredWith);
                userFuncs.Add(userFuncIdentifier, userFuncObject);
                newUserFuncs.Add(userFuncObject);
                return userFuncObject;
            }
            return null; // shouldn't happen - must call with declaredWith = something.
        }

        /// <summary>
        /// Returns true if the given user function is one of the ones that was just 
        /// declared during the current compile and not one that was leftover from
        /// a previous compile.
        /// <br/>
        /// Returns false if either the function exists and is old, or the function
        /// doesn't even exist in the collection at all.
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>
        public bool IsNew(UserFunction func)
        {
            // Possible opportuinity for an optimization refactor here:
            // -------------------------------------------
            // newUserFuncs is purely a flat sequential list with
            // no sort order or hash mapping.  Therefore seeing if a
            // function is new or not requires a sequential walk of all
            // the functions built during the current compile.
            // If people start writing library scripts containing upwards
            // of 20 or more functions in one file, that could become
            // inefficient.  It may be possible to refactor newUserFuncs
            // into some sort of hashmap or at least sorted list. That way
            // Contains() would be a faster search.
            
            return newUserFuncs.Contains(func);
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