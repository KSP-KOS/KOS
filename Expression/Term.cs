using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using kOS.Utilities;

namespace kOS.Expression
{
    public class Term
    {
        public enum TermTypes
        {
            REGULAR,
            FINAL,
            FUNCTION,
            PARAMETER_LIST,
            COMPARISON,
            BOOLEAN,
            SUFFIX,
            STRUCTURE,
            MATH_OPERATOR,
            COMPARISON_OPERATOR,
            BOOLEAN_OPERATOR,
            INDEX,
            INDEX_OPERATOR
        }

        private static List<string> comparisonSymbols;
        private static List<string> booleanSymbols;
        private static List<string> parameterSeperatorSymbols;
        private static List<string> listaccessSymbols;
        private static readonly List<string> mathSymbols;
        private static readonly List<string> allSymbols;
        private static readonly List<string> delimeterSymbols;
        private static readonly List<string> subaccessSymbols;
        public List<Term> SubTerms;
        public bool TermsAreParameters;
        public string Text;
        public TermTypes Type;

        static Term()
        {
            mathSymbols = new List<string>();
            mathSymbols.AddRange(new[] {"+", "-", "*", "/", "^"});

            comparisonSymbols = new List<string>();
            comparisonSymbols.AddRange(new[] {"<=", ">=", "!=", "==", "=", "<", ">"});

            booleanSymbols = new List<string>();
            booleanSymbols.AddRange(new[] {" AND ", " OR "});

            parameterSeperatorSymbols = new List<string>();
            parameterSeperatorSymbols.AddRange(new[] {","});

            subaccessSymbols = new List<string>();
            subaccessSymbols.AddRange(new[] {":"});

            listaccessSymbols = new List<string>();
            listaccessSymbols.AddRange(new[] {"#"});

            delimeterSymbols = new List<string>();
            delimeterSymbols.AddRange(new[] {"(", ")", "\""});

            allSymbols = new List<string>();
            allSymbols.AddRange(mathSymbols.ToArray());
            allSymbols.AddRange(comparisonSymbols.ToArray());
            allSymbols.AddRange(booleanSymbols.ToArray());
            allSymbols.AddRange(parameterSeperatorSymbols.ToArray());
            allSymbols.AddRange(subaccessSymbols.ToArray());
            allSymbols.AddRange(listaccessSymbols.ToArray());
            allSymbols.AddRange(delimeterSymbols.ToArray());
        }

        public Term(string input) : this(input, TermTypes.REGULAR, true)
        {
        }

        public Term(string input, TermTypes type) : this(input, type, true)
        {
        }

        public Term(string input, TermTypes type, bool autoTrim)
        {
            TermsAreParameters = false;
            Text = autoTrim ? input.Trim() : input;
            Type = type;
            SubTerms = new List<Term>();

            if (Type != TermTypes.SUFFIX && type != TermTypes.BOOLEAN_OPERATOR) ProcessSymbols();
        }

        public void CopyFrom(ref Term from)
        {
            Text = from.Text;
            SubTerms = from.SubTerms;
            Type = from.Type;
        }

        public Term Merge(params Term[] terms)
        {
            var output = new Term("");

            foreach (var t in terms)
            {
                switch (t.Type)
                {
                    case TermTypes.PARAMETER_LIST:
                        output.Text += "(" + t.Text + ")";
                        break;
                    case TermTypes.SUFFIX:
                        output.Text += ":" + t.Text;
                        break;
                    case TermTypes.INDEX:
                        output.Text += "#" + t.Text;
                        break;
                    default:
                        output.Text += t.Text;
                        break;
                }

                output.SubTerms.Add(t);
            }

            return output;
        }

        public string Demo()
        {
            return Demo(0);
        }

        public string Demo(int tabIndent)
        {
            var retstring = new string(' ', tabIndent*4);

            switch (Type)
            {
                case TermTypes.FUNCTION:
                    retstring += "FUNCTION->";
                    break;
                case TermTypes.PARAMETER_LIST:
                    retstring += "PARAMS->";
                    break;
                case TermTypes.COMPARISON:
                    retstring += "COMPARISON->";
                    break;
                case TermTypes.BOOLEAN:
                    retstring += "BOOLEAN->";
                    break;
                case TermTypes.STRUCTURE:
                    retstring += "STRUCTURE->";
                    break;
                case TermTypes.MATH_OPERATOR:
                    retstring += "MATH ";
                    break;
                case TermTypes.COMPARISON_OPERATOR:
                    retstring += "COMP ";
                    break;
                case TermTypes.BOOLEAN_OPERATOR:
                    retstring += "BOOL ";
                    break;
                case TermTypes.SUFFIX:
                    retstring += ":";
                    break;
            }

            retstring += Text + Environment.NewLine;

            foreach (var t in SubTerms)
            {
                retstring += t.Demo(tabIndent + 1);
            }

            return retstring;
        }

