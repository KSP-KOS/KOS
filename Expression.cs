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
        Term rootTerm;
        ExecutionContext executionContext;

        public Expression(String text, ExecutionContext context)
        {
            rootTerm = new Term(text);
            this.executionContext = context;
        }

        public object GetValue()
        {
            return GetValueOfTerm(rootTerm);
        }

        public object GetValueOfTerm(Term term)
        {
            object output;

            if (term.Type == Term.TermTypes.FINAL) // 'Final' terms can't be boiled down further, they should always be constants or variables
            {
                output = RecognizeConstant(term.Text);
                if (output != null) return output;

                output = AttemptGetVariableValue(term.Text);
                if (output != null) return output;
            }
            else if (term.Type == Term.TermTypes.REGULAR) 
            {
                output = TryProcessMathStatement(term);
                if (output != null) return output;
            }
            else if (term.Type == Term.TermTypes.FUNCTION)
            {
                output = TryProcessFunction(term);
                if (output != null) return output;
            }
            else if (term.Type == Term.TermTypes.STRUCTURE)
            {
                output = TryProcessStructure(term);
                if (output != null) return output;
            }
            else if (term.Type == Term.TermTypes.COMPARISON)
            {
                output = TryProcessComparison(term);
                if (output != null) return output;
            }
            else if (term.Type == Term.TermTypes.BOOLEAN)
            {
                output = TryProcessBoolean(term);
                if (output != null) return output;
            }
            
            throw new kOSException("Unrecognized term: '" + term.Text + "'");

            return null;
        }

        private object RecognizeConstant(String text)
        {
            text = text.Trim();

            // Numbers
            double testDouble;
            NumberStyles styles = NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite | NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;
            if (double.TryParse(text, styles, CultureInfo.InvariantCulture, out testDouble)) return testDouble;

            // Booleans
            bool testBool;
            if (bool.TryParse(text, out testBool)) return testBool;

            // Strings
            if (text.StartsWith("\""))
            {
                var end = Utils.FindEndOfString(text, 1);
                if (end == text.Length - 1) return text.Substring(1, text.Length - 2);
            }

            return null;
        }

        private void ReplaceChunkPairAt(ref List<StatementChunk> chunks, int index, StatementChunk replace)
        {
            chunks.RemoveRange(index, 2);
            chunks.Insert(index, replace);
        }

        private object TryProcessMathStatement(Term input)
        {
            List<StatementChunk> chunks = new List<StatementChunk>();

            for (int i = 0; i < input.SubTerms.Count; i += 2)
            {
                object termValue = GetValueOfTerm(input.SubTerms[i]);

                if (i + 1 < input.SubTerms.Count)
                {
                    Term opTerm = input.SubTerms[i + 1];
                    if (opTerm.Type == Term.TermTypes.MATH_OPERATOR)
                    {
                        chunks.Add(new StatementChunk(termValue, opTerm.Text));
                    }
                    else
                    {
                        throw new kOSException("Expression error processing statement '" + input.ToString() + "'");
                    }
                }
                else
                {
                    chunks.Add(new StatementChunk(termValue, ""));
                }
            }

            #region Exponents

            for (int i = 0; i < chunks.Count - 1; i++)
            {
                var c1 = chunks[i];
                var c2 = chunks[i + 1];

                if (c1.Opr == "^")
                {
                    var resultValue = AttemptPow(c1.Value, c2.Value);
                    if (resultValue == null) throw new kOSException("Can't use exponents with " + GetFriendlyNameOfItem(c1.Value) + " and " + GetFriendlyNameOfItem(c2.Value));

                    ReplaceChunkPairAt(ref chunks, i, new StatementChunk(resultValue, c2.Opr));
                    i--;
                }
            }

            #endregion

            #region Multiplication and Division

            for (int i = 0; i < chunks.Count - 1; i ++)
            {
                var c1 = chunks[i];
                var c2 = chunks[i + 1];

                if (c1.Opr == "*")
                {
                    var resultValue = AttemptMultiply(c1.Value, c2.Value);
                    if (resultValue == null) throw new kOSException("Can't multiply " + GetFriendlyNameOfItem(c1.Value) + " by " + GetFriendlyNameOfItem(c2.Value));

                    ReplaceChunkPairAt(ref chunks, i, new StatementChunk(resultValue, c2.Opr));
                    i--;
                }
                else if (c1.Opr == "/")
                {
                    var resultValue = AttemptDivide(c1.Value, c2.Value);
                    if (resultValue == null) throw new kOSException("Can't divide " + GetFriendlyNameOfItem(c1.Value) + " by " + GetFriendlyNameOfItem(c2.Value));

                    ReplaceChunkPairAt(ref chunks, i, new StatementChunk(resultValue, c2.Opr));
                    i--;
                }
            }

            #endregion

            #region Addition and Subtraction

            for (int i = 0; i < chunks.Count - 1; i++)
            {
                var c1 = chunks[i];
                var c2 = chunks[i + 1];

                if (c1.Opr == "+")
                {
                    var resultValue = AttemptAdd(c1.Value, c2.Value);
                    if (resultValue == null) throw new kOSException("Can't add " + GetFriendlyNameOfItem(c1.Value) + " and " + GetFriendlyNameOfItem(c2.Value));

                    ReplaceChunkPairAt(ref chunks, i, new StatementChunk(resultValue, c2.Opr));
                    i--;
                }
                else if (c1.Opr == "-")
                {
                    var resultValue = AttemptSubtract(c1.Value, c2.Value);
                    if (resultValue == null) throw new kOSException("Can't subtract " + GetFriendlyNameOfItem(c2.Value) + " from " + GetFriendlyNameOfItem(c1.Value));

                    ReplaceChunkPairAt(ref chunks, i, new StatementChunk(resultValue, c2.Opr));
                    i--;
                }
            }

            #endregion

            // If everything occurred correctly I should be left with one math chunk containing nothing but the resultant value
            if (chunks.Count == 1)
            {
                return chunks[0].Value;
            }

            return null;
        }

        private object TryProcessFunction(Term input)
        {
            object output;
            Term[] p = input.SubTerms[1].SubTerms.ToArray();

            output = TryCreateSV(input.SubTerms[0].Text, p);
            if (output != null) return output;

            output = TryMathFunction(input.SubTerms[0].Text, p);
            if (output != null) return output;

            output = TryExternalFunction(input.SubTerms[0].Text, p);
            if (output != null) return output;

            return null;
        }

        private object TryProcessStructure(Term input)
        {
            Term baseTerm = input.SubTerms[0];
            Term suffixTerm = input.SubTerms[1];

            if (suffixTerm.Type == Term.TermTypes.SUFFIX)
            {
                object baseTermValue = GetValueOfTerm(baseTerm);
                if (baseTermValue is SpecialValue)
                {
                    object output = ((SpecialValue)baseTermValue).GetSuffix(suffixTerm.Text.ToUpper());
                    if (output != null) return output;

                    throw new kOSException("Suffix '" + suffixTerm.Text + "' not found on object");
                }
                else
                {
                    throw new kOSException("Values of type " + GetFriendlyNameOfItem(baseTermValue) + " cannot have suffixes");
                }
            }

            return null;
        }

        private object TryProcessComparison(Term input)
        {
            List<StatementChunk> chunks = new List<StatementChunk>();

            for (int i = 0; i < input.SubTerms.Count; i += 2)
            {
                object termValue = GetValueOfTerm(input.SubTerms[i]);

                if (i + 1 < input.SubTerms.Count)
                {
                    Term opTerm = input.SubTerms[i + 1];
                    if (opTerm.Type == Term.TermTypes.COMPARISON_OPERATOR)
                    {
                        chunks.Add(new StatementChunk(termValue, opTerm.Text));
                    }
                    else
                    {
                        throw new kOSException("Expression error processing comparison '" + input.ToString() + "'");
                    }
                }
                else
                {
                    chunks.Add(new StatementChunk(termValue, ""));
                }
            }

            for (int i = 0; i < chunks.Count - 1; i++)
            {
                var c1 = chunks[i];
                var c2 = chunks[i + 1];
                object resultValue = null;

                if (c1.Opr == "==" || c1.Opr == "=") resultValue = AttemptEq(c1.Value, c2.Value);
                else if (c1.Opr == "!=") resultValue = AttemptNotEq(c1.Value, c2.Value);
                else if (c1.Opr == "<") resultValue = AttemptLT(c1.Value, c2.Value);
                else if (c1.Opr == ">") resultValue = AttemptGT(c1.Value, c2.Value);
                else if (c1.Opr == "<=") resultValue = AttemptLTE(c1.Value, c2.Value);
                else if (c1.Opr == ">=") resultValue = AttemptGTE(c1.Value, c2.Value);

                if (resultValue == null) throw new kOSException("Can't compare " + GetFriendlyNameOfItem(c1.Value) + " to " + GetFriendlyNameOfItem(c2.Value) + " using " + c1.Opr);

                ReplaceChunkPairAt(ref chunks, i, new StatementChunk(resultValue, c2.Opr));
                i--;
            }
            
            if (chunks.Count == 1)
            {
                return chunks[0].Value;
            }

            return null;
        }

        private object TryProcessBoolean(Term input)
        {
            List<StatementChunk> chunks = new List<StatementChunk>();

            for (int i = 0; i < input.SubTerms.Count; i += 2)
            {
                object termValue = GetValueOfTerm(input.SubTerms[i]);

                if (i + 1 < input.SubTerms.Count)
                {
                    Term opTerm = input.SubTerms[i + 1];
                    if (opTerm.Type == Term.TermTypes.BOOLEAN_OPERATOR)
                    {
                        chunks.Add(new StatementChunk(termValue, opTerm.Text));
                    }
                    else
                    {
                        throw new kOSException("Expression error processing boolean operation '" + input.ToString() + "'");
                    }
                }
                else
                {
                    chunks.Add(new StatementChunk(termValue, ""));
                }
            }

            for (int i = 0; i < chunks.Count - 1; i++)
            {
                var c1 = chunks[i];
                var c2 = chunks[i + 1];
                object resultValue = null;

                if (c1.Opr == "AND") resultValue = AttemptAnd(c1.Value, c2.Value);
                else if (c1.Opr == "OR") resultValue = AttemptOr(c1.Value, c2.Value);

                if (resultValue == null) throw new kOSException("Can't compare " + GetFriendlyNameOfItem(c1.Value) + " to " + GetFriendlyNameOfItem(c2.Value) + " using " + c1.Opr);

                ReplaceChunkPairAt(ref chunks, i, new StatementChunk(resultValue, c2.Opr));
                i--;
            }

            if (chunks.Count == 1)
            {
                return chunks[0].Value;
            }

            return null;
        }

        private object TryExternalFunction(String name, Term[] p)
        {
            foreach (kOSExternalFunction f in executionContext.ExternalFunctions)
            {
                if (f.Name.ToUpper() == name.ToUpper())
                {
                    if (p.Count() != f.ParameterCount) throw new Exception("Wrong number of arguments, expected " + f.ParameterCount);

                    String[] sp = new String[f.ParameterCount];
                    for (int i = 0; i < f.ParameterCount; i++)
                    {
                        sp[i] = GetValueOfTerm(p[i]).ToString();
                    }

                    object output = executionContext.CallExternalFunction(f.Name, sp);
                    if (output != null) return output;
                }
            }

            return null;
        }

        private object TryMathFunction(String name, Term[] p)
        {
            name = name.ToUpper();

            if (name == "SIN") { double[] dp = GetParamsAsT<double>(p, 1); return Math.Sin(dp[0] * (Math.PI / 180)); }
            if (name == "COS") { double[] dp = GetParamsAsT<double>(p, 1); return Math.Cos(dp[0] * (Math.PI / 180)); }
            if (name == "TAN") { double[] dp = GetParamsAsT<double>(p, 1); return Math.Tan(dp[0] * (Math.PI / 180)); }
            if (name == "ARCSIN") { double[] dp = GetParamsAsT<double>(p, 1); return Math.Asin(dp[0]) * (180 / Math.PI); }
            if (name == "ARCCOS") { double[] dp = GetParamsAsT<double>(p, 1); return Math.Acos(dp[0]) * (180 / Math.PI); }
            if (name == "ARCTAN") { double[] dp = GetParamsAsT<double>(p, 1); return Math.Atan(dp[0]) * (180 / Math.PI); }
            if (name == "ARCTAN2") { double[] dp = GetParamsAsT<double>(p, 2); return Math.Atan2(dp[0], dp[1]) * (180 / Math.PI); }

            if (name == "ABS") { double[] dp = GetParamsAsT<double>(p, 1); return Math.Abs(dp[0]); }
            if (name == "FLOOR") { double[] dp = GetParamsAsT<double>(p, 1); return Math.Floor(dp[0]); }
            if (name == "CEILING") { double[] dp = GetParamsAsT<double>(p, 1); return Math.Ceiling(dp[0]); }
            if (name == "ROUND") { double[] dp = GetParamsAsT<double>(p, 1); return Math.Round(dp[0]); }
            if (name == "SQRT") { double[] dp = GetParamsAsT<double>(p, 1); return Math.Sqrt(dp[0]); }

            return null;
        }

        private SpecialValue TryCreateSV(String name, Term[] p)
        {
            name = name.ToUpper();

            if (name == "NODE") { double[] dp = GetParamsAsT<double>(p, 4); return new Node(dp[0], dp[1], dp[2], dp[3]); }
            if (name == "V") { double[] dp = GetParamsAsT<double>(p, 3); return new Vector(dp[0], dp[1], dp[2]); }
            if (name == "R") { double[] dp = GetParamsAsT<double>(p, 3); return new Direction(new Vector3d(dp[0], dp[1], dp[2]), true); }
            if (name == "Q") { double[] dp = GetParamsAsT<double>(p, 4); return new Direction(new UnityEngine.Quaternion((float)dp[0], (float)dp[1], (float)dp[2], (float)dp[3])); }
            if (name == "T") { double[] dp = GetParamsAsT<double>(p, 1); return new TimeSpan(dp[0]); }
            if (name == "LATLNG") { double[] dp = GetParamsAsT<double>(p, 2); return new GeoCoordinates(executionContext.Vessel, dp[0], dp[1]); }
            if (name == "VESSEL") { String[] sp = GetParamsAsT<String>(p, 1); return new VesselTarget(VesselUtils.GetVesselByName(sp[0], executionContext.Vessel), executionContext); }
            if (name == "BODY") { String[] sp = GetParamsAsT<String>(p, 1); return new BodyTarget(sp[0], executionContext); }

            if (name == "HEADING")
            {
                int pCount = p.Count();
                if (pCount < 2 || pCount > 3) throw new kOSException("Wrong number of arguments supplied, expected 2 or 3");

                double[] dp = GetParamsAsT<double>(p, pCount);
                var q = UnityEngine.Quaternion.LookRotation(VesselUtils.GetNorthVector(executionContext.Vessel), executionContext.Vessel.upAxis);
                q *= UnityEngine.Quaternion.Euler(new UnityEngine.Vector3((float)-dp[0], (float)dp[1], (float)(dp.Count() > 2 ? dp[2] : 0)));

                return new Direction(q);
            }

            return null;
        }

        private T GetParamAsT<T>(Term input)
        {
            object value = GetValueOfTerm(input);
            if (value is T) return (T)value;

            if (typeof(T) == typeof(double)) throw new kOSException("Supplied parameter '" + input.Text + "' is not a number");
            if (typeof(T) == typeof(String)) throw new kOSException("Supplied parameter '" + input.Text + "' is not a string");
            if (typeof(T) == typeof(bool)) throw new kOSException("Supplied parameter '" + input.Text + "' is not a boolean");

            throw new kOSException("Supplied parameter '" + input.Text + "' is not of the correct type");
        }

        private T[] GetParamsAsT<T>(Term[] input, int size)
        {
            if (input.Count() != size)
            {
                throw new kOSException("Wrong number of arguments supplied, expected " + size);
            }

            T[] retVal = new T[size];

            for (var i = 0; i < size; i++)
            {
                retVal[i] = GetParamAsT<T>(input[i]);
            }

            return retVal;
        }

        private String GetFriendlyNameOfItem(object input)
        {
            if (input is String) return "string";
            if (input is double) return "number";
            if (input is float) return "number";
            if (input is int) return "number";
            if (input is VesselTarget) return "vessel";
            if (input is SpecialValue) return input.GetType().ToString().Replace("kOS.", "").ToLower();

            return "";
        }

        private object AttemptMultiply(object val1, object val2)
        {
            if (val1 is double && val2 is double) { return (double)val1 * (double)val2; }
            if (val1 is SpecialValue) { return ((SpecialValue)val1).TryOperation("*", val2, false); }
            if (val2 is SpecialValue) { return ((SpecialValue)val2).TryOperation("*", val1, true); }

            return null;
        }

        private object AttemptDivide(object val1, object val2)
        {
            if (val1 is double && val2 is double) { return (double)val1 / (double)val2; }
            if (val1 is SpecialValue) { return ((SpecialValue)val1).TryOperation("/", val2, false); }
            if (val2 is SpecialValue) { return ((SpecialValue)val2).TryOperation("/", val1, true); }

            return null;
        }

        private object AttemptAdd(object val1, object val2)
        {
            if (val1 is String || val2 is String) { return val1.ToString() + val2.ToString(); }

            if (val1 is double && val2 is double) { return (double)val1 + (double)val2; }
            if (val1 is SpecialValue) { return ((SpecialValue)val1).TryOperation("+", val2, false); }
            if (val2 is SpecialValue) { return ((SpecialValue)val2).TryOperation("+", val1, true); }

            return null;
        }

        private object AttemptSubtract(object val1, object val2)
        {
            if (val1 is double && val2 is double) { return (double)val1 - (double)val2; }
            if (val1 is SpecialValue) { return ((SpecialValue)val1).TryOperation("-", val2, false); }
            if (val2 is SpecialValue) { return ((SpecialValue)val2).TryOperation("-", val1, true); }

            return null;
        }

        private object AttemptPow(object val1, object val2)
        {
            if (val1 is double && val2 is double) { return Math.Pow((double)val1, (double)val2); }

            return null;
        }

        private object AttemptGetVariableValue(string varName)
        {
            executionContext.UpdateLock(varName);
            Variable v = executionContext.FindVariable(varName);
            
            return v == null ? null : (v.Value is float ? (double)((float)v.Value) : v.Value);
        }

        private object AttemptEq(object val1, object val2)
        {
            if (val1 is double && val2 is double) { return (double)val1 == (double)val2; }
            if (val1 is String || val2 is String) { return val1.ToString() == val2.ToString(); }
            if (val1 is SpecialValue) { return ((SpecialValue)val1).TryOperation("=", val2, false); }

            return null;
        }

        private object AttemptNotEq(object val1, object val2)
        {
            if (val1 is double && val2 is double) { return (double)val1 != (double)val2; }
            if (val1 is String || val2 is String) { return val1.ToString() != val2.ToString(); }
            if (val1 is SpecialValue) { return ((SpecialValue)val1).TryOperation("!=", val2, false); }

            return null;
        }

        private object AttemptGT(object val1, object val2)
        {
            if (val1 is double && val2 is double) { return (double)val1 > (double)val2; }
            if (val1 is SpecialValue) { return ((SpecialValue)val1).TryOperation(">", val2, false); }
            if (val2 is SpecialValue) { return ((SpecialValue)val2).TryOperation(">", val1, true); }

            return null;
        }

        private object AttemptLT(object val1, object val2)
        {
            if (val1 is double && val2 is double) { return (double)val1 < (double)val2; }
            if (val1 is SpecialValue) { return ((SpecialValue)val1).TryOperation("<", val2, false); }
            if (val2 is SpecialValue) { return ((SpecialValue)val2).TryOperation("<", val1, true); }

            return null;
        }

        private object AttemptGTE(object val1, object val2)
        {
            if (val1 is double && val2 is double) { return (double)val1 >= (double)val2; }
            if (val1 is SpecialValue) { return ((SpecialValue)val1).TryOperation(">=", val2, false); }
            if (val2 is SpecialValue) { return ((SpecialValue)val2).TryOperation(">=", val1, true); }

            return null;
        }

        private object AttemptLTE(object val1, object val2)
        {
            if (val1 is double && val2 is double) { return (double)val1 <= (double)val2; }
            if (val1 is SpecialValue) { return ((SpecialValue)val1).TryOperation("<=", val2, false); }
            if (val2 is SpecialValue) { return ((SpecialValue)val2).TryOperation("<=", val1, true); }

            return null;
        }

        private object AttemptAnd(object val1, object val2)
        {
            double v1 = (val1 is double) ? (double)val1 : 0;
            if (val1 is String && !double.TryParse((String)val1, out v1)) return null;

            double v2 = (val2 is double) ? (double)val2 : 0;
            if (val2 is String && !double.TryParse((String)val2, out v2)) return null;

            return (v1 > 0 && v2 > 0);
        }

        private object AttemptOr(object val1, object val2)
        {
            double v1 = (val1 is double) ? (double)val1 : 0;
            if (val1 is String && !double.TryParse((String)val1, out v1)) return null;

            double v2 = (val2 is double) ? (double)val2 : 0;
            if (val2 is String && !double.TryParse((String)val2, out v2)) return null;

            return (v1 > 0 || v2 > 0);
        }

        public bool IsNull()
        {
            object value = GetValue();

            return value == null;
        }

        public bool IsTrue()
        {
            object value = GetValue();

            if (value == null) return false;
            else if (value is bool) return (bool)value;
            else if (value is double) return (double)value > 0;
            else if (value is string)
            {
                bool boolVal;
                if (bool.TryParse((string)value, out boolVal)) return boolVal;

                double numberVal;
                if (double.TryParse((string)value, out numberVal)) return (double)numberVal > 0;

                return ((string)value).Trim() != "";
            }
            else if (value is SpecialValue)
            {
                return true;
            }

            return false;
        }

        public double Double()
        {
            object value = GetValue();

            if (value == null) return 0;
            else if (value is bool) return (bool)value ? 1 : 0;
            else if (value is double) return (double)value;
            else if (value is string)
            {
                double numberVal;
                if (double.TryParse((string)value, out numberVal)) return (double)numberVal;

                return 0;
            }

            return 0;
        }

        public float Float()
        {
            return (float)Double();
        }

        public override string ToString()
        {
            return GetValue().ToString();
        }

        private struct StatementChunk
        {
            public StatementChunk(object value, String opr)
            {
                this.Value = value;
                this.Opr = opr;
            }

            public object Value;
            public String Opr;
        }
    }
    
    /*
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
            Match match = Regex.Match(text, "^VESSEL\\(([ @A-Za-z0-9\"]+)\\)$", RegexOptions.IgnoreCase);
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

            result = TryParseNumericFunction("Q_(4)", text, delegate(double[] parameters)
            {
                Value = new Direction(new UnityEngine.Quaternion((float)parameters[0], (float)parameters[1], (float)parameters[2], (float)parameters[3]));
            });
            if (result) return true;


            match = Regex.Match(text, "^HEADING ?([ :@A-Za-z0-9\\.\\-\\+/\\*]+) BY ([ :@A-Za-z0-9\\.\\-\\+/\\*]+)$", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                EvalDlg = delegate()
                {
                    match = Regex.Match(text, "^HEADING ?([ :@A-Za-z0-9\\.\\-\\+/\\*]+) BY ([ :@A-Za-z0-9\\.\\-\\+/\\*]+)$", RegexOptions.IgnoreCase);

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
    }*/

    //public enum UnitType { DEFAULT, PERCENT, METERS, TEXT };
}
