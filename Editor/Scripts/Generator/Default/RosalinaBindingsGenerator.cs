#if UNITY_EDITOR

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

internal class RosalinaBindingsGenerator : IRosalinaGeneartor
{
    private const string DocumentFieldName = "_document";
    private const string RootVisualElementFieldName = "_rootVisualElement";
    private const string RootPropertyName = "Root";
    private const string InitializeDocumentMethodName = "InitializeDocument";

    public RosalinaGenerationResult Generate(UIDocumentAsset documentAsset)
    {
        if (documentAsset is null)
        {
            throw new ArgumentNullException(nameof(documentAsset), "Cannot generate binding with a null document asset.");
        }

        MemberDeclarationSyntax documentVariable = CreateDocumentVariable();
        MemberDeclarationSyntax visualElementVariable = CreateVisualElementVariable();
        MemberDeclarationSyntax visualElementProperty = CreateVisualElementRootProperty();
        MemberDeclarationSyntax[] constructors = CreateConstructors(documentAsset.Name);
        InitializationStatement[] statements = RosalinaStatementSyntaxFactory.GenerateInitializeStatements(documentAsset.UxmlDocument, CreateRootQueryMethodAccessor());
        PropertyDeclarationSyntax[] propertyStatements = statements.Select(x => x.Property).ToArray();
        StatementSyntax[] initializationStatements = statements.Select(x => x.Statement).ToArray();

        MethodDeclarationSyntax initializeMethod = CreateInitialiseMethod(initializationStatements);

        ClassDeclarationSyntax @class = SyntaxFactory.ClassDeclaration(documentAsset.Name)
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PartialKeyword))
            .AddMembers(documentVariable)
            .AddMembers(visualElementVariable)
            .AddMembers(propertyStatements)
            .AddMembers(visualElementProperty)
            .AddMembers(constructors)
            .AddMembers(initializeMethod);

        CompilationUnitSyntax compilationUnit = SyntaxFactory.CompilationUnit()
            .AddUsings(
                SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName("System")),
                SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName("UnityEngine")),
                SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName("UnityEngine.UIElements"))
             )
            .AddMembers(@class);

        string code = compilationUnit
            .NormalizeWhitespace()
            .ToFullString();
        string generatedCode = RosalinaGeneratorConstants.GeneratedCodeHeader + code;

        return new RosalinaGenerationResult(generatedCode);
    }

    public MemberDeclarationSyntax[] CreateConstructors(string className)
    {
        var defaultCtor = ConstructorDeclaration(Identifier(className))
            .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
            .WithBody(Block());

        var documentParamterIdentifier = IdentifierName("document");
        var rootParamterIdentifier = IdentifierName("root");
        var documentFieldIdentifier = IdentifierName(DocumentFieldName);

        var docCtor = ConstructorDeclaration(Identifier(className))
            .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
            .WithParameterList(
                ParameterList(
                    SeparatedList<ParameterSyntax>(
                        new SyntaxNodeOrToken[] {
                            Parameter(documentParamterIdentifier.Identifier)
                                .WithType(IdentifierName("UIDocument")),
                            Token(SyntaxKind.CommaToken),
                            Parameter(rootParamterIdentifier.Identifier)
                                .WithType(IdentifierName("VisualElement"))
                        }
                    )
                )
            )
            .WithBody(
                Block(
                    ExpressionStatement(
                        AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            documentFieldIdentifier,
                            documentParamterIdentifier)),
                    ExpressionStatement(
                        AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            IdentifierName(RootVisualElementFieldName),
                            rootParamterIdentifier
                        )
                    )
                )
            );

        return new MemberDeclarationSyntax[] { defaultCtor, docCtor };
    }

    private MethodDeclarationSyntax CreateInitialiseMethod(StatementSyntax[] initializationStatements)
    {
        return RosalinaSyntaxFactory.CreateMethod("void", InitializeDocumentMethodName, SyntaxKind.PublicKeyword)
            .WithBody(SyntaxFactory.Block(initializationStatements));
    }

    private static MemberDeclarationSyntax CreateDocumentVariable()
    {
        string documentPropertyTypeName = typeof(UIDocument).Name;
        NameSyntax serializeFieldName = SyntaxFactory.ParseName(typeof(SerializeField).Name);

        FieldDeclarationSyntax documentField = RosalinaSyntaxFactory.CreateField(documentPropertyTypeName, DocumentFieldName, SyntaxKind.PrivateKeyword)
            .AddAttributeLists(
                SyntaxFactory.AttributeList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Attribute(serializeFieldName)
                    )
                )
            );

        return documentField;
    }

    private static MemberDeclarationSyntax CreateVisualElementVariable()
    {
        string visualElementPropertyTypeName = typeof(VisualElement).Name;
        NameSyntax nonSerializeFieldName = SyntaxFactory.ParseName(typeof(NonSerializedAttribute).Name.Replace("Attribute", ""));

        FieldDeclarationSyntax documentField = RosalinaSyntaxFactory.CreateField(visualElementPropertyTypeName, RootVisualElementFieldName, SyntaxKind.PrivateKeyword)
            .AddAttributeLists(
                SyntaxFactory.AttributeList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Attribute(nonSerializeFieldName)
                    )
                )
            );

        return documentField;
    }

    private static MemberDeclarationSyntax CreateVisualElementRootProperty()
    {
        string propertyTypeName = typeof(VisualElement).Name;

        return RosalinaSyntaxFactory.CreateProperty(propertyTypeName, RootPropertyName, SyntaxKind.PublicKeyword)
            .AddAccessorListAccessors(
                SyntaxFactory.AccessorDeclaration(
                    SyntaxKind.GetAccessorDeclaration,
                    SyntaxFactory.Block(
                        SyntaxFactory.ReturnStatement(
                            SyntaxFactory.BinaryExpression(
                                SyntaxKind.CoalesceExpression,
                                SyntaxFactory.IdentifierName(RootVisualElementFieldName),
                                SyntaxFactory.ConditionalAccessExpression(
                                    SyntaxFactory.IdentifierName(DocumentFieldName),
                                    SyntaxFactory.MemberBindingExpression(
                                        SyntaxFactory.IdentifierName(UnityConstants.DocumentRootVisualElementFieldName)
                                    )
                                )
                            )
                        )
                    )
                )
            );
    }

    private static MemberAccessExpressionSyntax CreateRootQueryMethodAccessor()
    {
        return SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.IdentifierName($"{RootPropertyName}?"),
            SyntaxFactory.Token(SyntaxKind.DotToken),
            SyntaxFactory.IdentifierName(UnityConstants.RootVisualElementQueryMethodName)
        );
    }
}

#endif