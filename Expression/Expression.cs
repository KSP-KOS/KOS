using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using kOS.Context;
using kOS.Debug;
using kOS.Suffixed;
using kOS.Utilities;
using Random = System.Random;
using TimeSpan = kOS.Suffixed.TimeSpan;

namespace kOS.Expression
{
    public class Expression
    {
        private readonly Random random = new Random(); 
        private readonly IExecutionContext executionContext;
        private readonly Term rootTerm;

        public Expression(Term term, IExecutionContext context)
        {
            rootTerm = term;
            executionContext = context;
        }

        public Expression(string text, IExecutionContext context)
        {
            rootTerm = new Term(text);
            executionContext = context;
        }

        public object GetValue()
        {
            return GetValueOfTerm(rootTerm);
        }

        public object GetValueOfTerm(Term term)
        {
            object output;

            switch (term.Type)
            {
                case Term.TermTypes.FINAL:
                    output = RecognizeConstant(term.Text);
                    if (output != null) return output;
                    output = AttemptGetVariableValue(term.Text);
                    if (output != null) return output;
                    break;
                case Term.TermTypes.REGULAR:
                    output = TryProcessMathStatement(term);
                    if (output != null) return output;
                    break;
                case Term.TermTypes.FUNCTION:
                    output = TryProcessFunction(term);
                    if (output != null) return output;
                    break;
                case Term.TermTypes.STRUCTURE:
                    output = TryProcessStructure(term);
                    if (output != null) return output;
                    break;
                case Term.TermTypes.COMPARISON:
                    output = TryProcessComparison(term);
                    if (output != null) return output;
                    break;
                case Term.TermTypes.BOOLEAN:
                    output = TryProcessBoolean(term);
                    if (output != null) return output;
                    break;
                case Term.TermTypes.INDEX:
                    output = TryProcessIndex(term);
                    if (output != null) return output;
                    break;
            }

            throw new KOSException("Unrecognized term: '" + term.Text + "', Type:" + term.Type, executionContext);
        }

        private object TryProcessIndex(Term input)
        {
            var chunks = new List<StatementChunk>();

            for (var i = 0; i < input.SubTerms.Count; i += 2)
            {
                var termValue = GetValueOfTerm(input.SubTerms[i]);

                if (i + 1 < input.SubTerms.Count)
                {
                    var opTerm = input.SubTerms[i + 1];
                    if (opTerm.Type == Term.TermTypes.INDEX_OPERATOR)
                    {
                        chunks.Add(new StatementChunk(termValue, opTerm.Text));
                    }
                    else
                    {
                        throw new KOSException("Expression error processing boolean operation '" + input + "'",
                                               executionContext);
                    }
                }
                else
                {
                    chunks.Add(new StatementChunk(termValue, ""));
                }
            }

            for (var i = 0; i < chunks.Count - 1; i++)
            {
                var c1 = chunks[i];
                var c2 = chunks[i + 1];
                object resultValue = null;

                if (c1.Operator == "#")
                {
                    resultValue = AttemptAnd(c1.Value, c2.Value);
                    var baseTermValue = c1.Value as ListValue;

                    int suffixIntValue;
                    var secondChunkIsInt = int.TryParse(c2.Value.ToString(), out suffixIntValue);

                    if (baseTermValue != null && secondChunkIsInt)
                    {
                        resultValue = baseTermValue.GetIndex(suffixIntValue);
                    }
                }

                if (resultValue == null)
                    throw new KOSException("Can't Get Index " + GetFriendlyNameOfItem(c2.Value) + " from " +
                                           GetFriendlyNameOfItem(c1.Value));

                ReplaceChunkPairAt(ref chunks, i, new StatementChunk(resultValue, c2.Operator));
                i--;
            }

            return chunks.Count == 1 ? chunks[0].Value : null;
        }

        private object RecognizeConstant(string text)
        {
            text = text.Trim();

            // Numbers
            double testDouble;
            const NumberStyles styles =
                NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite | NumberStyles.AllowLeadingSign |
                NumberStyles.AllowDecimalPoint;
            if (double.TryParse(text, styles, CultureInfo.InvariantCulture, out testDouble)) return testDouble;

            // Booleans
            bool testBool;
            if (bool.TryParse(text, out testBool)) return testBool;

