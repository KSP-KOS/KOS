using System;
using System.Collections.Generic;

namespace kOS
{
    public class ListValue : SpecialValue
    {
        private readonly IList<object> list;

        public ListValue()
        {
           list = new List<object>(); 
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
                    return list.Remove(value);
                default:
                    return false;
            }
        }

        public override object GetSuffix(string suffixName)
        {
            UnityEngine.Debug.Log("ListObject: " + suffixName);
            if (suffixName.StartsWith("FETCH"))
            {
                var selectedIndex = Convert.ToInt32(suffixName.Split(':')[1]);
                UnityEngine.Debug.Log("ListObject: FETCH:" + selectedIndex);
                return list[selectedIndex];
            }

            switch (suffixName)
            {
                case "CLEAR":
                    list.Clear();
                    return true;
                case "RESETINDEX":
                    return true;
                case "LENGTH":
                    return list.Count;
                case "ITERATOR":
                    return new Enumerator(list.GetEnumerator());
                case "COPY":
                    return new List<object>(list);
                default:
                    return string.Format("Suffix {0} Not Found", suffixName);
            }

        }

        public override string ToString()
        {
            return "LIST("+ list.Count +")";
        }
    }

    public class Enumerator : SpecialValue
    {
        private readonly IEnumerator<object> enumerator;
        private int index;
        private readonly object lockObject = new object();

        public Enumerator(IEnumerator<object> enumerator)
        {
            this.enumerator = enumerator;
        }

        public override object GetSuffix(string suffixName)
        {
            lock (lockObject)
            {
                UnityEngine.Debug.Log("ListObject: SUFFIX:" + suffixName);
                switch (suffixName)
                {
                    case "RESET":
                        index = 0;
                        enumerator.Reset();
                        UnityEngine.Debug.Log("ListObject: RESET");
                        return true;
                    case "END":
                        UnityEngine.Debug.Log("ListObject: Advance:" + enumerator.Current);
                        var status = enumerator.MoveNext();
                        index++;
                        UnityEngine.Debug.Log("ListObject: Advance:" + enumerator.Current);
                        return !status;
                    case "INDEX":
                        UnityEngine.Debug.Log("ListObject: Index:" + index);
                        return index;
                    case "VALUE":
                        UnityEngine.Debug.Log("ListObject: Current:" + enumerator.Current);
                        return enumerator.Current;
                    default:
                        return string.Format("Suffix {0} Not Found", suffixName);

                }
            }
        }
    }
}
