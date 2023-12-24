﻿using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace DeepCloneGenerator;

[Generator]
public class CloneGenerator : IIncrementalGenerator
{
    internal const string Namespace = "DeepCloneGenerator";
    internal const string AttributeName = "GenerateCloneAttribute";
    internal const string IgnoreCloneAttribute = "CloneIgnoreAttribute";

    private const string AttributesSource =
        $$"""
          // <auto-generated/>

          namespace {{Namespace}}
          {
              [System.AttributeUsage(System.AttributeTargets.Class)]
              internal class {{AttributeName}} : System.Attribute
              {
              }
          
              [System.AttributeUsage(System.AttributeTargets.Property | System.AttributeTargets.Field)]
              internal class {{IgnoreCloneAttribute}} : System.Attribute
              {
              }
          }
          """;

    internal const string InterfaceName = "ISourceGeneratedCloneable";

    internal const string CtorVariableName = "original";
    internal const string CloneMethodName = "DeepClone";

    private const string InterfaceSource =
        $$"""
          // <auto-generated/>

          namespace {{Namespace}}
          {
              internal interface {{InterfaceName}}<TSelf>
              {
                  TSelf {{CloneMethodName}}();
              }
          }
          """;

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Add the marker attribute to the compilation.
        context.RegisterPostInitializationOutput(
            ctx =>
            {
                ctx.AddSource(
                    "Attributes.g.cs",
                    SourceText.From(AttributesSource, Encoding.UTF8)
                );
                ctx.AddSource(
                    $"{InterfaceName}.g.cs",
                    SourceText.From(InterfaceSource, Encoding.UTF8)
                );
            }
        );
        var provider = context.SyntaxProvider
            .CreateSyntaxProvider(
                (s, _) => s is TypeDeclarationSyntax,
                (ctx, _) => GetClassDeclarationForSourceGen(ctx)
            )
            .Where(t => t.AttributeFound)
            .Select((t, _) => t.Class);

        context.RegisterSourceOutput(
            context.CompilationProvider.Combine(provider.Collect()),
            (ctx, t) => GenerateCode(ctx, t.Left, t.Right)
        );
    }

    private void GenerateCode(SourceProductionContext ctx, Compilation compilation, ImmutableArray<TypeDeclarationSyntax> classDeclarations)
    {
        var classSymbols = classDeclarations
            .Select(
                classDeclaration => compilation.GetSemanticModel(classDeclaration.SyntaxTree)
                    .GetDeclaredSymbol(classDeclaration)
            )
            .OfType<INamedTypeSymbol>()
            .ToList();

        var context = new CloneGeneratorContext(classSymbols, compilation);
        context.Do(ref ctx);
    }

    private static (TypeDeclarationSyntax Class, bool AttributeFound) GetClassDeclarationForSourceGen(
        GeneratorSyntaxContext context)
    {
        var classDeclarationSyntax = (TypeDeclarationSyntax)context.Node;

        // Go through all attributes of the class.
        foreach (var attributeSyntax in classDeclarationSyntax.AttributeLists.SelectMany(attributeListSyntax => attributeListSyntax.Attributes))
        {
            var symbol = context.SemanticModel.GetSymbolInfo(attributeSyntax)
                .Symbol;

            if (symbol is not IMethodSymbol attributeSymbol)
            {
                continue; // if we can't get the symbol, ignore it
            }

            var attributeName = attributeSymbol.ContainingType.ToDisplayString();

            // Check the full name of the [Report] attribute.
            if (attributeName == $"{Namespace}.{AttributeName}")
            {
                return (classDeclarationSyntax, true);
            }
        }

        return (classDeclarationSyntax, false);
    }
}