            // strings
            if (text.StartsWith("\""))
            {
                var end = Utils.FindEndOfstring(text, 1);
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
            var chunks = new List<StatementChunk>();

            for (var i = 0; i < input.SubTerms.Count; i += 2)
            {
                var termValue = GetValueOfTerm(input.SubTerms[i]);

                if (i + 1 < input.SubTerms.Count)
                {
                    var opTerm = input.SubTerms[i + 1];
                    if (opTerm.Type == Term.TermTypes.MATH_OPERATOR)
                    {
                        chunks.Add(new StatementChunk(termValue, opTerm.Text));
                    }
                    else
                    {
                        throw new KOSException("Expression error processing statement '" + input + "'", executionContext);
                    }
                }
                else
                {
                    chunks.Add(new StatementChunk(termValue, ""));
                }
            }

            #region Exponents

            for (var i = 0; i < chunks.Count - 1; i++)
            {
                var c1 = chunks[i];
                var c2 = chunks[i + 1];

                if (c1.Operator == "^")
                {
                    var resultValue = AttemptPow(c1.Value, c2.Value);
                    if (resultValue == null)
                        throw new KOSException(
                            "Can't use exponents with " + GetFriendlyNameOfItem(c1.Value) + " and " +
                            GetFriendlyNameOfItem(c2.Value), executionContext);

                    ReplaceChunkPairAt(ref chunks, i, new StatementChunk(resultValue, c2.Operator));
                    i--;
                }
            }

            #endregion

            #region Multiplication and Division

            for (var i = 0; i < chunks.Count - 1; i ++)
            {
                var c1 = chunks[i];
                var c2 = chunks[i + 1];

                if (c1.Operator == "*")
                {
                    var resultValue = AttemptMultiply(c1.Value, c2.Value);
                    if (resultValue == null)
                        throw new KOSException(
                            "Can't multiply " + GetFriendlyNameOfItem(c1.Value) + " by " +
                            GetFriendlyNameOfItem(c2.Value), executionContext);

                    ReplaceChunkPairAt(ref chunks, i, new StatementChunk(resultValue, c2.Operator));
                    i--;
                }
                else if (c1.Operator == "/")
                {
                    var resultValue = AttemptDivide(c1.Value, c2.Value);
                    if (resultValue == null)
                        throw new KOSException(
                            "Can't divide " + GetFriendlyNameOfItem(c1.Value) + " by " + GetFriendlyNameOfItem(c2.Value),
                            executionContext);

                    ReplaceChunkPairAt(ref chunks, i, new StatementChunk(resultValue, c2.Operator));
                    i--;
                }
            }

            #endregion

            #region Addition and Subtraction

            for (var i = 0; i < chunks.Count - 1; i++)
            {
                var c1 = chunks[i];
                var c2 = chunks[i + 1];

                if (c1.Operator == "+")
                {
                    var resultValue = AttemptAdd(c1.Value, c2.Value);
                    if (resultValue == null)
                        throw new KOSException(
                            "Can't add " + GetFriendlyNameOfItem(c1.Value) + " and " + GetFriendlyNameOfItem(c2.Value),
                            executionContext);

                    ReplaceChunkPairAt(ref chunks, i, new StatementChunk(resultValue, c2.Operator));
                    i--;
                }
                else if (c1.Operator == "-")
                {
                    var resultValue = AttemptSubtract(c1.Value, c2.Value);
                    if (resultValue == null)
                        throw new KOSException(
                            "Can't subtract " + GetFriendlyNameOfItem(c2.Value) + " from " +
                            GetFriendlyNameOfItem(c1.Value), executionContext);

                    ReplaceChunkPairAt(ref chunks, i, new StatementChunk(resultValue, c2.Operator));
                    i--;
                }
            }

            #endregion

            // If everything occurred correctly I should be left with one math chunk containing nothing but the resultant value
            return chunks.Count == 1 ? chunks[0].Value : null;
        }

        private object TryProcessFunction(Term input)
        {
            var p = input.SubTerms[1].SubTerms.ToArray();

            object output = TryCreateSuffixed(input.SubTerms[0].Text, p);
            if (output != null) return output;

            output = TryMathFunction(input.SubTerms[0].Text, p);
            if (output != null) return output;

            output = TryExternalFunction(input.SubTerms[0].Text, p);
            return output;
        }

