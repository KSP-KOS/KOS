using System;
using kOS.Safe.Encapsulation;
using kOS.Safe.Exceptions;
namespace kOS.Safe.Compilation.KS
{
    public static class ExpressionBuilder
    {
        public static ExpressionNode BuildExpression(ParseNode node)
        {
            switch (node.Token.Type)
            {
            case TokenType.expr:
                // just a wrapper around another node
                return BuildExpression(node.Nodes[0]);
            case TokenType.or_expr:
                return BuildLogicExpression(node, new OrExpressionNode() { ParseNode = node });
            case TokenType.and_expr:
                return BuildLogicExpression(node, new AndExpressionNode() { ParseNode = node });
            case TokenType.compare_expr:
                return BuildBinaryExpression(node, (separator) => new CompareExpressionNode() {
                    Comparator = separator.Token.Text,
                    ParseNode = separator
                });
            case TokenType.arith_expr:
                return BuildBinaryExpression(node, (separator) => (
                    separator.Token.Text == "+"
                    ? (BinaryExpressionNode) new AddExpressionNode() { ParseNode = separator }
                    : (BinaryExpressionNode) new SubtractExpressionNode() { ParseNode = separator }
                ));
            case TokenType.multdiv_expr:
                return BuildBinaryExpression(node, (separator) => (
                    separator.Token.Type == TokenType.MULT
                    ? (BinaryExpressionNode) new MultiplyExpressionNode() { ParseNode = separator }
                    : (BinaryExpressionNode) new DivideExpressionNode() { ParseNode = separator }
                ));
            case TokenType.unary_expr:
                return BuildUnaryExpression(node);
            case TokenType.factor:
                return BuildBinaryExpression(node, (separator) => new PowerExpressionNode() { ParseNode = separator });
            case TokenType.suffix:
                return BuildSuffix(node);
            case TokenType.atom:
                return BuildAtom(node);
            case TokenType.onoff_trailer:
                return BuildOnOff(node);
            case TokenType.varidentifier:
                // just a wrapper around a suffix
                return BuildSuffix(node.Nodes[0]);
            case TokenType.instruction_block:
                return BuildLambda(node);
            default:
                throw new KOSYouShouldNeverSeeThisException("Unknown expression token: " + node.Token.Type);
            }
        }

        private static ExpressionNode BuildLogicExpression(ParseNode node, LogicExpressionNode logicNode)
        {
            // these are of the form
            // expr -> sub_expr (SEPARATOR sub_expr)*
            // these are logic operations, so we group them all up in one node so we can short circuit easily

            if (node.Nodes.Count == 1)
            {
                // just a passthrough
                return BuildExpression(node.Nodes[0]);
            }

            int count = (node.Nodes.Count + 1) / 2;
            ExpressionNode[] exprs = new ExpressionNode[count];

            for (int i = 0; i < count; i++)
            {
                exprs[i] = BuildExpression(node.Nodes[i*2]);
            }

            logicNode.Expressions = exprs;

            return logicNode;
        }

        private delegate BinaryExpressionNode BinaryExpressioner(ParseNode separator);

        private static ExpressionNode BuildBinaryExpression(ParseNode node, BinaryExpressioner expressioner)
        {
            // these all take the form
            // expr -> sub_expr (SEPARATOR sub_expr)*

            // start with the leftmost subexpression
            ExpressionNode expr = BuildExpression(node.Nodes[0]);

            // build up the tree
            for (int i = 1; i < node.Nodes.Count; i += 2)
            {
                // Construct the parent node from the separator
                BinaryExpressionNode next = expressioner(node.Nodes[i]);
                // Add its left and right children
                next.Left = expr;
                next.Right = BuildExpression(node.Nodes[i+1]);

                // this is our new root expression
                expr = next;
            }

            return expr;
        }

