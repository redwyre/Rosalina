#if UNITY_EDITOR

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

internal static class RosalinaStatementSyntaxFactory
{
    public static readonly string Instance = "Instance";
    public static readonly string Template = "Template";

    public static InitializationStatement[] GenerateInitializeStatements(UxmlDocument uxmlDocument, MemberAccessExpressionSyntax documentQueryMethodAccess)
    {
        var statements = new List<InitializationStatement>();
        IEnumerable<UIProperty> properties = uxmlDocument.GetChildren().Select(x => new UIProperty(x.Type, x.Name, x.Template)).ToList();

        var templates = properties.Where(p => p.TypeName == Template).ToDictionary(x => x.Name);

        properties = properties.Where(p => p.TypeName != Template);

        if (CheckForDuplicateProperties(properties))
        {
            throw new InvalidProgramException($"Failed to generate bindings for document: {uxmlDocument.Name} because of duplicate properties.");
        }

        foreach (UIProperty uiProperty in properties)
        {
            bool isInstance = uiProperty.TypeName == Instance;
            var template = isInstance ? templates[uiProperty.Template] : new UIProperty();
            var propertyTypeName = isInstance ? template.Name : uiProperty.Type?.Name;

            if (propertyTypeName is null)
            {
                Debug.LogWarning($"[Rosalina]: Failed to get property type: '{uiProperty.TypeName}', field: '{uiProperty.Name}' for document '{uxmlDocument.Path}'. Property will be ignored.");
                continue;
            }

            PropertyDeclarationSyntax @property = RosalinaSyntaxFactory.CreateProperty(propertyTypeName, uiProperty.Name, SyntaxKind.PublicKeyword)
                .AddAccessorListAccessors(
                    AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                        .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
                )
                .AddAccessorListAccessors(
                    AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                        .AddModifiers(Token(SyntaxKind.PrivateKeyword))
                        .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
                );

            var argumentList = SeparatedList(new[]
            {
                Argument(
                    LiteralExpression(
                        SyntaxKind.StringLiteralExpression,
                        Literal(uiProperty.OriginalName)
                    )
                )
            });

            ExpressionStatementSyntax statement;

            if (isInstance)
            {
                var templateType = IdentifierName(template.Name);
                statement = ExpressionStatement(
                    AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        IdentifierName(uiProperty.Name),
                        ObjectCreationExpression(templateType)
                        .WithArgumentList(
                            ArgumentList(
                                SeparatedList<ArgumentSyntax>(
                                    new SyntaxNodeOrToken[] {
                                        Argument(
                                            IdentifierName("_document")),
                                        Token(SyntaxKind.CommaToken),
                                        Argument(
                                            InvocationExpression(documentQueryMethodAccess, ArgumentList(argumentList))
                                        )
                                    }
                                )
                            )
                        )
                    )
                );
            }
            else
            {
                statement = ExpressionStatement(
                    AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        IdentifierName(uiProperty.Name),
                        CastExpression(
                            ParseTypeName(propertyTypeName),
                            InvocationExpression(documentQueryMethodAccess, ArgumentList(argumentList))
                        )
                    )
                );
            }

            statements.Add(new InitializationStatement(statement, property));
        }

        return statements.ToArray();
    }

    private static bool CheckForDuplicateProperties(IEnumerable<UIProperty> properties)
    {
        var duplicatePropertyGroups = properties.GroupBy(x => x.Name).Where(g => g.Count() > 1);
        bool containsDuplicateProperties = duplicatePropertyGroups.Any();

        if (containsDuplicateProperties)
        {
            foreach (var property in duplicatePropertyGroups)
            {
                string duplicateProperties = string.Join(", ", property.Select(x => $"{x.OriginalName}"));

                Debug.LogError($"Conflict detected between {duplicateProperties}.");
            }
        }

        return containsDuplicateProperties;
    }
}

#endif