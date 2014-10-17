using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Properties;

namespace kOS.Safe.Encapsulation
{
    public class ListValue : Structure, IIndexable
    {
        private readonly IList<object> list;
                
        public ListValue()
        {
            list = new List<object>();
            ListInitializeSuffixes();
        }

        private ListValue(ListValue toCopy)
        {
            list = new List<object>(toCopy.list);
            ListInitializeSuffixes();
        }

        private void ListInitializeSuffixes()
        {
            AddSuffix("ADD",      new OneArgsSuffix<object>         (toAdd => list.Add(toAdd), Resources.ListAddDescription));
            AddSuffix("REMOVE",   new OneArgsSuffix<int>            (toRemove => list.RemoveAt(toRemove)));
            AddSuffix("CLEAR",    new NoArgsSuffix                  (() => list.Clear()));
            AddSuffix("LENGTH",   new NoArgsSuffix<int>                 (() => list.Count));
            AddSuffix("ITERATOR", new NoArgsSuffix<Enumerator>          (() => new Enumerator(list.GetEnumerator())));
            AddSuffix("COPY",     new NoArgsSuffix<ListValue>           (() => new ListValue(this)));
            AddSuffix("CONTAINS", new OneArgsSuffix<bool, object>       (item => list.Contains(item)));
            AddSuffix("SUBLIST",  new TwoArgsSuffix<ListValue, int, int>(SubListMethod));
            AddSuffix("EMPTY",    new NoArgsSuffix<bool>                (() => !list.Any()));
            AddSuffix("DUMP",     new NoArgsSuffix<string>              (() => ListDumpDeep()));
        }

        public override bool SetSuffix(string suffixName, object value)
        {
            //These were deprecated in v0.15. Text here it to assist in upgrading scripts
            switch (suffixName)
            {
                case "ADD":
                    throw new Exception("Old syntax \n" +
                                               "   SET _somelist_:ADD TO _value_\n" +
                                               "is no longer supported. Try replacing it with: \n" +
                                               "   _somelist_:ADD(_value_).\n");
                case "CONTAINS":
                    throw new Exception("Old syntax \n" +
                                               "   SET _somelist_:CONTAINS TO _value_\n" +
                                               "is no longer supported. Try replacing it with: \n" +
                                               "   SET _somelist_:CONTAINS(_value_) TO _value_\n");
                case "REMOVE":
                    throw new Exception("Old syntax \n" +
                                               "   SET _somelist_:REMOVE TO _number_\n" +
                                               "is no longer supported. Try replacing it with: \n" +
                                               "   _somelist_:REMOVE(_number_).\n");
                default:
                    return false;
            }
        }

        // This test case was added to ensure there was an example method with more than 1 argument.
        public ListValue SubListMethod(int start, int runLength)
        {
            var subList = new ListValue();
            for (int i = start; i < list.Count && i < start + runLength; ++i)
            {
                subList.Add(list[i]);
            }
            return subList;
        }

        public void Add(object toAdd)
        {
            list.Add(toAdd);
        }

        // Using Statics for this is not thread-safe, but kOS doesn't do threads at the moment.
        // TODO: find a better way later to track the nesting level through all the messy
        // calls of nested objects' ToStrings.
        private static int currentNestDepth = 0;
        private static int maxVerboseDepth = 0;
        
        public override string ToString()
        {
            // If toString is nested inside another object's toString that was
            // called from another list, then honor the verbosity of that
            // original topmost call by not explicitly saying it's shallow or
            // nested here. otherwise explictly say it's shallow if it's the outermost
            // ToString() call:
            if (calledFrom("ListDump"))
                return ListDump();
            else
                return ListDumpShallow();
        }
        
