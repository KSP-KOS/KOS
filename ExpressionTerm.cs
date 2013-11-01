using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace kOS
{
    public class Term
    {
        public String Text;
        public List<Term> SubTerms;
        public bool TermsAreParameters;

        public enum TermTypes { REGULAR, FINAL, FUNCTION, PARAMETER_LIST, COMPARISON, BOOLEAN, SUFFIX, STRUCTURE, MATH_OPERATOR, COMPARISON_OPERATOR, BOOLEAN_OPERATOR }
        public TermTypes Type;

        private static List<String> mathSymbols;
        private static List<String> comparisonSymbols;
        private static List<String> booleanSymbols;
        private static List<String> allSymbols;
        private static List<String> parameterSeperatorSymbols;
        private static List<String> subaccessSymbols;
        private static List<String> delimeterSymbols;

        static Term()
        {
            mathSymbols = new List<string>();
            mathSymbols.AddRange(new string[] { "+", "-", "*", "/", "^" });

            comparisonSymbols = new List<string>();
            comparisonSymbols.AddRange(new string[] { "<=", ">=", "!=", "==", "=", "<", ">" });

            booleanSymbols = new List<string>();
            booleanSymbols.AddRange(new string[] { " AND ", " OR " });

            parameterSeperatorSymbols = new List<string>();
            parameterSeperatorSymbols.AddRange(new string[] { "," });

            subaccessSymbols = new List<string>();
            subaccessSymbols.AddRange(new string[] { ":" });

            delimeterSymbols = new List<string>();
            delimeterSymbols.AddRange(new string[] { "(", ")", "\"" });

            allSymbols = new List<string>();
            allSymbols.AddRange(mathSymbols.ToArray());
            allSymbols.AddRange(comparisonSymbols.ToArray());
            allSymbols.AddRange(booleanSymbols.ToArray());
            allSymbols.AddRange(parameterSeperatorSymbols.ToArray());
            allSymbols.AddRange(subaccessSymbols.ToArray());
            allSymbols.AddRange(delimeterSymbols.ToArray());
        }

        public Term(String input) : this (input, TermTypes.REGULAR, true) {}
        
        public Term(String input, TermTypes type) : this(input, type, true) { }

        public Term(String input, TermTypes type, bool autoTrim)
        {
            TermsAreParameters = false;
            Text = autoTrim ? input.Trim() : input;
            Type = type;
            SubTerms = new List<Term>();

            if (Type != TermTypes.SUFFIX && type != TermTypes.BOOLEAN_OPERATOR) processSymbols();
        }

        public void CopyFrom(ref Term from)
        {
            this.Text = from.Text;
            this.SubTerms = from.SubTerms;
            this.Type = from.Type;
        }

        public Term Merge(params Term[] terms)
        {
            Term output = new Term("");

            foreach (Term t in terms)
            {
                output.Text += t.Type == TermTypes.PARAMETER_LIST ? "(" + t.Text + ")" :
                                t.Type == TermTypes.SUFFIX ? ":" + t.Text :
                                t.Text;

                output.SubTerms.Add(t);
            }

            return output;
        }

        public String Demo()
        {
            return Demo(0);
        }

        public String Demo(int tabIndent)
        {
            String retString = new String(' ', tabIndent * 4);

            if (Type == TermTypes.FUNCTION) retString += "FUNCTION->";
            else if (Type == TermTypes.PARAMETER_LIST) retString += "PARAMS->";
            else if (Type == TermTypes.COMPARISON) retString += "COMPARISON->";
            else if (Type == TermTypes.BOOLEAN) retString += "BOOLEAN->";
            else if (Type == TermTypes.STRUCTURE) retString += "STRUCTURE->";
            else if (Type == TermTypes.MATH_OPERATOR) retString += "MATH ";
            else if (Type == TermTypes.COMPARISON_OPERATOR) retString += "COMP ";
            else if (Type == TermTypes.BOOLEAN_OPERATOR) retString += "BOOL ";
            else if (Type == TermTypes.SUFFIX) retString += ":";

            retString += Text + Environment.NewLine;

            foreach (Term t in SubTerms)
            {
                retString += t.Demo(tabIndent + 1);
            }

            return retString;
        }

        private void processSymbols()
        {
            // Is the input empty?
            if (String.IsNullOrEmpty(Text)) return;
            
            // HEADING.. BY is now deprecated in favor of HEADING(x,y), but here it is if you're using it still
            Text = Regex.Replace(Text, "HEADING ([ :@A-Za-z0-9\\.\\-\\+\\*/]+) BY ([ :@A-Za-z0-9\\.\\-\\+\\*/]+)", "HEADING($2,$1)", RegexOptions.IgnoreCase);

            // Is this JUST a matched symbol?                
            String s = matchAt(ref Text, 0, ref allSymbols);
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
                var parameterList = parseParameters(Text);
                if (parameterList != null)
                {
                    foreach (String param in parameterList)
                    {
                        SubTerms.Add(new Term(param));
                    }
                }

                return;
            }

            // Does this thing contain a boolean operation?
            var booleanElements = splitByListIgnoreBracket(Text, ref booleanSymbols);
            if (booleanElements != null)
            {
                Type = TermTypes.BOOLEAN;

                foreach (String element in booleanElements)
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
            var comparisonElements = splitByListIgnoreBracket(Text, ref comparisonSymbols);
            if (comparisonElements != null)
            {
                Type = TermTypes.COMPARISON;

                foreach (String element in comparisonElements)
                {
                    SubTerms.Add(new Term(element));
                }

                return;
            }

            // Parse this as a normal term
            String buffer = "";
            for (int i = 0; i < Text.Length; i++)
            {
                s = matchAt(ref Text, i, ref allSymbols);

                if (s == null)
                {
                    buffer += Text[i];
                }
                else if (s == "(")
                {
                    int startI = i;
                    Utils.Balance(ref Text, ref i, ')');
                    
                    if (buffer.Trim() != "")
                    {
                        string functionName = buffer.Trim();
                        buffer = "";

                        Term bracketTerm = new Term(Text.Substring(startI + 1, i - startI - 1), TermTypes.PARAMETER_LIST);
                        Term functionTerm = Merge(new Term(functionName), bracketTerm);
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
                    int startI = i;
                    i = Utils.FindEndOfString(Text, i + 1);
                    buffer += Text.Substring(startI, i - startI + 1); 
                }
                else if (s == ":")
                {
                    int end = findEndOfSuffix(Text, i + 1);
                    String suffixName = Text.Substring(i + 1, end - i);
                    i += end - i;

                    if (buffer.Trim() != "")
                    {
                        SubTerms.Add(new Term(buffer.Trim()));
                        buffer = "";
                    }

                    if (SubTerms.Count > 0)
                    {
                        Term last = SubTerms.Last();
                        SubTerms.Remove(last);

                        Term structureTerm = Merge(last, new Term(suffixName, TermTypes.SUFFIX));
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
            if (SubTerms.Count == 1 && this.Type == TermTypes.REGULAR)
            {
                Term child = SubTerms[0];
                SubTerms.Clear();

                CopyFrom(ref child);
            }
        }

        private int findEndOfSuffix(String input, int start)
        {
            for (int i = start; i < input.Length; i++)
            {
                var match = Regex.Match(input.Substring(i, 1), "[a-zA-Z0-9_]");
                if (!match.Success)
                {
                    return i == start ? 0 : i - 1;
                }
            }

            return input.Length - 1;
        }

        private List<String> splitByListIgnoreBracket(String input, ref List<String> operators)
        {
            return splitByListIgnoreBracket(input, ref operators, false);
        }

        private List<String> splitByListIgnoreBracket(String input, ref List<String> operators, bool returnIfOneElement)
        {
            String buffer = "";
            String s;
            List<String> retList = new List<string>();

            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] == '(')
                {
                    int startI = i;
                    Utils.Balance(ref Text, ref i, ')');
                    buffer += Text.Substring(startI, i - startI + 1);
                }
                else if (input[i] == '"')
                {
                    int startI = i;
                    i = Utils.FindEndOfString(Text, i + 1);
                    buffer += Text.Substring(startI, i - startI + 1);
                }
                else
                {
                    s = matchAt(ref input, i, ref operators);

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

        private List<String> parseParameters(String input)
        {
            var splitList = splitByListIgnoreBracket(input, ref parameterSeperatorSymbols, true);

            if (splitList != null)
            {
                List<String> retList = new List<string>();

                foreach (var listItem in splitList)
                {
                    if (listItem != ",") retList.Add(listItem);
                }

                return retList;
            }

            return null;
        }

        private String matchAt(ref String input, int i, ref List<String> matchables)
        {
            foreach (String s in matchables)
            {
                if (s.StartsWith(" "))
                {
                    Regex r = new Regex("^" + s.Replace(" ", "\\s"), RegexOptions.IgnoreCase);
                    Match m = r.Match(input.Substring(i));

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