        private object TryProcessStructure(Term input)
        {
            var baseTerm = input.SubTerms[0];
            var suffixTerm = input.SubTerms[1];

            if (suffixTerm.Type == Term.TermTypes.SUFFIX)
            {
                // First, see if this is just a variable with a comma in it (old-style structure)
                object output;
                if (Regex.Match(baseTerm.Text, "^[a-zA-Z]+$").Success)
                {
                    output = AttemptGetVariableValue(baseTerm.Text + ":" + suffixTerm.Text);
                    if (output != null) return output;
                }

                var baseTermValue = GetValueOfTerm(baseTerm);
                var value = baseTermValue as ISuffixed;
                if (value != null)
                {
                    output = value.GetSuffix(suffixTerm.Text.ToUpper());
                    if (output != null) return output;

                    throw new KOSException("Suffix '" + suffixTerm.Text + "' not found on object", executionContext);
                }
                throw new KOSException(
                    "Values of type " + GetFriendlyNameOfItem(baseTermValue) + " cannot have suffixes", executionContext);
            }

            return null;
        }

        private object TryProcessComparison(Term input)
        {
            var chunks = new List<StatementChunk>();

            for (var i = 0; i < input.SubTerms.Count; i += 2)
            {
                var termValue = GetValueOfTerm(input.SubTerms[i]);

                if (i + 1 < input.SubTerms.Count)
                {
                    var opTerm = input.SubTerms[i + 1];
                    if (opTerm.Type == Term.TermTypes.COMPARISON_OPERATOR)
                    {
                        chunks.Add(new StatementChunk(termValue, opTerm.Text));
                    }
                    else
                    {
                        throw new KOSException("Expression error processing comparison '" + input + "'",
                                               executionContext);
                    }
                }
                else
                {
                    chunks.Add(new StatementChunk(termValue, ""));
                }
            }

            for (var i = 0; i < chunks.Count - 1; i++)
            {
                var c1 = chunks[i];
                var c2 = chunks[i + 1];
                object resultValue = null;

                switch (c1.Operator)
                {
                    case "=":
                    case "==":
                        resultValue = AttemptEqual(c1.Value, c2.Value);
                        break;
                    case "!=":
                        resultValue = AttemptNotEqual(c1.Value, c2.Value);
                        break;
                    case "<":
                        resultValue = AttemptLessThan(c1.Value, c2.Value);
                        break;
                    case ">":
                        resultValue = AttemptGreaterThan(c1.Value, c2.Value);
                        break;
                    case "<=":
                        resultValue = AttemptLTE(c1.Value, c2.Value);
                        break;
                    case ">=":
                        resultValue = AttemptGTE(c1.Value, c2.Value);
                        break;
                }

                if (resultValue == null)
                    throw new KOSException(
                        "Can't compare " + GetFriendlyNameOfItem(c1.Value) + " to " + GetFriendlyNameOfItem(c2.Value) +
                        " using " + c1.Operator, executionContext);

                ReplaceChunkPairAt(ref chunks, i, new StatementChunk(resultValue, c2.Operator));
                i--;
            }

            return chunks.Count == 1 ? chunks[0].Value : null;
        }

        private object TryProcessBoolean(Term input)
        {
            var chunks = new List<StatementChunk>();

            for (var i = 0; i < input.SubTerms.Count; i += 2)
            {
                var termValue = GetValueOfTerm(input.SubTerms[i]);

                if (i + 1 < input.SubTerms.Count)
                {
                    var opTerm = input.SubTerms[i + 1];
                    if (opTerm.Type == Term.TermTypes.BOOLEAN_OPERATOR)
                    {
                        chunks.Add(new StatementChunk(termValue, opTerm.Text));
                    }
                    else
                    {
                        throw new KOSException("Expression error processing boolean operation '" + input + "'",
                                               executionContext);
                    }
                }
                else
                {
                    chunks.Add(new StatementChunk(termValue, ""));
                }
            }

