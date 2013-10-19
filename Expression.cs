using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;

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

		static String[] OperatorList = new String[] { "<=", ">=", "==", "!=", "=", "<", ">", "-", "+", "/", "*", "^" };
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

			if (!Utils.DelimterMatch (text)) 
			{
				throw new kOSException ("Error: mismatching delimiter.");
			}

	        UnwrapFullBrackets(ref text);

            Process(text);
        }

        private string dump()
        {
            if (Operator == null) return Value != null ? Value.ToString() : "null";

            string left = LeftSide != null ? LeftSide.dump() : "null";
            string right = RightSide != null ? RightSide.dump() : "null";

            return "|" + left + "|" + Operator + "|" + right + "|";
        }

        private void Process(String text)
        {
            if (TryParseDouble(text)) return;

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
            Match match = Regex.Match(text, Utils.BuildRegex("*:%"), RegexOptions.IgnoreCase);

            if (match.Success)
            {
                object leftSideResult = new Expression(match.Groups[1].Value, executionContext).GetValue();
                String suffixName = match.Groups[2].Value.ToUpper();

                if (leftSideResult is SpecialValue)
                {
                    EvalDlg = delegate()
                    {
                        Value = ((SpecialValue)leftSideResult).GetSuffix(suffixName);
                    };

                    EvalDlg();
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
                
                EvalDlg = delegate()
                {
                    Value = VesselUtils.GetResource(executionContext.Vessel, match.Groups[1].Value);
                };

                EvalDlg();
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

        public delegate void NumericFunctionParseDelegate(double[] parameters);

        private bool TryParseNumericFunction(String kegex, String text, NumericFunctionParseDelegate callback)
        {
            string regexStr = Utils.BuildRegex(kegex);
            var match = Regex.Match(text, regexStr, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                EvalDlg = delegate()
                {
                    match = Regex.Match(text, regexStr, RegexOptions.IgnoreCase);
                    List<double> values = new List<double>();

                    for (int i = 1; i < match.Groups.Count; i++)
                    {
                        double v = ParseSubExpressionAsDouble(match.Groups[i].Value);
                        values.Add((double)v);
                    }

                    callback(values.ToArray());
                };

                EvalDlg();

                return true;
            }

            return false;
        }

        private bool TryParseFunction(string text)
        {
            Match match;
            bool result;

            foreach(kOSExternalFunction f in executionContext.ExternalFunctions)
            {
                match = Regex.Match(text, f.regex, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    EvalDlg = delegate()
                    {
                        match = Regex.Match(text, f.regex, RegexOptions.IgnoreCase);
                        string[] pArr = new string[f.ParameterCount];

                        for (var i = 0; i < f.ParameterCount; i++)
                        {
                            pArr[i] = ParseSubExpressionAsString(match.Groups[i + 1].Value);
                        }

                        Value = executionContext.CallExternalFunction(f.Name, pArr);
                    };

                    EvalDlg();

                    return true;
                }
            }

            #region TRIG

            // Basic

            result = TryParseNumericFunction("SIN_(1)", text, delegate(double[] parameters)
            {
                Value = Math.Sin(parameters[0] * (Math.PI / 180));
            });
            if (result) return true;

            result = TryParseNumericFunction("COS_(1)", text, delegate(double[] parameters)
            {
                Value = Math.Cos(parameters[0] * (Math.PI / 180));
            });
            if (result) return true;

            result = TryParseNumericFunction("TAN_(1)", text, delegate(double[] parameters)
            {
                Value = Math.Tan(parameters[0] * (Math.PI / 180));
            });
            if (result) return true;

            // Inverse

            result = TryParseNumericFunction("ARCSIN_(1)", text, delegate(double[] parameters)
            {
                Value = (Math.Asin(parameters[0]) * (180 / Math.PI));
            });
            if (result) return true;

            result = TryParseNumericFunction("ARCCOS_(1)", text, delegate(double[] parameters)
            {
                Value = (Math.Acos(parameters[0]) * (180 / Math.PI));
            });
            if (result) return true;

            result = TryParseNumericFunction("ARCTAN_(1)", text, delegate(double[] parameters)
            {
                Value = Math.Atan(parameters[0]) * (180 / Math.PI);                
            });
            if (result) return true;

            result = TryParseNumericFunction("ARCTAN2_(2)", text, delegate(double[] parameters)
            {
                Value = Math.Atan2(parameters[0], parameters[1]) * (180 / Math.PI);    
            });
            if (result) return true;

            #endregion

            #region Other Math

            result = TryParseNumericFunction("ABS_(1)", text, delegate(double[] parameters)
            {
                Value = Math.Abs(parameters[0]);
            });
            if (result) return true;

            result = TryParseNumericFunction("FLOOR_(1)", text, delegate(double[] parameters)
            {
                Value = Math.Floor(parameters[0]);
            });
            if (result) return true;

            result = TryParseNumericFunction("CEILING_(1)", text, delegate(double[] parameters)
            {
                Value = Math.Ceiling(parameters[0]);
            });
            if (result) return true;

            result = TryParseNumericFunction("ROUND_(1)", text, delegate(double[] parameters)
            {
                Value = Math.Round(parameters[0]);
            });
            if (result) return true;

            result = TryParseNumericFunction("SQRT_(1)", text, delegate(double[] parameters)
            {
                Value = Math.Sqrt(parameters[0]);
            });
            if (result) return true;

            #endregion

            #region Geospatial

            result = TryParseNumericFunction("LATLNG_(2)", text, delegate(double[] parameters)
            {
                Value = new GeoCoordinates(executionContext.Vessel, parameters[0], parameters[1]);
            });
            if (result) return true;

            #endregion

            #region Time

            result = TryParseNumericFunction("T_(1)", text, delegate(double[] parameters)
            {
                Value = new TimeSpan(parameters[0]);
            });
            if (result) return true;

            #endregion

            #region Vectors & Rotations

            result = TryParseNumericFunction("V_(3)", text, delegate(double[] parameters)
            {
                Value = new Vector(parameters[0], parameters[1], parameters[2]);
            });
            if (result) return true;

            result = TryParseNumericFunction("R_(3)", text, delegate(double[] parameters)
            {
                Value = new Direction(new Vector3d(parameters[0], parameters[1], parameters[2]), true);
            });
            if (result) return true;


            match = Regex.Match(text, "^Q\\(([ :@A-Za-z0-9\\.\\-\\+\\*/]+),([ :@A-Za-z0-9\\.\\-\\+\\*/]+),([ :@A-Za-z0-9\\.\\-\\+\\*/]+),([ :@A-Za-z0-9\\.\\-\\+\\*/]+)\\)$", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                EvalDlg = delegate()
                {
                    match = Regex.Match(text, "^Q\\(([ :@A-Za-z0-9\\.\\-\\+\\*/]+),([ :@A-Za-z0-9\\.\\-\\+\\*/]+),([ :@A-Za-z0-9\\.\\-\\+\\*/]+),([ :@A-Za-z0-9\\.\\-\\+\\*/]+)\\)$", RegexOptions.IgnoreCase);

                    double x = ParseSubExpressionAsDouble(match.Groups[1].Value);
                    double y = ParseSubExpressionAsDouble(match.Groups[2].Value);
                    double z = ParseSubExpressionAsDouble(match.Groups[3].Value);
                    double w = ParseSubExpressionAsDouble(match.Groups[4].Value);

                    // eh? 
                    Value = x + " " + y + " " + z + " " + w;
                };

                EvalDlg();

                return true;
            }

            match = Regex.Match(text, "^HEADING ?([ :@A-Za-z0-9\\.\\-\\+\\*/]+) BY ([ :@A-Za-z0-9\\.\\-\\+\\*/]+)$", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                EvalDlg = delegate()
                {
                    match = Regex.Match(text, "^HEADING ?([ :@A-Za-z0-9\\.\\-\\+\\*/]+) BY ([ :@A-Za-z0-9\\.\\-\\+\\*/]+)$", RegexOptions.IgnoreCase);

                    double heading = ParseSubExpressionAsDouble(match.Groups[1].Value);
                    double pitch = ParseSubExpressionAsDouble(match.Groups[2].Value);

                    var q = UnityEngine.Quaternion.LookRotation(VesselUtils.GetNorthVector(executionContext.Vessel), executionContext.Vessel.upAxis);
                    q *= UnityEngine.Quaternion.Euler(new UnityEngine.Vector3((float)-pitch, (float)heading, 0));

                    Value = new Direction(q);
                };

                EvalDlg();

                return true;
            }



            #endregion

            #region Maneuver Nodes

            result = TryParseNumericFunction("NODE_(4)", text, delegate(double[] parameters)
            {
                Value = new Node(parameters[0], parameters[1], parameters[2], parameters[3]);
            });
            if (result) return true;

            #endregion

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

		private bool TryParseDouble(String text)
		{
			text = text.Trim();
            double testDouble;
			NumberStyles styles = NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite | NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;

			if (double.TryParse (text, styles, CultureInfo.InvariantCulture, out testDouble))
			{
				Value = testDouble;
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
                String variableName = startsWithAtSign ? text.Substring(1) : text;
                Variable variable = executionContext.FindVariable(variableName);

                if (variable != null)
                { 
                    if (variable.Value is SpecialValue)
                    {
                        IsStatic = startsWithAtSign;

                        if (IsStatic)
                        {
                            Variable = variable;
                            Value = Variable.Value;
                        }
                        else
                        {
                            EvalDlg = delegate()
                            {
                                executionContext.UpdateLock(variableName);
                                Value = variable.Value;
                            };

                            EvalDlg();
                        }

                        return true;
                    }
                    else
                    {
                        EvalDlg = delegate()
                        {
                            executionContext.UpdateLock(variableName);
                            Value = variable.Value;
                        };

                        EvalDlg();
                        return true;
                    }
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

                        // If this is a minus, and it comes right after an operator or is the beginning of the string, it's really a sign operator 
                        string prev = text.Substring(0, i);
                        bool isSign = (op == "-" && (EndsWithOperator(prev) || String.IsNullOrEmpty(prev.Trim())));

                        if (!isSign)
                        {
                            String leftString = text.Substring(0, i);
                            String rightString = text.Substring(i + op.Length);

                            LeftSide = new Expression(leftString, executionContext);
                            RightSide = new Expression(rightString, executionContext);

                            Value = GetValue();

                            return true;
                        }
                    }
                }
            }

            // No operator found!
            return false;
        }

        private bool EndsWithOperator(String input)
        {
            input = input.Trim();

            foreach (String op in OperatorList)
            {
                if (input.EndsWith(op)) return true;
            }

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
            var value = GetValue();
            if (value is double) return (float)((double)value);

            return (float)GetValue();
        }

        public double Double()
        {
            var value = GetValue();
            if (value is float) return (double)((float)value);

            return (double)GetValue();
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

                if (LeftSide.Value is SpecialValue)
                {
                    object result = ((SpecialValue)LeftSide.Value).TryOperation(Operator, RightSide.Value, false);
                    if (result != null) return result;
                }

                if (RightSide.Value is SpecialValue)
                {
                    object result = ((SpecialValue)RightSide.Value).TryOperation(Operator, LeftSide.Value, true);
                    if (result != null) return result;
                }

                if (LeftSide.Value is String || RightSide.Value is String)
                {
					if (Operator == "+") return LeftSide.Value.ToString() + RightSide.Value.ToString();

                    if (Operator == "==") return LeftSide.Value.ToString() == RightSide.Value.ToString();
                    if (Operator == "=") return LeftSide.Value.ToString() == RightSide.Value.ToString();
                    if (Operator == "!=") return LeftSide.Value.ToString() != RightSide.Value.ToString();
                }

                if (LeftSide.Value is double && RightSide.Value is Vector)
                {
                    if (Operator == "*") return (Vector)RightSide.GetValue() * LeftSide.Double();
                }
                if (LeftSide.Value is Vector && RightSide.Value is double)
                {
                    if (Operator == "*") return (Vector)LeftSide.GetValue() * RightSide.Double();
                }

                if (LeftSide.Value is float || LeftSide is double || RightSide.Value is float || RightSide.Value is double)
                {
                    if (Operator == "+") return LeftSide.Double() + RightSide.Double();
                    if (Operator == "-") return LeftSide.Double() - RightSide.Double();
                    if (Operator == "/") return LeftSide.Double() / RightSide.Double();
                    if (Operator == "*") return LeftSide.Double() * RightSide.Double();
                    if (Operator == "^") return Math.Pow(LeftSide.Double(), RightSide.Double());

                    if (Operator == "<") return LeftSide.Double() < RightSide.Double();
                    if (Operator == ">") return LeftSide.Double() > RightSide.Double();
                    if (Operator == "<=") return LeftSide.Double() <= RightSide.Double();
                    if (Operator == ">=") return LeftSide.Double() >= RightSide.Double();
                    if (Operator == "==") return LeftSide.Double() == RightSide.Double();
                    if (Operator == "=") return LeftSide.Double() == RightSide.Double();
                    if (Operator == "!=") return LeftSide.Double() != RightSide.Double();
                }

                if (LeftSide.Value is Direction && RightSide.Value is Direction)
                {
                    if (Operator == "*") return (Direction)LeftSide.GetValue() * (Direction)RightSide.GetValue();
                    if (Operator == "+") return (Direction)LeftSide.GetValue() + (Direction)RightSide.GetValue();
                    if (Operator == "-") return (Direction)LeftSide.GetValue() - (Direction)RightSide.GetValue();
                }

                if (LeftSide.Value is Direction && RightSide.Value is Vector)
                {
                    Vector RightVec = (Vector)RightSide.GetValue();

                    if (Operator == "*") return (Direction)LeftSide.GetValue() * RightVec.ToDirection();
                    if (Operator == "+") return (Direction)LeftSide.GetValue() + RightVec.ToDirection();
                    if (Operator == "-") return (Direction)LeftSide.GetValue() - RightVec.ToDirection();
                }

                if (LeftSide.Value is Vector && RightSide.Value is Vector)
                {
                    if (Operator == "*") return (Vector)LeftSide.GetValue() * (Vector)RightSide.GetValue();
                    if (Operator == "+") return (Vector)LeftSide.GetValue() + (Vector)RightSide.GetValue();
                    if (Operator == "-") return (Vector)LeftSide.GetValue() - (Vector)RightSide.GetValue();
                }
            }

            throw new kOSException("Expression error.");
        }
        
        public override String ToString()
        {
            var value = GetValue();
            if (value is float || value is double) return Double().ToString();

            return value.ToString();
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
            if (val is double) return ((double)val > 0);

            return false;
        }
    }

    public enum UnitType { DEFAULT, PERCENT, METERS, TEXT };
}
