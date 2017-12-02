using System;
using kOS.Safe.Encapsulation;
namespace kOS.Safe.Compilation.KS
{
    public abstract class ExpressionNode
    {
        public ParseNode ParseNode { get; set; }

        public abstract void Accept(IExpressionVisitor visitor);
    }

    public abstract class LogicExpressionNode : ExpressionNode
    {
        public ExpressionNode[] Expressions { get; set; }
    }

    public class OrExpressionNode : LogicExpressionNode
    {
        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.VisitExpression(this);
        }
    }

    public class AndExpressionNode : LogicExpressionNode
    {
        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.VisitExpression(this);
        }
    }

    public abstract class BinaryExpressionNode : ExpressionNode
    {
        public ExpressionNode Left { get; set; }
        public ExpressionNode Right { get; set; }
    }

    public class CompareExpressionNode : BinaryExpressionNode
    {
        public string Comparator { get; set; }

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.VisitExpression(this);
        }
    }

    public class AddExpressionNode : BinaryExpressionNode
    {
        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.VisitExpression(this);
        }
    }

    public class SubtractExpressionNode : BinaryExpressionNode
    {
        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.VisitExpression(this);
        }
    }

    public class MultiplyExpressionNode : BinaryExpressionNode
    {
        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.VisitExpression(this);
        }
    }

    public class DivideExpressionNode : BinaryExpressionNode
    {
        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.VisitExpression(this);
        }
    }

    public class PowerExpressionNode : BinaryExpressionNode
    {
        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.VisitExpression(this);
        }
    }

    public class NegateExpressionNode : ExpressionNode
    {
        public ExpressionNode Target { get; set; }

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.VisitExpression(this);
        }
    }

    public class NotExpressionNode : ExpressionNode
    {
        public ExpressionNode Target { get; set; }

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.VisitExpression(this);
        }
    }

    public class DefinedExpressionNode : ExpressionNode
    {
        public string Identifier { get; set; }

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.VisitExpression(this);
        }
    }

    public class GetSuffixNode : ExpressionNode
    {
        public ExpressionNode Base { get; set; }
        public string Suffix { get; set; }

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.VisitExpression(this);
        }
    }

    public class CallSuffixNode : ExpressionNode
    {
        public ExpressionNode Base { get; set; }
        public string Suffix { get; set; }
        public ExpressionNode[] Arguments { get; set; }

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.VisitExpression(this);
        }
    }

    public class GetIndexNode : ExpressionNode
    {
        public ExpressionNode Base { get; set; }
        public ExpressionNode Index { get; set; }

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.VisitExpression(this);
        }
    }

    public class DirectCallNode : ExpressionNode
    {
        public string Identifier { get; set; }
        public ExpressionNode[] Arguments { get; set; }

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.VisitExpression(this);
        }
    }

    public class IndirectCallNode : ExpressionNode
    {
        public ExpressionNode Base { get; set; }
        public ExpressionNode[] Arguments { get; set; }

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.VisitExpression(this);
        }
    }

    public class FunctionAddressNode : ExpressionNode
    {
        public string Identifier { get; set; }

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.VisitExpression(this);
        }
    }

    public class LambdaNode : ExpressionNode
    {
        // TODO: what needs to be here

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.VisitExpression(this);
        }
    }

    public class ScalarAtomNode : ExpressionNode
    {
        public ScalarValue Value { get; set; }

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.VisitExpression(this);
        }
    }

    public class StringAtomNode : ExpressionNode
    {
        public string Value { get; set; }

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.VisitExpression(this);
        }
    }

    public class BooleanAtomNode : ExpressionNode
    {
        public bool Value { get; set; }

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.VisitExpression(this);
        }
    }

    public class IdentifierAtomNode : ExpressionNode
    {
        public string Identifier { get; set; }

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.VisitExpression(this);
        }
    }
}
