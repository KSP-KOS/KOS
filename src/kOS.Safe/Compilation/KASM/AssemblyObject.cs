using kOS.Safe.Encapsulation;
using kOS.Safe.Execution;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace kOS.Safe.Compilation.KASM
{
    public class AssemblyObject : CompiledObject
    {
        public static string Disassemble(List<CodePart> parts)
        {
            StringBuilder b = new StringBuilder();

            b.AppendLine(";KASM");
            b.AppendLine();

            foreach(var part in parts)
            {
                WriteCodePart(part, b);
            }

            return b.ToString().Replace("\r\n","\n");
        }

        public static IEnumerable<CodePart> Assemble(string assembly, string prefix = null)
        {
            assembly = assembly.Replace("\r\n", "\n"); //Normalize line endings
            assembly = assembly.Trim(); //To compensate for parsing issue with whitespace after last instruction

            var parser = new Parser(new Scanner());
            var tree = parser.Parse(assembly);
            if (tree.Errors.Count != 0)
                throw new FormatException(tree.Errors.First().ToString());

            // Codeparts
            //  |-> FunctionSection, InitSection?, MainSection?
            //      |-> Operation
            //          |-> label?, opcode, arg1, arg2, arg...
            var distilled = (List<List<List<List<object>>>>)tree.Eval();
            
            string prevLabel = "#####";

            foreach (var codepartlist in distilled)
            {
                var part = new CodePart();
                
                part.FunctionsCode = ParseOpList(codepartlist[0], prefix, ref prevLabel);
                part.InitializationCode = ParseOpList(codepartlist[1], prefix, ref prevLabel);
                part.MainCode = ParseOpList(codepartlist[2], prefix, ref prevLabel);
                yield return part;
            }
        }

        private static List<Opcode> ParseOpList(List<List<object>> oplist, string prefix, ref string prevLabel)
        {
            if (oplist == null)
                return new List<Opcode>();

            var result = new List<Opcode>();
            foreach(var operation in oplist)
            {
                string label = (string)operation[0];

                var opType = Opcode.TypeFromName((string)operation[1]);
                var op = (Opcode)(Activator.CreateInstance(opType, true));

                var opArgs = new List<object>();
                int i = 2;

                foreach (MLArgInfo argInfo in op.GetArgumentDefs())
                {
                    if (operation.Count <= i)
                        break;
                    
                    object val;
                    if (prefix != null && argInfo.NeedReindex)
                    {
                        val = operation[i];
                        if (val is string && ((string)val).StartsWith("@") && (((string)val).Length > 1))
                        {
                            val = "@" + prefix + "_" + ((string)val).Substring(1);
                        }
                    }
                    else
                    {
                        val = operation[i];
                    }
                    opArgs.Add(val);
                    i++;
                }

                if (opArgs.Count > 0)
                    op.PopulateFromMLFields(opArgs);

                if (label != null)
                    op.Label = label;
                else
                    op.Label = NextConsecutiveLabel(prevLabel);

                prevLabel = op.Label;
                result.Add(op);
            }

            return result;
        }

        private static void WriteCodePart(CodePart part, StringBuilder builder)
        {
            builder.AppendLine();
            builder.AppendLine("\t.functions:");
            WriteOpcodeList(part.FunctionsCode, builder);

            builder.AppendLine();
            builder.AppendLine("\t.init:");
            WriteOpcodeList(part.InitializationCode, builder);

            builder.AppendLine();
            builder.AppendLine("\t.main:");
            WriteOpcodeList(part.MainCode, builder);
            builder.AppendLine();
        }

        private static void WriteOpcodeList(List<Opcode> instructions, StringBuilder builder)
        {
            foreach(var instruction in instructions)
            {
                if(instruction.SourceName != null)
                    builder.AppendLine(string.Format(";{0}: line {1}", instruction.SourceName, instruction.SourceLine));

                if(!string.IsNullOrEmpty(instruction.Label))
                    builder.AppendFormat("{0}:", instruction.Label);

                builder.Append(" \t");
                builder.Append(instruction.Name);
                Utilities.SafeHouse.Logger.Log(instruction.ToString());
                builder.Append(" \t");

                bool firstArg = true;
                foreach(var arg in instruction.GetArgumentDefs())
                {
                    if(!firstArg)
                    {
                        builder.Append(", ");
                    } else
                    {
                        firstArg = false;
                    }

                    object argVal = arg.PropertyInfo.GetValue(instruction, null) ?? new PseudoNull();
                    WriteArgument(argVal, builder);
                }

                builder.Append("\n");
            }
        }

        private static string EscapeString(string input)
        {
            if (input == null)
                return "";

            /*var result = input;
            result = result.Replace("\\", "\\\\");
            result = result.Replace("\"", "\\\"");
            result = result.Replace("\n", "\\n");
            result = result.Replace("\t", "\\t");
            */
            StringBuilder b = new StringBuilder();
            b.EnsureCapacity(input.Length);
            var parts = input.Select((c) =>
            {
                if (c == '\\')
                    return "\\\\";
                else if (c == '\"')
                    return "\\\"";
                else if (c == '\n')
                    return "\\n";
                else if (c == '\t')
                    return "\\t";
                else if (char.IsControl(c))
                    return string.Format("\\u{0:X4}", (int)c);
                return "" + c;
            });
            
            foreach (var part in parts)
                b.Append(part);

            return b.ToString();
        }

        private static void WriteArgument(object obj, StringBuilder builder)
        {
            Utilities.SafeHouse.Logger.Log(obj.GetType().Name);
            if (obj == null || obj is PseudoNull) { builder.Append("null"); }
            else if (obj is KOSArgMarkerType) builder.Append("mark");
            else if (obj is Boolean) builder.Append((bool)obj);
            else if (obj is Int32) builder.AppendFormat(CultureInfo.InvariantCulture, "{0}i",(Int32)obj);
            else if (obj is String) builder.AppendFormat(CultureInfo.InvariantCulture, "\"{0}\"", EscapeString((String)obj));
            else if (obj is Double) builder.AppendFormat(CultureInfo.InvariantCulture, "{0:r}d", (Double)obj);
            else if (obj is Single) builder.AppendFormat(CultureInfo.InvariantCulture, "{0:r}f", (Single)obj);
            else if (obj is Byte) builder.AppendFormat(CultureInfo.InvariantCulture, "{0}B", (byte)obj);
            else if (obj is Char) builder.AppendFormat(CultureInfo.InvariantCulture, "{0:D}C", (int)((char)obj));
            else if (obj is Decimal) builder.AppendFormat(CultureInfo.InvariantCulture, "{0}m", (Decimal)obj);
            else if (obj is Int16) builder.AppendFormat(CultureInfo.InvariantCulture, "{0}s", (Int16)obj);
            else if (obj is Int64) builder.AppendFormat(CultureInfo.InvariantCulture, "{0}l", (Int64)obj);
            else if (obj is UInt16) builder.AppendFormat(CultureInfo.InvariantCulture, "{0}S", (UInt16)obj);
            else if (obj is UInt32) builder.AppendFormat(CultureInfo.InvariantCulture, "{0}I", (UInt32)obj);
            else if (obj is UInt64) builder.AppendFormat(CultureInfo.InvariantCulture, "{0}L", (UInt64)obj);
            else if (obj is SByte) builder.AppendFormat(CultureInfo.InvariantCulture, "{0}b", (SByte)obj);
            else if (obj is ScalarIntValue) builder.AppendFormat(CultureInfo.InvariantCulture, ":{0}i", ((ScalarIntValue)obj).GetIntValue());
            else if (obj is ScalarDoubleValue) builder.AppendFormat(CultureInfo.InvariantCulture, ":{0:r}d", ((ScalarDoubleValue)obj).GetDoubleValue());
            else if (obj is BooleanValue) builder.AppendFormat(CultureInfo.InvariantCulture, ":{0}", (BooleanValue)obj);
            else if (obj is StringValue) builder.AppendFormat(CultureInfo.InvariantCulture, ":\"{0}\"", EscapeString((StringValue)obj));
            else
                throw new Exception("Don't know how to write this type of object to assembly file: " + obj.GetType().Name);
        }
    }
}
