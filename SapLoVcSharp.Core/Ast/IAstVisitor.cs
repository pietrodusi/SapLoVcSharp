using SapLoVcSharp.Core.Ast.Dependencies;
using SapLoVcSharp.Core.Ast.Expressions;
using SapLoVcSharp.Core.Ast.Statements;

namespace SapLoVcSharp.Core.Ast
{
    /// <summary>
    /// Visitor pattern for traversing the AST.
    /// </summary>
    public interface IAstVisitor
    {
        // Expressions
        void VisitLiteral(LiteralNode node);
        void VisitIdentifier(IdentifierNode node);
        void VisitBinaryExpression(BinaryExpressionNode node);
        void VisitUnaryExpression(UnaryExpressionNode node);
        void VisitCallExpression(CallExpressionNode node);
        void VisitMemberAccess(MemberAccessNode node);
        void VisitInExpression(InExpressionNode node);
        void VisitSpecified(SpecifiedNode node);
        void VisitTableCallExpression(TableCallExpressionNode node);
        void VisitObjectReference(ObjectReferenceNode node);
        void VisitConstraintMemberAccess(ConstraintMemberAccessNode node);

        // Statements
        void VisitAssignment(AssignmentNode node);
        void VisitTableCall(TableCallNode node);
        void VisitBuiltInFunctionCall(BuiltInFunctionCallNode node);
        void VisitReturnInconsistency(ReturnInconsistencyNode node);

        // Dependencies
        void VisitProcedure(ProcedureNode node);
        void VisitPrecondition(PreconditionNode node);
        void VisitSelectionCondition(SelectionConditionNode node);
        void VisitConstraint(ConstraintNode node);
    }
}