            for (var i = 0; i < chunks.Count - 1; i++)
            {
                var c1 = chunks[i];
                var c2 = chunks[i + 1];
                object resultValue = null;

                switch (c1.Operator)
                {
                    case "AND":
                        resultValue = AttemptAnd(c1.Value, c2.Value);
                        break;
                    case "OR":
                        resultValue = AttemptOr(c1.Value, c2.Value);
                        break;
                }

                if (resultValue == null)
                    throw new KOSException(
                        "Can't compare " + GetFriendlyNameOfItem(c1.Value) + " to " + GetFriendlyNameOfItem(c2.Value) +
                        " using " + c1.Operator, executionContext);

                ReplaceChunkPairAt(ref chunks, i, new StatementChunk(resultValue, c2.Operator));
                i--;
            }

            return chunks.Count == 1 ? chunks[0].Value : null;
        }

        private object TryExternalFunction(string name, IList<Term> p)
        {
            foreach (
                var f in
                    executionContext.ExternalFunctions.Where(f => f.Name.ToUpper() == name.ToUpper()))
            {
                if (p.Count() != f.ParameterCount)
                    throw new Exception("Wrong number of arguments, expected " + f.ParameterCount);

                var sp = new string[f.ParameterCount];
                for (var i = 0; i < f.ParameterCount; i++)
                {
                    sp[i] = GetValueOfTerm(p[i]).ToString();
                }

                var output = executionContext.CallExternalFunction(f.Name, sp);
                if (output != null) return output;
            }

            return null;
        }

        private object TryMathFunction(string name, Term[] p)
        {
            name = name.ToUpper();

            switch (name)
            {
                case "SIN":
                    {
                        var dp = GetParamsAsT<double>(p, 1);
                        return Math.Sin(dp[0]*(Math.PI/180));
                    }
                case "COS":
                    {
                        var dp = GetParamsAsT<double>(p, 1);
                        return Math.Cos(dp[0]*(Math.PI/180));
                    }
                case "TAN":
                    {
                        var dp = GetParamsAsT<double>(p, 1);
                        return Math.Tan(dp[0]*(Math.PI/180));
                    }
                case "ARCSIN":
                    {
                        var dp = GetParamsAsT<double>(p, 1);
                        return Math.Asin(dp[0])*(180/Math.PI);
                    }
                case "ARCCOS":
                    {
                        var dp = GetParamsAsT<double>(p, 1);
                        return Math.Acos(dp[0])*(180/Math.PI);
                    }
                case "ARCTAN":
                    {
                        var dp = GetParamsAsT<double>(p, 1);
                        return Math.Atan(dp[0])*(180/Math.PI);
                    }
                case "ARCTAN2":
                    {
                        var dp = GetParamsAsT<double>(p, 2);
                        return Math.Atan2(dp[0], dp[1])*(180/Math.PI);
                    }
                case "ABS":
                    {
                        var dp = GetParamsAsT<double>(p, 1);
                        return Math.Abs(dp[0]);
                    }
                case "MOD":
                    {
                        var dp = GetParamsAsT<double>(p, 2);
                        return dp[0]%dp[1];
                    }
                case "FLOOR":
                    {
                        var dp = GetParamsAsT<double>(p, 1);
                        return Math.Floor(dp[0]);
                    }
                case "CEILING":
                    {
                        var dp = GetParamsAsT<double>(p, 1);
                        return Math.Ceiling(dp[0]);
                    }
                case "SQRT":
                    {
                        var dp = GetParamsAsT<double>(p, 1);
                        return Math.Sqrt(dp[0]);
                    }
                case "LN":
                    {
                        var dp = GetParamsAsT<double>(p, 1);
                        return Math.Log(dp[0]);
                    }
                case "LOG10":
                    {
                        var dp = GetParamsAsT<double>(p, 1);
                        return Math.Log10(dp[0]);
                    }
                case "MIN":
                    {
                        var dp = GetParamsAsT<double>(p, 2);
                        return Math.Min(dp[0], dp[1]);
                    }
                case "MAX":
                    {
                        var dp = GetParamsAsT<double>(p, 2);
                        return Math.Max(dp[0], dp[1]);
                    }
                case "RANDOM":
                    return random.NextDouble();
                case "VCRS":
                case "VECTORCROSSPRODUCT":
                    {
                        var dp = GetParamsAsT<Vector>(p, 2);
                        return new Vector(Vector3d.Cross(dp[0].ToVector3D(),dp[1].ToVector3D()));  
                    }
                case "VDOT":
                case "VECTORDOTPRODUCT":
                    {
                        var dp = GetParamsAsT<Vector>(p, 2);
                        return Vector3d.Dot(dp[0].ToVector3D(), dp[1].ToVector3D()); 
                    }
                case "VXCL":
                case "VECTOREXCLUDE":
                    {
                        var dp = GetParamsAsT<Vector>(p, 2);
                        return new Vector(Vector3d.Exclude(dp[0].ToVector3D(), dp[1].ToVector3D())); 
                    }
                case "VANG":
                case "VECTORANGLE":
                    {
                        var dp = GetParamsAsT<Vector>(p, 2);
                        return Vector3d.Angle(dp[0].ToVector3D(), dp[1].ToVector3D()); 
                    }


            }

