using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace DeepCloneGenerator;

public class CloneGeneratorContext
{
    private readonly IReadOnlyCollection<INamedTypeSymbol> _classSymbols;

    public CloneGeneratorContext(IReadOnlyCollection<INamedTypeSymbol> classSymbols, Compilation compilation)
    {
        _classSymbols = classSymbols;
        Compilation = compilation;
        var classesInAssemblyGeneratingClone = classSymbols
            .Select(c => c.ToDisplayString())
            .ToImmutableHashSet();
        ClassesInAssemblyGeneratingClone = classesInAssemblyGeneratingClone;
    }

    public Compilation Compilation { get; }
    public IReadOnlyCollection<string> ClassesInAssemblyGeneratingClone { get; }

    public void Do(ref SourceProductionContext ctx)
    {
        foreach (var classSymbol in _classSymbols)
        {
            using var classContext = new CloneGeneratorClassContext(this, classSymbol);
            classContext.Do(ref ctx);
        }
    }
}