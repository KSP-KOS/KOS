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
            
            // TODO: I don't like that the compiler has to know about the interpreter
            if (contextId != "interpreter") parts = _cache.GetFromCache(scriptText);

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

                    // TODO: I don't like that the compiler has to know about the interpreter
                    if (contextId != "interpreter") _cache.AddToCache(scriptText, parts);
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
    }
}
