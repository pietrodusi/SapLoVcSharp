using SapLoVcSharp.Core.Ast;
using SapLoVcSharp.Core.Ast.Dependencies;
using SapLoVcSharp.Core.Ast.Expressions;
using SapLoVcSharp.Core.Ast.Statements;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace SapLoVcSharp.Data.Serialization
{
    public class AstSerializer : IAstSerializer
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PreferredObjectCreationHandling = JsonObjectCreationHandling.Replace,
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
            },
            TypeInfoResolver = new PolymorphicTypeResolver(),
            ReferenceHandler = ReferenceHandler.IgnoreCycles
        };

        public string Serialize(DependencyNode node)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));

            return JsonSerializer.Serialize<DependencyNode>(node, Options);
        }

        public DependencyNode Deserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("JSON cannot be empty", nameof(json));

            return JsonSerializer.Deserialize<DependencyNode>(json, Options)
                ?? throw new InvalidOperationException("Deserialization returned null");
        }

        public T Deserialize<T>(string json) where T : DependencyNode
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("JSON cannot be empty", nameof(json));

            return JsonSerializer.Deserialize<T>(json, Options)
                ?? throw new InvalidOperationException("Deserialization returned null");
        }
    }

    public class PolymorphicTypeResolver : DefaultJsonTypeInfoResolver
    {
        public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
        {
            var typeInfo = base.GetTypeInfo(type, options);

            if (type == typeof(AstNode))
            {
                typeInfo.PolymorphismOptions = new JsonPolymorphismOptions
                {
                    TypeDiscriminatorPropertyName = "$type",
                    IgnoreUnrecognizedTypeDiscriminators = true,
                    UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToNearestAncestor
                };
            }
            else if (type == typeof(DependencyNode))
            {
                typeInfo.PolymorphismOptions = new JsonPolymorphismOptions
                {
                    TypeDiscriminatorPropertyName = "$type",
                    IgnoreUnrecognizedTypeDiscriminators = true,
                    UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToNearestAncestor,
                    DerivedTypes =
                    {
                        new JsonDerivedType(typeof(ProcedureNode), "Procedure"),
                        new JsonDerivedType(typeof(ConstraintNode), "Constraint"),
                        new JsonDerivedType(typeof(PreconditionNode), "Precondition"),
                        new JsonDerivedType(typeof(SelectionConditionNode), "SelectionCondition")
                    }
                };
            }
            else if (type == typeof(ExpressionNode))
            {
                typeInfo.PolymorphismOptions = new JsonPolymorphismOptions
                {
                    TypeDiscriminatorPropertyName = "$type",
                    IgnoreUnrecognizedTypeDiscriminators = true,
                    UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToNearestAncestor,
                    DerivedTypes =
                    {
                        new JsonDerivedType(typeof(LiteralNode), "Literal"),
                        new JsonDerivedType(typeof(IdentifierNode), "Identifier"),
                        new JsonDerivedType(typeof(MemberAccessNode), "MemberAccess"),
                        new JsonDerivedType(typeof(ConstraintMemberAccessNode), "ConstraintMemberAccess"),
                        new JsonDerivedType(typeof(ObjectReferenceNode), "ObjectReference"),
                        new JsonDerivedType(typeof(BinaryExpressionNode), "BinaryExpression"),
                        new JsonDerivedType(typeof(UnaryExpressionNode), "UnaryExpression"),
                        new JsonDerivedType(typeof(CallExpressionNode), "CallExpression"),
                        new JsonDerivedType(typeof(InExpressionNode), "InExpression"),
                        new JsonDerivedType(typeof(SpecifiedNode), "Specified"),
                        new JsonDerivedType(typeof(TableCallExpressionNode), "TableCall")
                    }
                };
            }
            else if (type == typeof(StatementNode))
            {
                typeInfo.PolymorphismOptions = new JsonPolymorphismOptions
                {
                    TypeDiscriminatorPropertyName = "$type",
                    IgnoreUnrecognizedTypeDiscriminators = true,
                    UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToNearestAncestor,
                    DerivedTypes =
                    {
                        new JsonDerivedType(typeof(AssignmentNode), "Assignment"),
                        new JsonDerivedType(typeof(TableCallNode), "TableCall"),
                        new JsonDerivedType(typeof(BuiltInFunctionCallNode), "BuiltInFunctionCall")
                    }
                };
            }

            return typeInfo;
        }
    }
}