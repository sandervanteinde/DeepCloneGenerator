using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DeepCloneGenerator;

[Generator]
public class CloneGenerator : IIncrementalGenerator
{
    internal const string Namespace = "DeepCloneGenerator";
    internal const string AttributeName = "GenerateDeepCloneAttribute";
    internal const string IgnoreCloneAttribute = "CloneIgnoreAttribute";

    internal const string InterfaceName = "ISourceGeneratedCloneable";
    internal const string GenericInterfaceName = "ISourceGeneratedCloneableWithGenerics";

    internal const string CtorVariableName = "original";
    internal const string CloneMethodName = "DeepClone";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
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