            if (name == "ROUND")
            {
                if (p.Count() == 1)
                {
                    var dp = GetParamsAsT<double>(p, 1);
                    return Math.Round(dp[0]);
                }
                if (p.Count() == 2)
                {
                    var dp = GetParamsAsT<double>(p, 2);
                    return Math.Round(dp[0], (int) dp[1]);
                }
            }

            return null;
        }

        private ISuffixed TryCreateSuffixed(string name, Term[] p)
        {
            name = name.ToUpper();

            switch (name)
            {
                case "NODE":
                    {
                        var dp = GetParamsAsT<double>(p, 4);
                        return new Node(dp[0], dp[1], dp[2], dp[3]);
                    }
                case "V":
                    {
                        var dp = GetParamsAsT<double>(p, 3);
                        return new Vector(dp[0], dp[1], dp[2]);
                    }
                case "R":
                    {
                        var dp = GetParamsAsT<double>(p, 3);
                        return new Direction(new Vector3d(dp[0], dp[1], dp[2]), true);
                    }
                case "Q":
                    {
                        var dp = GetParamsAsT<double>(p, 4);
                        return new Direction(new Quaternion((float) dp[0], (float) dp[1], (float) dp[2], (float) dp[3]));
                    }
                case "T":
                    {
                        var dp = GetParamsAsT<double>(p, 1);
                        return new TimeSpan(dp[0]);
                    }
                case "LATLNG":
                    {
                        var dp = GetParamsAsT<double>(p, 2);
                        return new GeoCoordinates(executionContext.Vessel, dp[0], dp[1]);
                    }
                case "VESSEL":
                    {
                        var sp = GetParamsAsT<string>(p, 1);
                        return new VesselTarget(VesselUtils.GetVesselByName(sp[0], executionContext.Vessel),
                                                executionContext);
                    }
                case "BODY":
                    {
                        var sp = GetParamsAsT<string>(p, 1);
                        return new BodyTarget(sp[0], executionContext.Vessel);
                    }
                case "BODYATMOSPHERE":
                    {
                        var sp = GetParamsAsT<string>(p, 1);
                        return new BodyAtmosphere(VesselUtils.GetBodyByName(sp[0]));
                    }
                case "LIST":
                    return new ListValue();
                case "CONSTANT":
                    return new ConstantValue();
                case "HEADING":
                    {
                        var pCount = p.Count();
                        if (pCount < 2 || pCount > 3)
                            throw new KOSException("Wrong number of arguments supplied, expected 2 or 3",
                                                   executionContext);

                        var dp = GetParamsAsT<double>(p, pCount);
                        var q = Quaternion.LookRotation(VesselUtils.GetNorthVector(executionContext.Vessel),
                                                        executionContext.Vessel.upAxis);
                        q *=
                            Quaternion.Euler(new Vector3((float) -dp[0], (float) dp[1],
                                                         (float) (dp.Count() > 2 ? dp[2] : 0)));

                        return new Direction(q);
                    }
            }

            return null;
        }

        private T GetParamAsT<T>(Term input)
        {
            var value = GetValueOfTerm(input);
            if (value is T) return (T) value;

            if (typeof (T) == typeof (double))
                throw new KOSException("Supplied parameter '" + input.Text + "' is not a number", executionContext);
            if (typeof (T) == typeof (string))
                throw new KOSException("Supplied parameter '" + input.Text + "' is not a string", executionContext);
            if (typeof (T) == typeof (bool))
                throw new KOSException("Supplied parameter '" + input.Text + "' is not a boolean", executionContext);

            throw new KOSException("Supplied parameter '" + input.Text + "' is not of the correct type",
                                   executionContext);
        }

