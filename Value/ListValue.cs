using System.Collections.Generic;

namespace kOS.Value
{
    public class ListValue : SpecialValue
    {
        private readonly IList<object> list;

        public ListValue()
        {
           list = new List<object>(); 
        }

        public override object GetIndex(int index)
        {
            return list[index];
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
                switch (suffixName)
                {
                    case "RESET":
                        index = 0;
                        enumerator.Reset();
                        return true;
                    case "END":
                        var status = enumerator.MoveNext();
                        index++;
                        return !status;
                    case "INDEX":
                        return index;
                    case "VALUE":
                        return enumerator.Current;
                    default:
                        return string.Format("Suffix {0} Not Found", suffixName);
                }
            }
        }
    }
}
