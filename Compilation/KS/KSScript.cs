using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace kOS.Compilation.KS
{
    public class KSScript : Script
    {
        private Scanner _scanner;
        private Parser _parser;
        private Dictionary<string, Context> _contexts;
        private Context _currentContext;

        public KSScript()
        {
            _scanner = new Scanner();
            _parser = new Parser(_scanner);
            _contexts = new Dictionary<string, Context>();
        }

        public override List<CodePart> Compile(string scriptText)
        {
            return Compile(scriptText, string.Empty);
        }

        public override List<CodePart> Compile(string scriptText, string contextId)
        {
            List<CodePart> parts = null;
            
            // make the code lowercase
            scriptText = MakeLowerCase(scriptText);
            scriptText = ReplaceIdentifiers(scriptText);
            
            if (contextId == string.Empty) parts = _cache.GetFromCache(scriptText);

            // if parts is null means the code doesn't exists in the cache
            if (parts == null)
            {
                parts = new List<CodePart>();
                ParseTree parseTree = _parser.Parse(scriptText);
                if (parseTree.Errors.Count == 0)
                {
                    Compiler compiler = new Compiler();
                    LoadContext(contextId);

                    // TODO: handle compile errors (e.g. wrong run parameter count)
                    CodePart mainPart = compiler.Compile(parseTree, _currentContext);

                    // add locks and triggers
                    parts.AddRange(_currentContext.Locks.GetNewParts());
                    parts.AddRange(_currentContext.Triggers.GetNewParts());

                    parts.Add(mainPart);

                    AssignInstructionId(parts);

                    if (contextId == string.Empty) _cache.AddToCache(scriptText, parts);
                }
                else
                {
                    ParseError error = parseTree.Errors[0];
                    RaiseParseException(scriptText, error.Line, error.Position);
                } 
            }

            return parts;
        }

        private void LoadContext(string contextId)
        {
            if (contextId != string.Empty)
            {
                if (_contexts.ContainsKey(contextId))
                {
                    _currentContext = _contexts[contextId];
                }
                else
                {
                    _currentContext = new Context();
                    _contexts.Add(contextId, _currentContext);
                }
            }
            else
            {
                _currentContext = new Context();
            }
        }

        private void AssignInstructionId(List<CodePart> parts)
        {
            _currentContext.InstructionId++;
            foreach (CodePart part in parts)
            {
                part.AssignInstructionId(_currentContext.InstructionId);
            }
        }

        public override void ClearContext(string contextId)
        {
            if (_contexts.ContainsKey(contextId))
            {
                _contexts.Remove(contextId);
            }
        }

        public override bool IsCommandComplete(string command)
        {
            char[] commandChars = command.ToCharArray();
            int length = commandChars.Length;
            int openCurlyBrackets = 0;
            int openBrackets = 0;

            for (int n = 0; n < length; n++)
            {
                // match curly brackets
                if (commandChars[n] == '{')
                    openCurlyBrackets++;

                if (commandChars[n] == '}')
                    openCurlyBrackets--;

                // match brackets
                if (commandChars[n] == '(')
                    openBrackets++;

                if (commandChars[n] == ')')
                    openBrackets--;
            }

            return (openCurlyBrackets <= 0)
                && (openBrackets <= 0);
        }
    }
}
