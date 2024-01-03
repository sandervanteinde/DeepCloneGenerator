using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DeepCloneGenerator;

public static class PropertySymbolExtensions
{
    public static bool IsAutoProperty(this IPropertySymbol property)
    {
        if (property is not { GetMethod: { } getMethod })
        {
            return false;
        }

        return getMethod.DeclaringSyntaxReferences
            .Select(reference => reference.GetSyntax())
            .OfType<AccessorDeclarationSyntax>()
            .All(accessor => accessor.Body == null && accessor.ExpressionBody == null);

    }
}