        private static ExpressionNode BuildUnaryExpression(ParseNode node)
        {
            // unary_expr -> (PLUSMINUS|NOT|DEFINED)? factor

            if (node.Nodes.Count == 1)
            {
                // just a passthrough node
                return BuildExpression(node.Nodes[0]);
            }

            Token op = node.Nodes[0].Token;
            ExpressionNode target = BuildExpression(node.Nodes[1]);

            switch (op.Type)
            {
            case TokenType.PLUSMINUS:
                if (op.Text == "+")
                {
                    // +foo is the same as foo
                    return target;
                }
                else
                {
                    return new NegateExpressionNode() {
                        ParseNode = node,
                        Target = target
                    };
                }
            case TokenType.NOT:
                return new NotExpressionNode() {
                    ParseNode = node,
                    Target = target
                };
            case TokenType.DEFINED:
                if (target is IdentifierAtomNode)
                {
                    return new DefinedExpressionNode() {
                        ParseNode = node,
                        Identifier = ((IdentifierAtomNode)target).Identifier
                    };
                }
                else
                {
                    throw new KOSCompileException(node.Token, "DEFINED can only operate on an identifier");
                }
            default:
                throw new KOSYouShouldNeverSeeThisException("Unexpected unary_expr op: " + op.Type);
            }
        }

        private static ExpressionNode BuildSuffix(ParseNode node)
        {
            // suffix -> suffixterm (suffix_trailer)*

            ExpressionNode expr = BuildSuffixTerm(node.Nodes[0], null);
            for (int i = 1; i < node.Nodes.Count; i++)
            {
                // suffix_trailer -> COLON suffixterm
                ParseNode trailer = node.Nodes[i];

                expr = BuildSuffixTerm(trailer.Nodes[1], expr);
            }

            return expr;
        }

        private static ExpressionNode BuildSuffixTerm(ParseNode node, ExpressionNode baseNode)
        {
            // suffixterm -> atom suffixterm_trailer*
            ExpressionNode expr = BuildExpression(node.Nodes[0]);

            // If we have a baseNode, the first expression is a suffix and must be an IdentifierAtomNode
            if (baseNode != null)
            {
                if (expr is IdentifierAtomNode)
                {
                    expr = new GetSuffixNode() {
                        ParseNode = node,
                        Base = baseNode,
                        Suffix = ((IdentifierAtomNode)expr).Identifier
                    };
                }
                else
                {
                    throw new KOSCompileException(node.Token, "Suffix must be an identifier");
                }
            }

            for (int i = 1; i < node.Nodes.Count; i++)
            {
                // suffixterm_trailer -> (function_trailer | array_trailer)
                // immediately unwrap it into the trailer
                ParseNode trailer = node.Nodes[i].Nodes[0];

                switch (trailer.Token.Type)
                {
                case TokenType.function_trailer:
                    // function_trailer -> (BRACKETOPEN arglist? BRACKETCLOSE) | ATSIGN

                    if (trailer.Nodes[0].Token.Type == TokenType.ATSIGN)
                    {
                        // this is only valid on identifiers
                        if (expr is IdentifierAtomNode)
                        {
                            expr = new FunctionAddressNode() {
                                ParseNode = trailer,
                                Identifier = ((IdentifierAtomNode)expr).Identifier
                            };
                        }
                        else
                        {
                            throw new KOSCompileException(trailer.Token, "function address only valid on identifiers");
                        }
                    }
                    else
                    {
                        ExpressionNode[] args = new ExpressionNode[0];
                        if (trailer.Nodes.Count == 3)
                        {
                            args = BuildArglist(trailer.Nodes[1]);
                        }

                        if (expr is IdentifierAtomNode)
                        {
                            expr = new DirectCallNode() {
                                ParseNode = trailer,
                                Identifier = ((IdentifierAtomNode)expr).Identifier,
                                Arguments = args
                            };
                        }
                        else if (expr is GetSuffixNode)
                        {
                            expr = new CallSuffixNode() {
                                ParseNode = trailer,
                                Base = ((GetSuffixNode)expr).Base,
                                Suffix = ((GetSuffixNode)expr).Suffix,
                                Arguments = args
                            };
                        }
                        else
                        {
                            expr = new IndirectCallNode() {
                                ParseNode = trailer,
                                Base = expr,
                                Arguments = args
                            };
                        }
                    }
                    break;
                case TokenType.array_trailer:
                    // array_trailer -> (ARRAYINDEX (IDENTIFIER | INTEGER)) | (SQUAREOPEN expr SQUARECLOSE)
                    // either way the array index is node #1
                    expr = new GetIndexNode() {
                        ParseNode = trailer,
                        Base = expr,
                        Index = BuildExpression(trailer.Nodes[1])
                    };
                    break;
                default:
                    throw new KOSYouShouldNeverSeeThisException("unknown suffixterm_trailer: " + trailer.Token.Type);
                }
            }

            return expr;
        }

