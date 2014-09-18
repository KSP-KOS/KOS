using System.Collections;
using System.Collections.Generic;
using System.Linq;
using kOS.Safe.Encapsulation;

namespace kOS.Suffixed
{
    public class ListValue : Structure, IIndexable
    {
        private readonly IList<object> list;

        public ListValue()
        {
            list = new List<object>();
        }

        private ListValue(ListValue toCopy)
        {
            list = new List<object>(toCopy.list);
        }

        public int Count
        {
            get { return list.Count; }
        }

        public override bool SetSuffix(string suffixName, object value)
        {
            switch (suffixName)
            {
                case "ADD":
                    list.Add(value);
                    return true;
                case "CONTAINS":
                    return list.Contains(value);
                case "REMOVE":
                    var index = int.Parse(value.ToString());
                    list.RemoveAt(index);
                    return true;
                default:
                    return false;
            }
        }

        public override object GetSuffix(string suffixName)
        {
            switch (suffixName)
            {
                case "CLEAR":
                    list.Clear();
                    return true;
                case "LENGTH":
                    return list.Count;
                case "ITERATOR":
                    return new Enumerator(list.GetEnumerator());
                case "COPY":
                    return new ListValue(this);
                default:
                    return string.Format("Suffix {0} Not Found", suffixName);
            }
        }

        public void Add(object toAdd)
        {
            list.Add(toAdd);
        }

        public override string ToString()
        {
            return string.Format("{0} LIST({1})", base.ToString(), list.Count);
        }

        public bool Empty()
        {
            return !list.Any();
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

        #endregion
    }

    public class Enumerator : Structure
    {
        private readonly IEnumerator enumerator;
        private readonly object lockObject = new object();
        private int index = -1;
        private bool status;

        public Enumerator(IEnumerator enumerator)
        {
            this.enumerator = enumerator;
        }

        public override object GetSuffix(string suffixName)
        {
            lock (lockObject)
            {
                switch (suffixName)
                {
                    case "RESET":
                        index = -1;
                        status = false;
                        enumerator.Reset();
                        return true;
                    case "NEXT":
                        status = enumerator.MoveNext();
                        index++;
                        return status;
                    case "ATEND":
                        return !status;
                    case "INDEX":
                        return index;
                    case "VALUE":
                        return enumerator.Current;
                    case "ITERATOR":
                        return this;
                    default:
                        return string.Format("Suffix {0} Not Found", suffixName);
                }
            }
        }

        public override string ToString()
        {
            return string.Format("{0} Enumerator", base.ToString());
        }
    }
}
