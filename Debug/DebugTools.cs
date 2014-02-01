using System;
using System.Text;

namespace kOS.Debug
{
    public static class DebugTools
    {
        public static string DisplayObjectInfo(Object o)
        {
            var sb = new StringBuilder();

            // Include the type of the object
            var type = o.GetType();
            sb.Append("Type: " + type.Name);

            // Include information for each Field
            sb.Append("\r\n\r\nFields:");
            var fi = type.GetFields();
            if (fi.Length > 0)
            {
                foreach (var f in fi)
                {
                    sb.Append("\r\n " + f + " = " +
                              f.GetValue(o));
                }
            }
            else
                sb.Append("\r\n None");

            // Include information for each Property
            sb.Append("\r\n\r\nProperties:");
            var pi = type.GetProperties();
            if (pi.Length > 0)
            {
                foreach (var p in pi)
                {
                    sb.Append("\r\n " + p + " = " +
                              p.GetValue(o, null));
                }
            }
            else
                sb.Append("\r\n None");

            return sb.ToString();
        }
    }
}