        private T[] GetParamsAsT<T>(IList<Term> input, int size)
        {
            if (input.Count() != size)
            {
                throw new KOSException("Wrong number of arguments supplied, expected " + size, executionContext);
            }

            var retVal = new T[size];

            for (var i = 0; i < size; i++)
            {
                retVal[i] = GetParamAsT<T>(input[i]);
            }

            return retVal;
        }

        public static string GetFriendlyNameOfItem(object input)
        {
            if (input is string) return "string";
            if (input is double) return "number";
            if (input is float) return "number";
            if (input is int) return "number";
            if (input is VesselTarget) return "vessel";
            if (input is ISuffixed) return input.GetType().ToString().Replace("kOS.", "").ToLower();

            return "";
        }

        private object AttemptMultiply(object val1, object val2)
        {
            if ((val1 is double || val1 is float || val1 is int) && (val2 is double || val2 is float || val2 is int))
            {
                return (double) val1*(double) val2;
            }
            if (val1 is IOperatable)
            {
                return ((IOperatable) val1).TryOperation("*", val2, false);
            }
            if (val2 is IOperatable)
            {
                return ((IOperatable) val2).TryOperation("*", val1, true);
            }

            return null;
        }

        private object AttemptDivide(object val1, object val2)
        {
            if ((val1 is double || val1 is float || val1 is int) && (val2 is double || val2 is float || val2 is int))
            {
                return (double) val1/(double) val2;
            }
            if (val1 is IOperatable)
            {
                return ((IOperatable) val1).TryOperation("/", val2, false);
            }
            if (val2 is IOperatable)
            {
                return ((IOperatable) val2).TryOperation("/", val1, true);
            }

            return null;
        }

        private object AttemptAdd(object val1, object val2)
        {
            if (val1 is string || val2 is string)
            {
                return val1 + val2.ToString();
            }

            if ((val1 is double || val1 is float || val1 is int) && (val2 is double || val2 is float || val2 is int))
            {
                return (double) val1 + (double) val2;
            }
            if (val1 is IOperatable)
            {
                return ((IOperatable) val1).TryOperation("+", val2, false);
            }
            if (val2 is IOperatable)
            {
                return ((IOperatable) val2).TryOperation("+", val1, true);
            }

            return null;
        }

        private object AttemptSubtract(object val1, object val2)
        {
            if ((val1 is double || val1 is float || val1 is int) && (val2 is double || val2 is float || val2 is int))
            {
                return (double) val1 - (double) val2;
            }
            if (val1 is IOperatable)
            {
                return ((IOperatable) val1).TryOperation("-", val2, false);
            }
            if (val2 is IOperatable)
            {
                return ((IOperatable) val2).TryOperation("-", val1, true);
            }

            return null;
        }

        private object AttemptPow(object val1, object val2)
        {
            if ((val1 is double || val1 is float || val1 is int) && (val2 is double || val2 is float || val2 is int))
            {
                return Math.Pow((double) val1, (double) val2);
            }

            return null;
        }

        private object AttemptGetVariableValue(string varName)
        {
            executionContext.UpdateLock(varName);
            var v = executionContext.FindVariable(varName);

            return v == null ? null : (v.Value is float ? (double) ((float) v.Value) : v.Value);
        }

        private object AttemptEqual(object val1, object val2)
        {
            if ((val1 is double || val1 is float || val1 is int) && (val2 is double || val2 is float || val2 is int))
            {
                return Math.Abs((double) val1 - (double) val2) < 0.0001;
            }
            if (val1 is string || val2 is string)
            {
                return val1.ToString() == val2.ToString();
            }
            if (val1 is bool && val2 is bool)
            {
                return ((bool) val1) == ((bool) val2);
            }
            if (val1 is IOperatable)
            {
                return ((IOperatable) val1).TryOperation("=", val2, false);
            }

            return null;
        }

        private object AttemptNotEqual(object val1, object val2)
        {
            if ((val1 is double || val1 is float || val1 is int) && (val2 is double || val2 is float || val2 is int))
            {
                return Math.Abs((double) val1 - (double) val2) > 0.0001;
            }
            if (val1 is string || val2 is string)
            {
                return val1.ToString() != val2.ToString();
            }
            if (val1 is bool && val2 is bool)
            {
                return ((bool) val1) != ((bool) val2);
            }
            if (val1 is IOperatable)
            {
                return ((IOperatable) val1).TryOperation("!=", val2, false);
            }

