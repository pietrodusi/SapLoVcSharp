using SapLoVcSharp.Core.Ast.Expressions;
using SapLoVcSharp.Core.Ast.Statements;
using SapLoVcSharp.Core.Common;

namespace SapLoVcSharp.Core.Ast.Dependencies
{
    /// <summary>
    /// Represents a constraint dependency with OBJECTS, CONDITION, RESTRICTIONS, INFERENCES sections.
    /// </summary>
    public class ConstraintNode : DependencyNode
    {
        public List<ObjectDeclaration> Objects { get; }
        public ExpressionNode? Condition { get; }
        public List<StatementNode> Restrictions { get; }
        public List<string> Inferences { get; }

        public ConstraintNode(
            List<ObjectDeclaration> objects,
            ExpressionNode? condition,
            List<StatementNode> restrictions,
            List<string> inferences,
            SourcePosition position)
            : base(position)
        {
            Objects = objects;
            Condition = condition;
            Restrictions = restrictions;
            Inferences = inferences;
        }

        public override void Accept(IAstVisitor visitor)
        {
            visitor.VisitConstraint(this);
        }

        public override string ToString() => $"Constraint(Objects: {Objects.Count}, Restrictions: {Restrictions.Count})";
    }

    /// <summary>
    /// Represents an object declaration in a constraint (e.g., PC IS_A (300) BIKE).
    /// </summary>
    public class ObjectDeclaration
    {
        public string Variable { get; }
        public string ClassName { get; }
        public string? ClassType { get; }
        public Dictionary<string, string>? VariableMappings { get; }  // WHERE clause

        public ObjectDeclaration(
            string variable,
            string className,
            string? classType = null,
            Dictionary<string, string>? variableMappings = null)
        {
            Variable = variable;
            ClassName = className;
            ClassType = classType;
            VariableMappings = variableMappings;
        }

        public override string ToString() => $"{Variable} IS_A ({ClassType}) {ClassName}";
    }
}