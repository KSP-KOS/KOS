using System.Collections.Generic;

namespace kOS.Safe.Utilities
{
    public class MovingAverage
    {
        public List<double> Values { get; set; }

        public double Mean { get; private set; }

        public int ValueCount { get { return Values.Count; } }

        public int SampleLimit { get; set; }

        public MovingAverage()
        {
            Reset();
            SampleLimit = 30;
        }

        public void Reset()
        {
            Mean = 0;
            if (Values == null) Values = new List<double>();
            else Values.Clear();
        }

        public double Update(double value)
        {
            if (double.IsInfinity(value) || double.IsNaN(value)) return value;

            Values.Add(value);
            while (Values.Count > SampleLimit)
            {
                Values.RemoveAt(0);
            }
            //if (Values.Count > 5) Mean = Values.OrderBy(e => e).Skip(1).Take(Values.Count - 2).Average();
            //else Mean = Values.Average();
            //Mean = Values.Average();
            double sum = 0;
            double count = 0;
            double max = double.MinValue;
            double min = double.MaxValue;
            for (int i = 0; i < Values.Count; i++)
            {
                double val = Values[i];
                if (val > max)
                {
                    if (max != double.MinValue)
                    {
                        sum += max;
                        count++;
                    }
                    max = val;
                }
                else if (val < min)
                {
                    if (min != double.MaxValue)
                    {
                        sum += min;
                        count++;
                    }
                    min = val;
                }
                else
                {
                    sum += val;
                    count++;
                }
            }
            if (count == 0)
            {
                if (max != double.MinValue)
                {
                    sum += max;
                    count++;
                }
                if (min != double.MaxValue)
                {
                    sum += min;
                    count++;
                }
            }
            Mean = sum / count;
            return Mean;
        }
    }
}