using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        }

        private ListValue(ListValue toCopy)
        {
            list = new List<object>(toCopy.list);
        }

        protected override void InitializeSuffixes()
        {
            AddSuffix("ADD",      new OneArgsVoidSuffix<object>         (toAdd => list.Add(toAdd), Resources.ListAddDescription));
            AddSuffix("REMOVE",   new OneArgsVoidSuffix<int>            (toRemove => list.RemoveAt(toRemove)));
            AddSuffix("CLEAR",    new NoArgsVoidSuffix                  (() => list.Clear()));
            AddSuffix("LENGTH",   new NoArgsSuffix<int>                 (() => list.Count));
            AddSuffix("ITERATOR", new NoArgsSuffix<Enumerator>          (() => new Enumerator(list.GetEnumerator())));
            AddSuffix("COPY",     new NoArgsSuffix<ListValue>           (() => new ListValue(this)));
            AddSuffix("CONTAINS", new OneArgsSuffix<bool, object>       (item => list.Contains(item)));
            AddSuffix("SUBLIST",  new TwoArgsSuffix<ListValue, int, int>(SubListMethod));
            AddSuffix("EMPTY",    new NoArgsSuffix<bool>                (() => !list.Any()));
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

        public override string ToString()
        {
            return string.Format("{0} LIST({1})", base.ToString(), list.Count);
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
        }

        protected override void InitializeSuffixes()
        {
            AddSuffix("RESET",    new NoArgsVoidSuffix    (() =>
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