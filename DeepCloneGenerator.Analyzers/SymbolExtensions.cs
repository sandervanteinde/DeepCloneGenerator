using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace DeepCloneGenerator.Analyzers;

public static class SymbolExtensions
{
    private const string DeepCloneAttribute = "GenerateDeepCloneAttribute";

    public static bool IsFieldInClassWithDeepCloneAttribute(this ISymbol symbol)
    {
        return symbol.ContainingType
            .GetAttributes()
            .HasDeepCloneAttribute();
    }

    public static bool HasDeepCloneAttribute(this ITypeSymbol typeSymbol)
    {
        return typeSymbol.GetAttributes()
            .HasDeepCloneAttribute();
    }

    private static bool HasDeepCloneAttribute(this ImmutableArray<AttributeData> attributes)
    {
        
        return attributes.Any(a => a.AttributeClass?.Name is DeepCloneAttribute);
        
    }
}