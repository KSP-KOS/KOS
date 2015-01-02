using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Properties;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace kOS.Safe.Encapsulation
{
    public class ListValue<T> : Structure, IList<T>
    {
        private readonly IList<T> internalList;

        public ListValue()
            : this(new List<T>())
        {
        }

        public ListValue(IEnumerable<T> listValue)
        {
            internalList = listValue.ToList();
            ListInitializeSuffixes();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return internalList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(T item)
        {
            internalList.Add(item);
        }

        public void Clear()
        {
            internalList.Clear();
        }

        public bool Contains(T item)
        {
            return internalList.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            internalList.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            return internalList.Remove(item);
        }

        public int Count
        {
            get { return internalList.Count; }
        }

        public bool IsReadOnly
        {
            get { return internalList.IsReadOnly; }
        }

        public int IndexOf(T item)
        {
            return internalList.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            internalList.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            internalList.RemoveAt(index);
        }

        public T this[int index]
        {
            get { return internalList[index]; }
            set { internalList[index] = value; }
        }

        private void ListInitializeSuffixes()
        {
            AddSuffix("ADD", new OneArgsSuffix<T>(toAdd => internalList.Add(toAdd), Resources.ListAddDescription));
            AddSuffix("INSERT", new TwoArgsSuffix<int, T>((index, toAdd) => internalList.Insert(index, toAdd)));
            AddSuffix("REMOVE", new OneArgsSuffix<int>(toRemove => internalList.RemoveAt(toRemove)));
            AddSuffix("CLEAR", new NoArgsSuffix(() => internalList.Clear()));
            AddSuffix("LENGTH", new NoArgsSuffix<int>(() => internalList.Count));
            AddSuffix("ITERATOR", new NoArgsSuffix<Enumerator>(() => new Enumerator(internalList.GetEnumerator())));
            AddSuffix("COPY", new NoArgsSuffix<ListValue<T>>(() => new ListValue<T>(this)));
            AddSuffix("CONTAINS", new OneArgsSuffix<bool, T>(item => internalList.Contains(item)));
            AddSuffix("SUBLIST", new TwoArgsSuffix<ListValue, int, int>(SubListMethod));
            AddSuffix("EMPTY", new NoArgsSuffix<bool>(() => !internalList.Any()));
            AddSuffix("DUMP", new NoArgsSuffix<string>(ListDumpDeep));
        }

        // This test case was added to ensure there was an example method with more than 1 argument.
        private ListValue SubListMethod(int start, int runLength)
        {
            var subList = new ListValue();
            for (int i = start; i < internalList.Count && i < start + runLength; ++i)
            {
                subList.Add(internalList[i]);
            }
            return subList;
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

        // Using Statics for this is not thread-safe, but kOS doesn't do threads at the moment.
        // TODO: find a better way later to track the nesting level through all the messy
        // calls of nested objects' ToStrings.
        private static int currentNestDepth;

        private static int maxVerboseDepth;

        public override string ToString()
        {
            // If toString is nested inside another object's toString that was
            // called from another list, then honor the verbosity of that
            // original topmost call by not explicitly saying it's shallow or
            // nested here. otherwise explicitly say it's shallow if it's the outermost
            // ToString() call:
            return CalledFrom("ListDump") ? ListDump() : ListDumpShallow();
        }

        /// <summary>
        /// Returns whether or not the current method was called from the given method name
        /// by examining the callstack downward from the current level's parent.  Assumes the
        /// method in question is a method of this class (ListValue) itself.  Examines all
        /// the nesting levels, so if A called B called C called D, then during D, a call to
        /// calledFrom("A") would still return true.
        /// </summary>
        /// <param name="methodName">Test if this method called me</param>
        /// <returns>True if the current method was called from the given method name</returns>
        private bool CalledFrom(string methodName)
        {
            StackFrame[] callStack = new StackTrace().GetFrames();  // get call stack

            if (callStack == null) return false;
            var declaringType = callStack[0].GetMethod().DeclaringType;
            if (declaringType == null) return false;

            string thisDeclaringType = declaringType.Name;

            // Find out whether or not this method call was nested inside
            // another method call of itself which was not meant to recurse.
            // If so, then set a flag that will avoid doing the full dump:

            // (i starts at 1 not 0 deliberately.  That's not a bug - skipping to top stack
            // frame on purpose because the top stack frame is the current method we're in
            // the middle of executing):
            for (int i = 1; i < callStack.Length; ++i)
            {
                var type = callStack[i].GetMethod().DeclaringType;
                if (type == null) continue;

                var matchingName = callStack[i].GetMethod().Name == methodName;
                var matchingType = type.Name == thisDeclaringType;
                if (matchingName && matchingType)
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
        private string TerseDump()
        {
            return "LIST of " + internalList.Count + " item" + (internalList.Count == 1 ? "" : "s");
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
        /// Dump the contents of the list into a string, by descending through the
        /// list and appending the "ToString"'s of all the elements in the list.<br/>
        /// </summary>
        /// Warning: If you Ever change the name of this, then change the value of the two
        /// static variables "shallowDumpName" and "deepDumpName", or the ListDump algorithm
        /// will break:
        /// <returns>long string including eolns, ready for printing</returns>
        private string ListDumpDeep()
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
            const int SPACES_PER_INDENT = 2;

            ++currentNestDepth;
            bool truncateHere = currentNestDepth > maxVerboseDepth;

            if (truncateHere)
            {
                --currentNestDepth;
                return TerseDump();
            }
            var contents = new StringBuilder();
            contents.AppendLine(TerseDump() + ":");
            var indent = new string(' ', currentNestDepth * SPACES_PER_INDENT);
            for (int i = 0; i < internalList.Count; ++i)
            {
                contents.AppendLine(string.Format("{0}[{1,2}]= {2}", indent, i, internalList[i]));
            }
            --currentNestDepth;
            return contents.ToString();
        }
    }

    public class ListValue : ListValue<object>
    {
        public ListValue()
        {
            InitializeSuffixes();
        }

        public ListValue(IEnumerable<object> toCopy)
            : base(toCopy)
        {
            InitializeSuffixes();
        }

        private void InitializeSuffixes()
        {
            AddSuffix("COPY", new NoArgsSuffix<ListValue>(() => new ListValue(this)));
        }

        public static ListValue CreateList<T>(List<T> toCopy)
        {
            return new ListValue(toCopy.Cast<object>());
        }
    }
}