            return null;
        }

        private object AttemptGreaterThan(object val1, object val2)
        {
            if ((val1 is double || val1 is float || val1 is int) && (val2 is double || val2 is float || val2 is int))
            {
                return (double) val1 > (double) val2;
            }
            if (val1 is IOperatable)
            {
                return ((IOperatable) val1).TryOperation(">", val2, false);
            }
            if (val2 is IOperatable)
            {
                return ((IOperatable) val2).TryOperation(">", val1, true);
            }

            return null;
        }

        private object AttemptLessThan(object val1, object val2)
        {
            if ((val1 is double || val1 is float || val1 is int) && (val2 is double || val2 is float || val2 is int))
            {
                return (double) val1 < (double) val2;
            }
            if (val1 is IOperatable)
            {
                return ((IOperatable) val1).TryOperation("<", val2, false);
            }
            if (val2 is IOperatable)
            {
                return ((IOperatable) val2).TryOperation("<", val1, true);
            }

            return null;
        }

        private object AttemptGTE(object val1, object val2)
        {
            if ((val1 is double || val1 is float || val1 is int) && (val2 is double || val2 is float || val2 is int))
            {
                return (double) val1 >= (double) val2;
            }
            if (val1 is IOperatable)
            {
                return ((IOperatable) val1).TryOperation(">=", val2, false);
            }
            if (val2 is IOperatable)
            {
                return ((IOperatable) val2).TryOperation(">=", val1, true);
            }

            return null;
        }

        private object AttemptLTE(object val1, object val2)
        {
            if ((val1 is double || val1 is float || val1 is int) && (val2 is double || val2 is float || val2 is int))
            {
                return (double) val1 <= (double) val2;
            }
            if (val1 is IOperatable)
            {
                return ((IOperatable) val1).TryOperation("<=", val2, false);
            }
            if (val2 is IOperatable)
            {
                return ((IOperatable) val2).TryOperation("<=", val1, true);
            }

            return null;
        }

        private bool ObjectToBool(object input, out bool result)
        {
            if (input is bool)
            {
                result = (bool) input;
                return true;
            }
            if (input is double)
            {
                result = (double) input > 0;
                return true;
            }
            var s = input as string;
            if (s != null)
            {
                if (bool.TryParse(s, out result)) return true;
                double dblVal;
                if (double.TryParse(s, out dblVal))
                {
                    result = dblVal > 0;
                    return true;
                }
            }

            result = false;
            return false;
        }

        private object AttemptAnd(object val1, object val2)
        {
            bool v1, v2;

            if (!ObjectToBool(val1, out v1)) return null;
            if (!ObjectToBool(val2, out v2)) return null;

            return (v1 && v2);
        }

        private object AttemptOr(object val1, object val2)
        {
            bool v1, v2;

            if (!ObjectToBool(val1, out v1)) return null;
            if (!ObjectToBool(val2, out v2)) return null;

            return (v1 || v2);
        }

        public bool IsNull()
        {
            var value = GetValue();

            return value == null;
        }

        public bool IsTrue()
        {
            var value = GetValue();

            if (value == null) return false;
            if (value is bool) return (bool) value;
            if (value is double) return (double) value > 0;
            var strValue = value as string;
            if (strValue != null)
            {
                bool boolVal;
                if (bool.TryParse(strValue, out boolVal)) return boolVal;

                double numberVal;
                if (double.TryParse(strValue, out numberVal)) return numberVal > 0;

                return strValue.Trim() != "";
            }
            return value is ISuffixed;
        }

        public double Double()
        {
            var value = GetValue();

            if (value == null) return 0;
            if (value is bool) return (bool) value ? 1 : 0;
            if (value is double) return (double) value;
            var strValue = value as string;
            if (strValue != null)
            {
                double numberVal;
                return double.TryParse(strValue, out numberVal) ? numberVal : 0;
            }

            return 0;
        }

        public float Float()
        {
            return (float) Double();
        }

        public override string ToString()
        {
            return GetValue().ToString();
        }

        private struct StatementChunk
        {
            public readonly string Operator;
            public readonly object Value;

            public StatementChunk(object value, string opr)
            {
                Value = value;
                Operator = opr;
            }
        }
    }
}