        private void ProcessSymbols()
        {
            // Is the input empty?
            if (string.IsNullOrEmpty(Text)) return;

            // HEADING.. BY is now deprecated in favor of HEADING(x,y), but here it is if you're using it still
            Text = Regex.Replace(Text, "HEADING ([ :@A-Za-z0-9\\.\\-\\+\\*/]+) BY ([ :@A-Za-z0-9\\.\\-\\+\\*/]+)",
                                 "HEADING($2,$1)", RegexOptions.IgnoreCase);

            //enables scientific notation eg 6.6E-11 -> 6.6*10^-11
            Text = Regex.Replace(Text, "(\\d)E([-+]{1}[0-9]+)", "$1*10^$2");

            // Resource tags are now deprecated in favor of SHIP:ResourceName
            Text = Regex.Replace(Text, "(\\s|^)<([a-zA-Z]+)>(\\s|$)", " SHIP:$2 ", RegexOptions.IgnoreCase);

            // Is this JUST a matched symbol?                
            var s = MatchAt(ref Text, 0, allSymbols);
            if (s != null && Text.Length == s.Length)
            {
                if (mathSymbols.Contains(s)) Type = TermTypes.MATH_OPERATOR;
                else if (comparisonSymbols.Contains(s)) Type = TermTypes.COMPARISON_OPERATOR;
                else if (booleanSymbols.Contains(s)) Type = TermTypes.BOOLEAN_OPERATOR;

                return;
            }

            SubTerms = new List<Term>();

            // If this is a parameter list, grab the parameters
            if (Type == TermTypes.PARAMETER_LIST)
            {
                var parameterList = ParseParameters(Text);
                if (parameterList != null)
                {
                    foreach (var param in parameterList)
                    {
                        SubTerms.Add(new Term(param));
                    }
                }

                return;
            }

            // Does this thing contain a boolean operation?
            var booleanElements = SplitByListIgnoreBracket(Text, ref booleanSymbols);
            if (booleanElements != null)
            {
                Type = TermTypes.BOOLEAN;

                foreach (var element in booleanElements)
                {
                    if (booleanSymbols.Contains(element))
                    {
                        SubTerms.Add(new Term(element, TermTypes.BOOLEAN_OPERATOR));
                    }
                    else
                    {
                        SubTerms.Add(new Term(element));
                    }
                }

                return;
            }

            // Does this thing contain a comparison?
            var comparisonElements = SplitByListIgnoreBracket(Text, ref comparisonSymbols);
            if (comparisonElements != null)
            {
                Type = TermTypes.COMPARISON;

                foreach (var element in comparisonElements)
                {
                    SubTerms.Add(new Term(element));
                }

                return;
            }

            // Does this thing contain an Index?
            var listElements = SplitByListIgnoreBracket(Text, ref listaccessSymbols);
            if (listElements != null)
            {
                Type = TermTypes.INDEX;

                foreach (var element in listElements)
                {
                    if (listaccessSymbols.Contains(element))
                    {
                        SubTerms.Add(new Term(element, TermTypes.INDEX_OPERATOR));
                    }
                    else
                    {
                        SubTerms.Add(new Term(element));
                    }
                }

                return;
            }


            // Parse this as a normal term
            var buffer = "";
            for (var i = 0; i < Text.Length; i++)
            {
                s = MatchAt(ref Text, i, allSymbols);

                if (s == null)
                {
                    buffer += Text[i];
                }
                else if (s == "(")
                {
                    var startI = i;
                    Utils.Balance(ref Text, ref i, ')');

                    if (buffer.Trim() != "")
                    {
                        var functionName = buffer.Trim();
                        buffer = "";

                        var bracketTerm = new Term(Text.Substring(startI + 1, i - startI - 1), TermTypes.PARAMETER_LIST);
                        var functionTerm = Merge(new Term(functionName), bracketTerm);
                        functionTerm.Type = TermTypes.FUNCTION;

                        SubTerms.Add(functionTerm);
                    }
                    else
                    {
                        SubTerms.Add(new Term(Text.Substring(startI + 1, i - startI - 1)));
                    }
                }
                else if (s == "\"")
                {
                    var startI = i;
                    i = Utils.FindEndOfstring(Text, i + 1);
                    buffer += Text.Substring(startI, i - startI + 1);
                }
                else if (s == ":")
                {
                    var end = FindEndOfSuffix(Text, i + 1);
                    var suffixName = Text.Substring(i + 1, end - i);
                    i += end - i;

                    if (buffer.Trim() != "")
                    {
                        SubTerms.Add(new Term(buffer.Trim()));
                        buffer = "";
                    }

                    if (SubTerms.Count > 0)
                    {
                        var last = SubTerms.Last();
                        SubTerms.Remove(last);

                        var structureTerm = Merge(last, new Term(suffixName, TermTypes.SUFFIX));
                        structureTerm.Type = TermTypes.STRUCTURE;
                        SubTerms.Add(structureTerm);
                    }
                }
                else if (s == "-")
                {
                    if (buffer.Trim() != "" ||
                        (SubTerms.Count > 0 && SubTerms.Last().Type != TermTypes.MATH_OPERATOR
                         && SubTerms.Last().Type != TermTypes.COMPARISON_OPERATOR))
                    {
                        // Not a sign, treat as operator
                        if (buffer.Trim() != "") SubTerms.Add(new Term(buffer.Trim()));
                        SubTerms.Add(new Term(s));

                        buffer = "";
                        i += s.Length - 1;
                    }
                    else
                    {
                        buffer += Text[i];
                    }
                }
                else
                {
                    if (buffer.Trim() != "") SubTerms.Add(new Term(buffer.Trim()));
                    SubTerms.Add(new Term(s));

                    buffer = "";
                    i += s.Length - 1;
                }
            }

            // If there's only one term, we're done!
            if (SubTerms.Count == 0)
            {
                Type = TermTypes.FINAL;
                return;
            }

            if (buffer.Trim() != "") SubTerms.Add(new Term(buffer));

            // If I end up with exactly one subTerm, then I AM that subterm. Exception: If I already have a special type
            if (SubTerms.Count == 1 && Type == TermTypes.REGULAR)
            {
                var child = SubTerms[0];
                SubTerms.Clear();

                CopyFrom(ref child);
            }
        }

