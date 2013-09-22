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
        public SpecialValue SpecialValue;
        public String SpecialValueSuffix;
        public String Operator;
        public Variable Variable = null;
        public bool IsStatic = false;
        public ExpressionReEvalDlg EvalDlg = null;
        public ExecutionContext executionContext;
        public String Suffix;
        public String Text;

        public delegate void ExpressionReEvalDlg();

        static String[] OperatorList = new String[] { "==", "=", "<=", ">=", "<", ">", "-", "+", "/", "*", "^" };
        static String[] SpecialOperatorList = new String[] { "^\\sAND\\s", "^\\sOR\\s" };

        public static String Evaluate(String text, ExecutionContext context)
        {
            Expression e = new Expression(text, context);
            return e.GetValue().ToString();
        }

        public Expression(String text, ExecutionContext context)
        {
            this.executionContext = context;

            text = text.Trim();
            Text = text;
	    if(!CheckForBrackets (text))
	    {
	      throw new kOSException ("Missing brackets.");
	    };
	    UnwrapFullBrackets(ref text);

            Process(text);
        }

        private void Process(String text)
        {
            if (TryParseFloat(text)) return;

            if (TryParseBoolean(text)) return;

            if (TryParseString(text)) return;

            if (TryParseResource(text)) return;

            if (TryParseVariable(text)) return;

            if (TryParseFunction(text)) return;

            if (TryParseSuffix(text)) return;

            if (TryParseVessel(text)) return;

            if (EvalBoolean(ref text)) return;

            if (Eval(ref text)) return;

            throw new kOSException("Unrecognized term: '" + text + "'.");
        }

        private bool TryParseSuffix(string text)
        {
            Match match = Regex.Match(text, "^([A-Z0-9_\\-]+?):([A-Z0-9_\\-]+?)$", RegexOptions.IgnoreCase);

            if (match.Success)
            {
                var obj = new Expression(match.Groups[1].Value, executionContext).GetValue();
                if (obj is SpecialValue)
                {
                    SpecialValue = (SpecialValue)obj;
                    Suffix = match.Groups[2].Value.ToUpper();

                    Value = SpecialValue.GetSuffix(Suffix);
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
            Match match = Regex.Match(text, "^VESSEL\\(([ @A-Za-z0-9\"]+)\\)$");
            if (match.Success)
            {
                var input = ParseSubExpressionAsString(match.Groups[1].Value.Trim());

                Value = new VesselTarget(VesselUtils.GetVesselByName(input, executionContext.Vessel), executionContext); // Will throw if not found
                return true;
            }

            return false;
        }

        private bool TryParseFunction(string text)
        {
            Match match;

            #region TRIG
            match = Regex.Match(text, "^SIN\\(([ :@A-Za-z0-9\\.\\-\\+\\*/]+)\\)$", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                EvalDlg = delegate()
                {
                    match = Regex.Match(text, "^SIN\\(([ :@A-Za-z0-9\\.\\-\\+\\*/]+)\\)$", RegexOptions.IgnoreCase);
                    double v = ParseSubExpressionAsDouble(match.Groups[1].Value);
                    Value = (float)Math.Sin(v * (Math.PI / 180));
                };

                EvalDlg();

                return true;
            } 
            
            match = Regex.Match(text, "^COS\\(([ :@A-Za-z0-9\\.\\-\\+\\*/]+)\\)$", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                EvalDlg = delegate()
                {
                    match = Regex.Match(text, "^COS\\(([ :@A-Za-z0-9\\.\\-\\+\\*/]+)\\)$", RegexOptions.IgnoreCase);
                    double v = ParseSubExpressionAsDouble(match.Groups[1].Value);
                    Value = (float)Math.Cos(v * (Math.PI / 180));
                };

                EvalDlg();

                return true;
            }

            match = Regex.Match(text, "^TAN\\(([ :@A-Za-z0-9\\.\\-\\+\\*/]+)\\)$", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                EvalDlg = delegate()
                {
                    match = Regex.Match(text, "^TAN\\(([ :@A-Za-z0-9\\.\\-\\+\\*/]+)\\)$", RegexOptions.IgnoreCase);
                    double v = ParseSubExpressionAsDouble(match.Groups[1].Value);
                    Value = (float)Math.Tan(v * (Math.PI / 180));
                };

                EvalDlg();

                return true;
            }

            match = Regex.Match(text, "^ARCSIN\\(([ :@A-Za-z0-9\\.\\-\\+\\*/]+)\\)$", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                EvalDlg = delegate()
                {
                    match = Regex.Match(text, "^ARCSIN\\(([ :@A-Za-z0-9\\.\\-\\+\\*/]+)\\)$", RegexOptions.IgnoreCase);
                    double v = ParseSubExpressionAsDouble(match.Groups[1].Value);
                    Value = ((float)Math.Asin(v) * (180 / Math.PI));
                };

                EvalDlg();

                return true;
            }

            match = Regex.Match(text, "^ARCCOS\\(([ :@A-Za-z0-9\\.\\-\\+\\*/]+)\\)$", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                EvalDlg = delegate()
                {
                    match = Regex.Match(text, "^ARCCOS\\(([ :@A-Za-z0-9\\.\\-\\+\\*/]+)\\)$", RegexOptions.IgnoreCase);
                    double v = ParseSubExpressionAsDouble(match.Groups[1].Value);
                    Value = ((float)Math.Acos(v) * (180 / Math.PI));
                };

                EvalDlg();

                return true;
            }

            match = Regex.Match(text, "^ARCTAN\\(([ :@A-Za-z0-9\\.\\-\\+\\*/]+)\\)$", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                EvalDlg = delegate()
                {
                    match = Regex.Match(text, "^ARCTAN\\(([ :@A-Za-z0-9\\.\\-\\+\\*/]+)\\)$", RegexOptions.IgnoreCase);
                    double v = ParseSubExpressionAsDouble(match.Groups[1].Value);
                    Value = ((float)Math.Atan(v) * (180 / Math.PI));
                };

                EvalDlg();

                return true;
            }

            match = Regex.Match(text, "^ARCTAN2\\(([ :@A-Za-z0-9\\.\\-\\+\\*/]+),([ :@A-Za-z0-9\\.\\-\\+\\*/]+)\\)$", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                EvalDlg = delegate()
                {
                    match = Regex.Match(text, "^ARCTAN2\\(([ :@A-Za-z0-9\\.\\-\\+\\*/]+),([ :@A-Za-z0-9\\.\\-\\+\\*/]+)\\)$", RegexOptions.IgnoreCase);
                    double x = ParseSubExpressionAsDouble(match.Groups[1].Value);
                    double y = ParseSubExpressionAsDouble(match.Groups[2].Value);
                    Value = (float)(Math.Atan2(x, y) * (180 / Math.PI));
                };

                EvalDlg();

                return true;
            }
            #endregion

            #region ABS
            match = Regex.Match(text, "^ABS\\(([ :@A-Za-z0-9\\.\\-\\+\\*/]+)\\)$", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                EvalDlg = delegate()
                {
                    match = Regex.Match(text, "^ABS\\(([ :@A-Za-z0-9\\.\\-\\+\\*/]+)\\)$", RegexOptions.IgnoreCase);
                    double v = ParseSubExpressionAsDouble(match.Groups[1].Value);
                    Value = (float)Math.Abs(v);
                };

                EvalDlg();

                return true;
            } 
            #endregion

            #region Geospatial
            match = Regex.Match(text, "^LATLNG ?\\(([ :@A-Za-z0-9\\.\\-\\+\\*/]+),([ :@A-Za-z0-9\\.\\-\\+\\*/]+)\\)$", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                EvalDlg = delegate()
                {
                    match = Regex.Match(text, "^LATLNG ?\\(([ :@A-Za-z0-9\\.\\-\\+\\*/]+),([ :@A-Za-z0-9\\.\\-\\+\\*/]+)\\)$", RegexOptions.IgnoreCase);

                    double lat = ParseSubExpressionAsDouble(match.Groups[1].Value);
                    double lng = ParseSubExpressionAsDouble(match.Groups[2].Value);

                    Value = new GeoCoordinates(executionContext.Vessel, lat, lng);
                };

                EvalDlg();

                return true;
            }
            #endregion

            #region Vectors & Rotations
            match = Regex.Match(text, "^V\\(([ :@A-Za-z0-9\\.\\-\\+\\*/]+),([ :@A-Za-z0-9\\.\\-\\+\\*/]+),([ :@A-Za-z0-9\\.\\-\\+\\*/]+)\\)$", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                EvalDlg = delegate()
                {
                    match = Regex.Match(text, "^V\\(([ :@A-Za-z0-9\\.\\-\\+\\*/]+),([ :@A-Za-z0-9\\.\\-\\+\\*/]+),([ :@A-Za-z0-9\\.\\-\\+\\*/]+)\\)$", RegexOptions.IgnoreCase);

                    double x = ParseSubExpressionAsDouble(match.Groups[1].Value);
                    double y = ParseSubExpressionAsDouble(match.Groups[2].Value);
                    double z = ParseSubExpressionAsDouble(match.Groups[3].Value);

                    Value = new Vector(x,y,z);
                };

                EvalDlg();

                return true;
            }

            match = Regex.Match(text, "^R\\(([ :@A-Za-z0-9\\.\\-\\+\\*/]+),([ :@A-Za-z0-9\\.\\-\\+\\*/]+),([ :@A-Za-z0-9\\.\\-\\+\\*/]+)\\)$", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                EvalDlg = delegate()
                {
                    match = Regex.Match(text, "^R\\(([ :@A-Za-z0-9\\.\\-\\+\\*/]+),([ :@A-Za-z0-9\\.\\-\\+\\*/]+),([ :@A-Za-z0-9\\.\\-\\+\\*/]+)\\)$", RegexOptions.IgnoreCase);

                    double x = ParseSubExpressionAsDouble(match.Groups[1].Value);
                    double y = ParseSubExpressionAsDouble(match.Groups[2].Value);
                    double z = ParseSubExpressionAsDouble(match.Groups[3].Value);

                    Value = new Direction(new Vector3d(x, y, z), true);
                };

                EvalDlg();

                return true;
            }

            match = Regex.Match(text, "^Q\\(([ :@A-Za-z0-9\\.\\-\\+\\*/]+),([ :@A-Za-z0-9\\.\\-\\+\\*/]+),([ :@A-Za-z0-9\\.\\-\\+\\*/]+),([ :@A-Za-z0-9\\.\\-\\+\\*/]+)\\)$", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                EvalDlg = delegate()
                {
                    match = Regex.Match(text, "^Q\\(([ :@A-Za-z0-9\\.\\-\\+\\*/]+),([ :@A-Za-z0-9\\.\\-\\+\\*/]+),([ :@A-Za-z0-9\\.\\-\\+\\*/]+),([ :@A-Za-z0-9\\.\\-\\+\\*/]+)\\)$", RegexOptions.IgnoreCase);

                    float x = (float)ParseSubExpressionAsDouble(match.Groups[1].Value);
                    float y = (float)ParseSubExpressionAsDouble(match.Groups[2].Value);
                    float z = (float)ParseSubExpressionAsDouble(match.Groups[3].Value);
                    float w = (float)ParseSubExpressionAsDouble(match.Groups[4].Value);

                    Value = x + " " + y + " " + z + " " + w;
                };

                EvalDlg();

                return true;
            } 
            #endregion

            match = Regex.Match(text, "^HEADING ?([ :@A-Za-z0-9\\.\\-\\+\\*/]+) BY ([ :@A-Za-z0-9\\.\\-\\+\\*/]+)$", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                EvalDlg = delegate()
                {
                    match = Regex.Match(text, "^HEADING ?([ :@A-Za-z0-9\\.\\-\\+\\*/]+) BY ([ :@A-Za-z0-9\\.\\-\\+\\*/]+)$", RegexOptions.IgnoreCase);

                    float heading = (float)ParseSubExpressionAsDouble(match.Groups[1].Value);
                    float pitch = (float)ParseSubExpressionAsDouble(match.Groups[2].Value);

                    var q = UnityEngine.Quaternion.LookRotation(VesselUtils.GetNorthVector(executionContext.Vessel), executionContext.Vessel.upAxis);
                    q *= UnityEngine.Quaternion.Euler(new UnityEngine.Vector3(-pitch, heading, 0));

                    Value = new Direction(q);
                };

                EvalDlg();

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

        private static bool CheckForBrackets(string str)
	{
	  var items = new Stack<int>(str.Length);
	  for (int i = 0; i < str.Length; i++)
	  {
	    char c = str[i];
	    if (c == '(' || c == ')') 
	    {
	      items.Push (i);
	    }
	  }
	  if ((items.Count % 2) == 1)
	  {
	    return false;
	  }
	  return true;
	}

        // Evaluate a part of an expression
        private bool Eval(ref String text)
        {
            // Eval mathematical parts
            foreach (String op in OperatorList)
            {
                for (int i = 0; i < text.Length; i++)
                {
                    if (text.Substring(i, 1) == "<")
                    {
                        // Special case: is this less than or start of angle quote?
                        var regex = new Regex("^<[A-Za-z0-9]+?>"); // Make sure that there are no math symbols between start and end brace
                        var match = regex.Match(text.Substring(i));

                        if (match.Success)
                        {
                            // This is angle brackets, move to end bracket
                            i += match.Groups[0].Length;
                        }
                    }

                    if (text.Substring(i, 1) == "\"")
                    {
                        // String detected, jump to end
                        i = FindEndOfString(text, i + 1);
                    }
                    else if (text.Substring(i, 1) == "(")
                    {
                        i = FindEndOfBracket(text, i);
                    }
                    else if (i <= text.Length - op.Length && op == text.Substring(i, op.Length).ToUpper())
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

        private bool EvalBoolean(ref String text)
        {
            foreach (String op in SpecialOperatorList)
            {
                for (int i = 0; i < text.Length; i++)
                {
                    var regex = new Regex(op);
                    Match match = Regex.Match(text.Substring(i), op, RegexOptions.IgnoreCase);

                    if (match.Success)
                    {
                        Operator = match.Groups[0].Value.Trim().ToUpper();

                        String leftString = text.Substring(0, i);
                        String rightString = text.Substring(i + match.Groups[0].Value.Length);

                        LeftSide = new Expression(leftString, executionContext);
                        RightSide = new Expression(rightString, executionContext);

                        Value = GetValue();

                        return true;
                    }
                }
            }

            // No boolean operator found
            return false;
        }

        public float Float()
        {
            return (float)GetValue();
        }

        public Boolean Bool()
        {
            object val = GetValue();
            if (val is Boolean) return (Boolean)val;
            if (val is float) return ((float)val) > 0;
            if (val is String) return ((String)val) != "" && ((String)val) != "0" && ((String)val).ToUpper() != "FALSE";

            float fParse;
            if (float.TryParse(val.ToString(), out fParse))
            {
                return fParse > 0;
            }

            throw new kOSException("Unable to convert value to Boolean.");
        }

        public double Double()
        {
            // By default numbers are stored as floats, and must be 'unboxed' before casting to double
            return (double)Float();
        }

        // Evaluate and return the value of the part of an expression that this instance represents
        public object GetValue()
        {
            if (EvalDlg != null)
            {
                EvalDlg();
            }

            if (SpecialValue != null)
            {
                if (string.IsNullOrEmpty(Suffix)) 
                    return SpecialValue.ToString();
                else
                    return SpecialValue.GetSuffix(Suffix);
            }

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
                if (Operator == "AND") return LeftSide.Bool() && RightSide.Bool();
                if (Operator == "OR") return LeftSide.Bool() || RightSide.Bool();

                if (LeftSide.Value is String || RightSide.Value is String)
                {
                    if (Operator == "+") return LeftSide.Value.ToString() + RightSide.Value.ToString();

                    if (Operator == "==") return LeftSide.Value.ToString() == RightSide.Value.ToString();
                    if (Operator == "=") return LeftSide.Value.ToString() == RightSide.Value.ToString();
                    if (Operator == "!=") return LeftSide.Value.ToString() != RightSide.Value.ToString();
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
                    if (Operator == "*") return (Direction)LeftSide.GetValue() * (Direction)RightSide.GetValue();
                    if (Operator == "+") return (Direction)LeftSide.GetValue() + (Direction)RightSide.GetValue();
                    if (Operator == "-") return (Direction)LeftSide.GetValue() + (Direction)RightSide.GetValue();
                }

                if (LeftSide.Value is Direction && RightSide.Value is Vector)
                {
                    Vector RightVec = (Vector)RightSide.GetValue();

                    if (Operator == "*") return (Direction)LeftSide.GetValue() * RightVec.ToDirection();
                    if (Operator == "+") return (Direction)LeftSide.GetValue() + RightVec.ToDirection();
                    if (Operator == "-") return (Direction)LeftSide.GetValue() + RightVec.ToDirection();
                }

                if (LeftSide.Value is Vector && RightSide.Value is Vector)
                {
                    if (Operator == "*") return (Vector)LeftSide.GetValue() * (Vector)RightSide.GetValue();
                    if (Operator == "+") return (Vector)LeftSide.GetValue() + (Vector)RightSide.GetValue();
                    if (Operator == "-") return (Vector)LeftSide.GetValue() + (Vector)RightSide.GetValue();
                }
            }

            throw new kOSException("Expression error.");
        }
        
        public override String ToString()
        {
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