        /// <summary>
        /// Returns whether or not the current method was called from the given method name
        /// by examning the callstack downward from the current level's parent.  Assumes the
        /// method in question is a method of this class (ListValue) itself.  Examines all
        /// the nesting levels, so if A called B called C called D, then during D, a call to
        /// calledFrom("A") would still return true.
        /// </summary>
        /// <param name="methodName">Test if this method called me</param>
        /// <returns>True if the current method was called from the given method name</returns>
        private bool calledFrom(string methodName)
        {
            StackFrame[] callStack = new StackTrace().GetFrames();  // get call stack

            string thisDeclaringType = callStack[0].GetMethod().DeclaringType.Name;

            // Find out whether or not this method call was nested inside
            // another method call of itself which was not meant to recurse.
            // If so, then set a flag that will avoid doing the full dump:

            // (i starts at 1 not 0 deliberately.  That's not a bug - skipping to top stack
            // frame on purpose because the top stack frame is the current method we're in
            // the middle of executing):
            for (int i = 1 ; i < callStack.Length ; ++i )
            {
                if (callStack[i].GetMethod().Name == methodName &&
                    callStack[i].GetMethod().DeclaringType.Name == thisDeclaringType )
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Show the contents of the string as tersely as possible, as just
        /// a "this is a list and it has this many things int it" message.
        /// </summary>
        /// <returns>short string without eoln</returns>
        private string terseDump()
        {
            return "LIST of " + list.Count + " item" + (list.Count>1 ? "s" : "");
        }

        /// <summary>
        /// Dump the contents of the list in a shallow way, without recursing down
        /// into any sublists inside the topmost list:<br/>
        /// </summary>
        /// Warning: If you Ever change the name of this, then change the value of the two
        /// static variables "shallowDumpName" and "deepDumpName", or the ListDump algorithm
        /// will break:
        /// <returns>long string including eolns, ready for printing</returns>
        private string ListDumpShallow()
        {
            maxVerboseDepth = 1;
            return ListDump();
        }
        
        /// <summary>
        /// Dump the contens of the list into a string, by descending through the
        /// list and appending the "ToString"'s of all the elements in the list.<br/>
        /// </summary>
        /// Warning: If you Ever change the name of this, then change the value of the two
        /// static variables "shallowDumpName" and "deepDumpName", or the ListDump algorithm
        /// will break:
        /// <returns>long string including eolns, ready for printing</returns>
        public string ListDumpDeep()
        {
            maxVerboseDepth = 99;
            return ListDump();
        }
        
        /// <summary>
        /// This is the engine underneath ListDump shallow/deep.
        /// </summary>
        /// <returns>string dump of the list</returns>
        private string ListDump()
        {
            ++currentNestDepth;
            bool truncateHere = currentNestDepth > maxVerboseDepth;

            if (truncateHere)
            {
                --currentNestDepth;                
                return terseDump();
            }
            else
            {
                StringBuilder contents = new StringBuilder();
                contents.AppendLine( terseDump() + ":" );
                for (int i = 0 ; i < list.Count ; ++i)
                {
                    contents.AppendLine( string.Format("{0}[{1,2}]= {2}", new String(' ',currentNestDepth*2), i, list[i].ToString()) );
                }
                --currentNestDepth;                
                return contents.ToString();
            }
        }

        #region IIndexable Members

        public object GetIndex(int index)
        {
            return list[index];
        }

        public void SetIndex(int index, object value)
        {
            list[index] = value;
        }

        #endregion IIndexable Members
    }

    public class Enumerator : Structure
    {
        private readonly IEnumerator enumerator;
        private int index = -1;
        private bool status;

        public Enumerator(IEnumerator enumerator)
        {
            this.enumerator = enumerator;
            EnumeratorInitializeSuffixes();
        }

        private void EnumeratorInitializeSuffixes()
        {
            AddSuffix("RESET",    new NoArgsSuffix    (() =>
                {
                    index = -1;
                    status = false;
                    enumerator.Reset();
                }));
            AddSuffix("NEXT",     new NoArgsSuffix<bool>  (() =>
                {
                    status = enumerator.MoveNext();
                    index++;
                    return status;
                }));
            AddSuffix("ATEND",    new NoArgsSuffix<bool>  (() => !status));
            AddSuffix("INDEX",    new NoArgsSuffix<int>   (() => index));
            AddSuffix("VALUE",    new NoArgsSuffix<object>(() => enumerator.Current));
            AddSuffix("ITERATOR", new NoArgsSuffix<object>(() => this));
        }

        public override string ToString()
        {
            return string.Format("{0} Enumerator", base.ToString());
        }
    }
}