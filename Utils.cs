using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using UnityEngine;

namespace kOS
{
    public enum kOSKeys
    {
        LEFT = 37, UP = 38, RIGHT = 39, DOWN = 40,
        DEL = 46,
        F1 = 112, F2 = 113, F3 = 114, F4 = 115, F5 = 116, F6 = 117, F7 = 118, F8 = 119, F9 = 120, F10 = 121, F11 = 122, F12 = 123,
        PGUP = 33, PGDN = 34, END = 35, HOME = 36, DELETE = 44, INSERT = 45,
        BREAK = 19
    }

    public static class Utils
    {
        public static List<String> Split(String input, char delimiter, bool ignoreString)
        {
            input = input.Trim();

            List<String> retList = new List<string>();

            char[] inputChars = input.ToCharArray();

            int start = 0;
            for (int i = 0; i < input.Length; i++)
            {
                if (ignoreString && inputChars[i] == '"')
                {
                    // Skip over strings
                    i = FindEndOfString(input, i + 1);
                }
                else if (inputChars[i] == delimiter)
                {
                    retList.Add(input.Substring(start, i - start));
                    start = i + 1;
                }
            }

            if (start < input.Length - 1)
            {
                retList.Add(input.Substring(start));
            }

            return retList;
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

        public static int BraceMatch(String text, int start)
        {
            char[] input = text.ToCharArray();
            int braceLevel = 0;
            for (int i = start; i < input.Count(); i++)
            {
                if (input[i] == '{')
                {
                    braceLevel++;
                }
                else if (input[i] == '}')
                {
                    braceLevel--;
                    if (braceLevel == 0) return i;
                }
                else if (input[i] == '"')
                {
                    i = FindEndOfString(text, i + 1);
                }
            }

            return -1;
        }

		public static bool DelimterMatch(string str)
		{
			var items = new Stack<int>(str.Length);
			for (int i = 0; i < str.Length; i++)
			{
				char c = str[i];
				if (c == '(' || c == ')' | c == '"')
				{
					items.Push(i);
				}
			}
			if (items.Count != 0 && (items.Count % 2) == 1)
			{
				return false;
			}
			return true;
		}

        public static float ProspectForResource(String resourceName, List<Part> engines)
        {
            List<Part> visited = new List<Part>();
            float total = 0;

            foreach (var part in engines)
            {
                total += ProspectForResource(resourceName, part, ref visited);
            }

            return total;
        }

        public static float ProspectForResource(String resourceName, Part engine)
        {
            List<Part> visited = new List<Part>();

            return ProspectForResource(resourceName, engine, ref visited);
        }

        public static float ProspectForResource(String resourceName, Part part, ref List<Part> visited)
        {
            float ret = 0;

            if (visited.Contains(part))
            {
                return 0;
            }

            visited.Add(part);

            foreach (PartResource resource in part.Resources)
            {
                if (resource.resourceName.ToLower() == resourceName.ToLower())
                {
                    ret += (float)resource.amount;
                }
            }

            foreach (AttachNode attachNode in part.attachNodes)
            {
                if (attachNode.attachedPart != null                                 //if there is a part attached here            
                        && attachNode.nodeType == AttachNode.NodeType.Stack             //and the attached part is stacked (rather than surface mounted)
                        && (attachNode.attachedPart.fuelCrossFeed                       //and the attached part allows fuel flow
                            )
                        && !(part.NoCrossFeedNodeKey.Length > 0                       //and this part does not forbid fuel flow
                                && attachNode.id.Contains(part.NoCrossFeedNodeKey)))     //    through this particular node
                {


                    ret += ProspectForResource(resourceName, attachNode.attachedPart, ref visited);
                }
            }

            return ret;
        }

        public static string[] ProcessParams(string input)
        {
            String buffer = "";
            List<String> output = new List<string>();

            for (var i = 0; i < input.Length; i++)
            {
                char c = input[i];

                if (c == '\"')
                {
                    var prevI = i;
                    i = Expression.FindEndOfString(input, i + 1);
                    buffer += input.Substring(prevI, i - prevI + 1);
                }
                else
                {
                    if (c == ',')
                    {
                        output.Add(buffer.Trim());
                        buffer = "";
                    }
                    else
                    {
                        buffer += c;
                    }
                }
            }

            if (buffer.Trim().Length > 0) output.Add(buffer.Trim());

            return output.ToArray();
        }
    }
}

 
