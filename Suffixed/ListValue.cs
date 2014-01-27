using System.Collections.Generic;

namespace kOS.Suffixed
{
    public class MixedListValue : ListValue<object>
    {
        
    }

    public class ListValue<T> : SpecialValue
    {
        private readonly IList<T> list;

        public ListValue()
        {
           list = new List<T>(); 
        }

        public T GetIndex(int index)
        {
            return list[index];
        }

        public bool SetSuffix(string suffixName, T value)
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
                    return new Enumerator<T>(list.GetEnumerator());
                case "COPY":
                    return new List<T>(list);
                default:
                    return string.Format("Suffix {0} Not Found", suffixName);
            }
        }

        public void Add(T toAdd)
        {
            list.Add(toAdd);
        }

        public override string ToString()
        {
            return "LIST("+ list.Count +")";
        }
    }

    public class Enumerator<T> : SpecialValue
    {
        private readonly IEnumerator<T> enumerator;
        private int index;
        private readonly object lockObject = new object();

        public Enumerator(IEnumerator<T> enumerator)
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