        private static ExpressionNode[] BuildArglist(ParseNode node)
        {
            // arglist -> expr (COMMA expr)*
            int count = (node.Nodes.Count + 1) / 2;
            ExpressionNode[] args = new ExpressionNode[count];

            for (int i = 0; i < count; i++)
            {
                // arguments are children 0, 2, 4, 6, etc.
                args[i] = BuildExpression(node.Nodes[i*2]);
            }

            return args;
        }

        private static ExpressionNode BuildAtom(ParseNode node)
        {
            // atom -> sci_number | TRUEFALSE | IDENTIFIER | FILEIDENT | STRING | (BRACKETOPEN expr BRACKETCLOSE)

            // If its a parenthesized expression, just return the inner expression
            if (node.Nodes.Count == 3)
            {
                return BuildExpression(node.Nodes[1]);
            }

            ParseNode atom = node.Nodes[0];

            switch (atom.Token.Type)
            {
            case TokenType.sci_number:
                return BuildNumber(atom);
            case TokenType.TRUEFALSE:
                return new BooleanAtomNode() {
                    ParseNode = atom,
                    Value = atom.Token.Text.ToLower() == "true"
                };
            case TokenType.IDENTIFIER:
            case TokenType.FILEIDENT:
                return new IdentifierAtomNode() {
                    ParseNode = atom,
                    Identifier = atom.Token.Text
                };
            case TokenType.STRING:
                // strip off the quotes
                return new StringAtomNode() {
                    ParseNode = atom,
                    Value = atom.Token.Text.Substring(1, atom.Token.Text.Length - 2)
                };
            default:
                throw new KOSYouShouldNeverSeeThisException("Unknown atom token: " + atom.Token.Type);
            }
        }

        private static ExpressionNode BuildNumber(ParseNode node)
        {
            // sci_number -> number (E PLUSMINUS? INTEGER)?

            // We just join all the strings together, then try to parse it as an int,
            // falling back to a double if that doesn't work.

            // number is just a wrapper around a numeric token
            string numberText = node.Nodes[0].Nodes[0].Token.Text;

            // add all the suffixes
            for (int i = 1; i < node.Nodes.Count; i++)
            {
                numberText += node.Nodes[i].Token.Text;
            }

            numberText = numberText.Replace("_", "");

            ScalarValue value;

            if (ScalarValue.TryParseInt(numberText, out value) || ScalarValue.TryParseDouble(numberText, out value)) {
                return new ScalarAtomNode() {
                    ParseNode = node,
                    Value = value
                };
            }

            throw new KOSCompileException(node.Token, string.Format(KOSNumberParseException.TERSE_MSG_FMT, node.Token.Text));
        }

        private static ExpressionNode BuildOnOff(ParseNode node)
        {
            // onoff_trailer -> (ON | OFF)
            return new BooleanAtomNode() {
                ParseNode = node,
                Value = node.Nodes[0].Token.Type == TokenType.ON
            };
        }

        private static ExpressionNode BuildLambda(ParseNode node)
        {
            return new LambdaNode() { ParseNode = node };
        }
    }
}
