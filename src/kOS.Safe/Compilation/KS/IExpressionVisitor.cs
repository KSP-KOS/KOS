using System;
namespace kOS.Safe.Compilation.KS
{
    public interface IExpressionVisitor
    {
        void VisitExpression(OrExpressionNode node);
        void VisitExpression(AndExpressionNode node);
        void VisitExpression(CompareExpressionNode node);
        void VisitExpression(AddExpressionNode node);
        void VisitExpression(SubtractExpressionNode node);
        void VisitExpression(MultiplyExpressionNode node);
        void VisitExpression(DivideExpressionNode node);
        void VisitExpression(PowerExpressionNode node);
        void VisitExpression(NegateExpressionNode node);
        void VisitExpression(NotExpressionNode node);
        void VisitExpression(DefinedExpressionNode node);
        void VisitExpression(GetSuffixNode node);
        void VisitExpression(CallSuffixNode node);
        void VisitExpression(GetIndexNode node);
        void VisitExpression(DirectCallNode node);
        void VisitExpression(IndirectCallNode node);
        void VisitExpression(FunctionAddressNode node);
        void VisitExpression(LambdaNode node);
        void VisitExpression(ScalarAtomNode node);
        void VisitExpression(StringAtomNode node);
        void VisitExpression(BooleanAtomNode node);
        void VisitExpression(IdentifierAtomNode node);
    }
}
