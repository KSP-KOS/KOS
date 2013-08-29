using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace kOS
{
    public class Expression
    {
        public object Value;
        public Expression LeftSide;
        public Expression RightSide;
        public String Operator;
        public Variable Variable = null;
        public bool IsStatic = false;
        public ExecutionContext executionContext;

        static String[] OperatorList = new String[] { "==", "=", "<=", ">=", "<", ">", "-", "+", "/", "*", "^" };

        public static String Evaluate(String text, ExecutionContext context)
        {
            Expression e = new Expression(text, context);
            return e.GetValue().ToString();
        }

        public Expression(String text, ExecutionContext context)
        {
            this.executionContext = context;

            text = text.Trim();

            UnwrapFullBrackets(ref text);

            if (TryParseResource(text)) return;

            if (TryParseVariable(text)) return;

            if (TryParseDirection(text)) return;

            if (TryParseSuffix(text)) return;

            if (TryParseVessel(text)) return;

            if (TryParseFloat(text)) return;

            if (TryParseBoolean(text)) return;

            if (TryParseString(text)) return;
            
            if (Eval(ref text)) return;

            throw new kOSException("Unrecognized term: '" + text + "'.");
        }

        private bool TryParseSuffix(string text)
        {
            if (text.Contains(':'))
            {
                var parts = text.Split(':');

                var obj = new Expression(parts[0], executionContext).GetValue();
                if (obj is SpecialValue)
                {
                    Value = ((SpecialValue)obj).GetSuffix(parts[1]);
                    return true;
                }
            }

            return false;
        }

        private void UnwrapFullBrackets(ref String text)
        {
            if (text.StartsWith("("))
            {
                int end = FindEndOfBracket(text, 0);
                if (end == text.Length - 1)
                {
                    text = text.Substring(1, text.Length - 2);
                }
            }
        }

        private bool TryParseResource(string text)
        {
            Match match = Regex.Match(text, "^<(.+)>$", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                Value = VesselUtils.GetResource(executionContext.Vessel, match.Groups[1].Value);
                return true;
            }

            return false;
        }

        private bool TryParseVessel(string text)
        {
            Match match = Regex.Match(text, "^VESSEL\\(([ A-Za-z0-9\"]+)\\)$");
            if (match.Success)
            {
                var input = ParseSubExpressionAsString(match.Groups[1].Value.Trim());

                Value = new VesselTarget(VesselUtils.GetVesselByName(input), executionContext); // Will throw if not found
                return true;
            }

            return false;
        }
        
        private bool TryParseDirection(string text)
        {
            Match match = Regex.Match(text, "^V\\(([ A-Za-z0-9\\.\\-\\+\\*/]+),([ A-Za-z0-9\\.\\-\\+\\*/]+),([ A-Za-z0-9\\.\\-\\+\\*/]+)\\)$", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                double x = ParseSubExpressionAsDouble(match.Groups[1].Value);
                double y = ParseSubExpressionAsDouble(match.Groups[2].Value); 
                double z = ParseSubExpressionAsDouble(match.Groups[3].Value);

                Value = new Direction(new Vector3d(x, y, z), false);

                return true;
            }

            match = Regex.Match(text, "^R\\(([ A-Za-z0-9\\.\\-\\+\\*/]+),([ A-Za-z0-9\\.\\-\\+\\*/]+),([ A-Za-z0-9\\.\\-\\+\\*/]+)\\)$", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                double x = ParseSubExpressionAsDouble(match.Groups[1].Value);
                double y = ParseSubExpressionAsDouble(match.Groups[2].Value);
                double z = ParseSubExpressionAsDouble(match.Groups[3].Value);

                Value = new Direction(new Vector3d(x, y, z), true);

                return true;
            }

            return false;
        }

        private String ParseSubExpressionAsString(String input)
        {
            return new Expression(input, executionContext).ToString();
        }

        private double ParseSubExpressionAsDouble(String input)
        {
            double val = 0;
            if (double.TryParse(input, out val))
            {
                return val;
            }
            else
            {
                var expValue = new Expression(input, executionContext).GetValue();
                if (expValue is double)
                {
                    return (double)expValue;
                }
                else if (double.TryParse(expValue.ToString(), out val))
                {
                    return val;
                }
                else
                {
                    throw new kOSException("Non-numeric parameter used on a numeric function");
                }
            }
        }

        private bool TryParseFloat(String text)
        {
            text = text.Trim();
            float testFloat;

            if (float.TryParse(text, out testFloat))
            {
                Value = testFloat;
                return true;
            }

            return false;
        }

        private bool TryParseBoolean(String text)
        {
            text = text.Trim();
            bool testBool;

            if (bool.TryParse(text, out testBool))
            {
                Value = testBool;
                return true;
            }

            return false;
        }

        // Check if this term is a string
        private bool TryParseString(String text)
        {
            text = text.Trim();
            if (text.StartsWith("\""))
            {
                var end = FindEndOfString(text, 1);
                if (end == text.Length - 1)
                {
                    Value = text.Substring(1, text.Length - 2);
                    return true;
                }
            }

            return false;
        }
        
        // Check if this expression term represents a registered variable
        private bool TryParseVariable(String text)
        {
            if (this.Variable != null)
            {
                Value = this.Variable.Value;
                return true;
            }
            else
            {
                bool startsWithAtSign = text.StartsWith("@");
                Variable variable = executionContext.FindVariable(startsWithAtSign ? text.Substring(1) : text);

                if (variable != null)
                {
                    IsStatic = startsWithAtSign;
                    Value = variable.Value;
                    Variable = variable;
                    return true;
                }

                return false;
            }
        }

        // Find the next unescaped double quote
        public static int FindEndOfString(String text, int start)
        {
            char[] input = text.ToCharArray();
            for (int i = start; i < input.Count(); i++)
            {
                if (input[i] == '"' && input[i - 1] != '\\')
                {
                    return i;
                }
            }

            return -1;
        }

        public static int FindBeginningOfString(String text, int start)
        {
            char[] input = text.ToCharArray();
            for (int i = start; i >= 0; i--)
            {
                if (input[i] == '"' && (i == 0 || input[i - 1] != '\\'))
                {
                    return i;
                }
            }

            return -1;
        }

        // Find the end of a pair of round brackets
        public static int FindEndOfBracket(String text, int start)
        {
            char[] input = text.ToCharArray();
            int level = 0;

            for (int i = start; i < input.Count(); i++)
            {
                if (input[i] == '"')
                {
                    i = FindEndOfString(text, i + 1);
                }
                else if (input[i] == '(')
                {
                    level++;
                }
                else if (input[i] == ')')
                {
                    level--;
                    if (level == 0) return i;
                }
            }

            return -1;
        }

        // Evaluate a part of an expression
        private bool Eval(ref String text)
        {
            // Eval mathematical parts
            foreach (String op in OperatorList)
            {
                for (int i = 0; i < text.Length; i++)
                {
                    if (text.Substring(i, 1) == "\"")
                    {
                        // String detected, jump to end
                        i = FindEndOfString(text, i + 1);
                    }
                    else if (text.Substring(i, 1) == "(")
                    {
                        i = FindEndOfBracket(text, i);
                    }
                    else if (i <= text.Length - op.Length && op == text.Substring(i, op.Length))
                    {
                        Operator = op;

                        String leftString = text.Substring(0, i);
                        String rightString = text.Substring(i + op.Length);

                        LeftSide = new Expression(leftString, executionContext);
                        RightSide = new Expression(rightString, executionContext);

                        Value = GetValue();

                        return true;
                    }
                }
            }

            // No operator found!
            return false;
        }

        public float Float()
        {
            return (float)GetValue();
        }

        public double Double()
        {
            // By default numbers are stored as floats, and must be 'unboxed' before casting to double
            return (double)Float();
        }

        // Evaluate and return the value of the part of an expression that this instance represents
        public object GetValue()
        {
            if (LeftSide == null)
            {
                if (Variable != null && !IsStatic)
                {
                    return Variable.Value;
                }
                else
                {
                    return Value;
                }
            }
            else
            {
                if (LeftSide.Value is String || RightSide.Value is String)
                {
                    if (Operator == "+") return LeftSide.Value.ToString() + RightSide.Value.ToString();
                }

                if (LeftSide.Value is float || RightSide.Value is float)
                {
                    if (Operator == "+") return LeftSide.Float() + RightSide.Float();
                    if (Operator == "-") return LeftSide.Float() - RightSide.Float();
                    if (Operator == "/") return LeftSide.Float() / RightSide.Float();
                    if (Operator == "*") return LeftSide.Float() * RightSide.Float();
                    if (Operator == "^") return (float)Math.Pow(LeftSide.Double(), RightSide.Double());

                    if (Operator == "<") return LeftSide.Float() < RightSide.Float();
                    if (Operator == ">") return LeftSide.Float() > RightSide.Float();
                    if (Operator == "<=") return LeftSide.Float() <= RightSide.Float();
                    if (Operator == ">=") return LeftSide.Float() >= RightSide.Float();
                    if (Operator == "==") return LeftSide.Float() == RightSide.Float();
                    if (Operator == "=") return LeftSide.Float() == RightSide.Float();
                    if (Operator == "!=") return LeftSide.Float() != RightSide.Float();
                }

                if (LeftSide.Value is Direction && RightSide.Value is Direction)
                {
                    if (Operator == "*") return (Direction)LeftSide.Value * (Direction)RightSide.Value;
                    if (Operator == "+") return (Direction)LeftSide.Value + (Direction)RightSide.Value;
                    if (Operator == "-") return (Direction)LeftSide.Value + (Direction)RightSide.Value;
                }
            }

            throw new kOSException("Expression error.");
        }
        
        public override String ToString()
        {
            if (GetValue() is float)
            {
                return ((float)GetValue()).ToString("0.00");
            }

            return GetValue().ToString();
        }

        internal bool IsNull()
        {
            return (LeftSide == null && RightSide == null && Value == null);
        }

        internal bool IsTrue()
        {
            object val = GetValue();
            if (val is bool) return (bool)val;
            if (val is float) return ((float)val > 0);

            return false;
        }
    }

    public enum UnitType { DEFAULT, PERCENT, METERS, TEXT };
}