        private int FindEndOfSuffix(string input, int start)
        {
            for (var i = start; i < input.Length; i++)
            {
                var match = Regex.Match(input.Substring(i, 1), "[a-zA-Z0-9_]");
                if (!match.Success)
                {
                    return i == start ? 0 : i - 1;
                }
            }

            return input.Length - 1;
        }

        private IEnumerable<string> SplitByListIgnoreBracket(string input, ref List<string> operators)
        {
            return SplitByListIgnoreBracket(input, ref operators, false);
        }

        private IEnumerable<string> SplitByListIgnoreBracket(string input, ref List<string> operators,
                                                             bool returnIfOneElement)
        {
            var buffer = "";
            string s;
            var retList = new List<string>();

            for (var i = 0; i < input.Length; i++)
            {
                if (input[i] == '(')
                {
                    var startI = i;
                    Utils.Balance(ref Text, ref i, ')');
                    buffer += Text.Substring(startI, i - startI + 1);
                }
                else if (input[i] == '"')
                {
                    var startI = i;
                    i = Utils.FindEndOfstring(Text, i + 1);
                    buffer += Text.Substring(startI, i - startI + 1);
                }
                else
                {
                    s = MatchAt(ref input, i, operators);

                    if (s != null)
                    {
                        // TODO: If buffer empty, syntax error

                        retList.Add(buffer);
                        retList.Add(s);
                        buffer = "";

                        i += s.Length - 1;
                    }
                    else
                    {
                        buffer += input[i];
                    }
                }
            }

            if (buffer.Trim() != "") retList.Add(buffer);

            if (returnIfOneElement)
            {
                return retList;
            }
            else
            {
                return retList.Count > 1 ? retList : null;
            }
        }

        private List<string> ParseParameters(string input)
        {
            var splitList = SplitByListIgnoreBracket(input, ref parameterSeperatorSymbols, true);

            if (splitList != null)
            {
                var retList = new List<string>();

                foreach (var listItem in splitList)
                {
                    if (listItem != ",") retList.Add(listItem);
                }

                return retList;
            }

            return null;
        }

        private string MatchAt(ref string input, int i, IEnumerable<string> matchables)
        {
            foreach (var s in matchables)
            {
                if (s.StartsWith(" "))
                {
                    var r = new Regex("^" + s.Replace(" ", "\\s"), RegexOptions.IgnoreCase);
                    var m = r.Match(input.Substring(i));

                    if (m.Success)
                    {
                        return s;
                    }
                }
                else if (input.Length - i >= s.Length && input.Substring(i, s.Length) == s)
                {
                    return s;
                }
            }

            return null;
        }
    }
}