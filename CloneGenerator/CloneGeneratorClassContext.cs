using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static CloneGenerator.CloneGenerator;

namespace CloneGenerator;

public class CloneGeneratorClassContext : IDisposable
{
    private readonly INamedTypeSymbol _classSymbol;
    private readonly CloneGeneratorContext _context;
    private readonly HashSet<string> _mandatoryNamespaces = new();
    private readonly StringWriter _stringWriter;

    private readonly IEnumerator<string> _uniqueVariableNames = UniqueVariableNames()
        .GetEnumerator();

    private readonly IndentedTextWriter _writer;

    public CloneGeneratorClassContext(CloneGeneratorContext context, INamedTypeSymbol classSymbol)
    {
        _context = context;
        _classSymbol = classSymbol;
        _stringWriter = new StringWriter();
        var writer = new IndentedTextWriter(_stringWriter);
        _writer = writer;
    }

    public void Dispose()
    {
        _writer.Dispose();
        _uniqueVariableNames.Dispose();
    }

    public void Do(ref SourceProductionContext ctx)
    {
        var className = _classSymbol.Name;
        var hierarchy = EnumerateParentHierarchy(_classSymbol);

        foreach (var item in hierarchy)
        {
            switch (item)
            {
                case INamespaceSymbol:
                    _writer.WriteLine($"namespace {item.ToDisplayString()}");
                    _writer.WriteLine(value: '{');
                    _writer.Indent++;
                    continue;
                case ITypeSymbol { TypeKind: TypeKind.Class } typeSymbol:
                {
                    if (!TryInterpretType(typeSymbol, out var itemType))
                    {
                        return;
                    }

                    _writer.WriteLine($"partial {itemType} {item.Name}");
                    _writer.WriteLine(value: '{');
                    _writer.Indent++;
                    continue;
                }
                default:
                    return;
            }
        }

        if (!TryInterpretType(_classSymbol, out var type))
        {
            return;
        }

        _writer.WriteLine($"partial {type} {className} : {Namespace}.{InterfaceName}<{className}>");
        _writer.WriteLine(value: '{');
        _writer.Indent++;

        if (!HasCtorDefined(_classSymbol))
        {
            _writer.WriteLine($"public {className}() {{ }}");
            _writer.WriteLine();
        }

        if (HasRequiredMembers(_classSymbol))
        {
            _writer.WriteLine("[System.Diagnostics.CodeAnalysis.SetsRequiredMembers]");
        }

        _writer.WriteLine($"private {className}({className} {CtorVariableName})");
        _writer.WriteLine(value: '{');
        _writer.Indent++;
        var nonCompilerGeneratedMembers = _classSymbol.GetMembers()
            .Where(symbol => symbol.CanBeReferencedByName);

        foreach (var member in nonCompilerGeneratedMembers)
        {
            var syntaxNode = member.DeclaringSyntaxReferences
                .FirstOrDefault()
                ?.GetSyntax();

            switch (member, syntaxNode)
            {
                case (IFieldSymbol { CanBeReferencedByName: true, Type: var returnType } field, _):
                    WriteAssignment(field, returnType);
                    break;
                case (IPropertySymbol { GetMethod: not null, Type: var returnType } propertySymbol, PropertyDeclarationSyntax { AccessorList: not null }):
                {
                    WriteAssignment(propertySymbol, returnType);
                    break;
                }
                default:
#if DEBUG
                    Console.WriteLine($"Class {_classSymbol.Name} skipped generation of field {member.Name}");
#endif
                    break;
            }
        }

        _writer.Indent--;
        _writer.WriteLine(value: '}');
        _writer.WriteLine();
        _writer.WriteLine($"public {className} {CloneMethodName}()");
        _writer.WriteLine(value: '{');
        _writer.Indent++;
        _writer.WriteLine($"return new {className}(this);");
        _writer.Indent--;
        _writer.WriteLine(value: '}');
        _writer.Indent--;
        _writer.WriteLine(value: '}');

        foreach (var _ in hierarchy)
        {
            _writer.Indent--;
            _writer.WriteLine(value: '}');
        }

        var fileNameElements = hierarchy
            .OfType<ITypeSymbol>()
            .Select(typeSymbol => typeSymbol.Name)
            .Append(className);

        var fileName = string.Join(".", fileNameElements);

        ctx.AddSource(
            $"{fileName}.g.cs",
            _mandatoryNamespaces.Count == 0
                ? _stringWriter.ToString()
                : $"""
                   {string.Join(_stringWriter.NewLine, _mandatoryNamespaces.OrderBy(c => c).Select(c => $"using {c};"))}

                   {_stringWriter}
                   """
        );
    }

    private IReadOnlyCollection<ISymbol> EnumerateParentHierarchy(ISymbol symbol)
    {
        var linkedList = new LinkedList<ISymbol>();
        var containingType = _classSymbol.ContainingType;

        while (containingType is not null)
        {
            linkedList.AddFirst(containingType);
            containingType = containingType.ContainingType;
        }

        if (symbol.ContainingNamespace is not null)
        {
            linkedList.AddFirst(symbol.ContainingNamespace);
        }

        return linkedList;
    }

    private void WriteAssignment(ISymbol symbol, ITypeSymbol returnType)
    {
        var variableName = $"{CtorVariableName}.{symbol.Name}";

        var variableNameToAssign = WriteCloneLogic(variableName, returnType);
        _writer.Write("this.");
        _writer.Write(symbol.Name);
        _writer.Write(" = ");
        _writer.Write(variableNameToAssign);
        _writer.WriteLine(value: ';');
    }

    private string NextUniqueVariableName()
    {
        if (!_uniqueVariableNames.MoveNext())
        {
            throw new InvalidOperationException("To many unique variable names requested");
        }

        return _uniqueVariableNames.Current!;
    }

    private string WriteCloneLogic(string variableName, ITypeSymbol returnType)
    {
        if (returnType is IArrayTypeSymbol arraySymbol)
        {
            return WriteArrayClone(variableName, arraySymbol.ElementType);
        }

        if (IsCollectionWithParameterlessConstructor(returnType, out var elementType))
        {
            return WriteCollectionClone(variableName, (INamedTypeSymbol)returnType, elementType!);
        }

        if (IsEnumerableType(returnType, out var enumerableType))
        {
            return WriteEnumerableClone($"(({enumerableType!.ToDisplayString()}){variableName})", enumerableType);
        }

        var returnTypeName = returnType.ToDisplayString();
        var isCloneMethodAvailable = _context.ClassesInAssemblyGeneratingClone.Contains(returnTypeName)
            || returnType.AllInterfaces.Any(c => c.Name == InterfaceName);
        return $"{variableName}{(isCloneMethodAvailable ? $"?.{CloneMethodName}()" : string.Empty)}";
    }

    private string WriteCollectionClone(string variableName, INamedTypeSymbol collectionType, ITypeSymbol elementType)
    {
        var variableNameToAssign = NextUniqueVariableName();
        _writer.WriteLine($"var {variableNameToAssign} = new {collectionType.ToDisplayString()}();");
        var iteratorVariableName = NextUniqueVariableName();
        _writer.WriteLine($"foreach(var {iteratorVariableName} in {variableName})");
        _writer.WriteLine(value: '{');
        _writer.Indent++;
        var elementVariableName = WriteCloneLogic(iteratorVariableName, elementType);
        _writer.WriteLine($"{variableNameToAssign}.Add({elementVariableName});");
        _writer.Indent--;
        _writer.WriteLine(value: '}');

        return variableNameToAssign;
    }

    private string WriteArrayClone(string variableName, ITypeSymbol elementType)
    {
        var variableNameToAssign = NextUniqueVariableName();
        var elementTypeAsName = elementType.ToDisplayString();
        _writer.WriteLine($"var {variableNameToAssign} = ReferenceEquals({variableName}, null) ? null : new {elementTypeAsName}[{variableName}.Length];");
        _writer.WriteLine($"for(var i = 0; i < {variableNameToAssign}?.Length; i++)");
        _writer.WriteLine(value: '{');
        _writer.Indent++;
        var elementVariableName = WriteCloneLogic($"{variableName}[i]", elementType);
        _writer.WriteLine($"{variableNameToAssign}[i] = {elementVariableName};");
        _writer.Indent--;
        _writer.WriteLine("}");
        return variableNameToAssign;
    }

    private void AddNamespace(string @namespace)
    {
        _mandatoryNamespaces.Add(@namespace);
    }

    private string WriteEnumerableClone(string variableName, INamedTypeSymbol enumerableType)
    {
        AddNamespace("System.Linq");

        var assignEnumerableVariable = NextUniqueVariableName();
        _writer.WriteLine($"var {assignEnumerableVariable} = {variableName};");
        var countVariable = NextUniqueVariableName();
        _writer.WriteLine($"_ = {assignEnumerableVariable}.TryGetNonEnumeratedCount(out var {countVariable});");
        var listVariable = NextUniqueVariableName();
        var elementType = enumerableType.TypeArguments.Single();
        var elementTypeName = elementType.ToDisplayString();
        _writer.WriteLine($"var {listVariable} = new System.Collections.Generic.List<{elementTypeName}>({countVariable});");
        var iteratorVariable = NextUniqueVariableName();
        _writer.WriteLine($"foreach(var {iteratorVariable} in {assignEnumerableVariable})");
        _writer.WriteLine(value: '{');
        _writer.Indent++;
        var resultVariable = WriteCloneLogic(iteratorVariable, elementType);
        _writer.WriteLine($"{listVariable}.Add({resultVariable});");
        _writer.Indent--;
        _writer.WriteLine(value: '}');
        return listVariable;
    }

    private static bool IsCollectionWithParameterlessConstructor(ITypeSymbol symbol, out ITypeSymbol? elementType)
    {
        if (symbol is not INamedTypeSymbol namedTypeSymbol)
        {
            elementType = null;
            return false;
        }

        if (!namedTypeSymbol.Constructors.Any(c => c.Parameters.Length == 0 && c.DeclaredAccessibility == Accessibility.Public))
        {
            elementType = null;
            return false;
        }

        var collectionInterface = namedTypeSymbol.AllInterfaces.FirstOrDefault(
            c => c.ToDisplayString()
                .StartsWith("System.Collections.Generic.ICollection")
        );

        if (collectionInterface is null)
        {
            elementType = null;
            return false;
        }

        elementType = collectionInterface.TypeArguments[index: 0];
        return true;
    }

    private static bool IsEnumerableType(ITypeSymbol symbol, out INamedTypeSymbol? enumerableType)
    {
        if (symbol.TypeKind != TypeKind.Interface)
        {
            enumerableType = null;
            return false;
        }

        if (symbol.ToDisplayString()
            .StartsWith("System.Collections.Generic.IEnumerable"))
        {
            enumerableType = (INamedTypeSymbol)symbol;
            return true;
        }

        var iface = symbol.AllInterfaces.FirstOrDefault(
            iface => iface.ToDisplayString()
                .StartsWith("System.Collections.Generic.IEnumerable")
        );

        enumerableType = iface;
        return enumerableType is not null;
    }

    private static IEnumerable<string> UniqueVariableNames()
    {
        var i = 1;

        while (i < 10_000)
        {
            yield return $"__codeGeneratedTemporaryVariable{i++}";
        }
    }

    private static bool HasCtorDefined(INamedTypeSymbol symbol)
    {
        return symbol.Constructors.Any(c => c.DeclaringSyntaxReferences.Length > 0);
    }

    private static bool HasRequiredMembers(INamespaceOrTypeSymbol symbol)
    {
        return symbol.GetMembers()
            .Any(member => member is IFieldSymbol { IsRequired: true } or IPropertySymbol { IsRequired: true });
    }

    private bool TryInterpretType(ITypeSymbol symbol, out string interpretedType)
    {
        return symbol switch
        {
            { TypeKind: TypeKind.Class, IsRecord: false } => SetAndReturnType("class", out interpretedType),
            { TypeKind: TypeKind.Class, IsRecord: true } => SetAndReturnType("record", out interpretedType),
            { TypeKind: TypeKind.Struct } => SetAndReturnType("struct", out interpretedType),
            _ => Fail(out interpretedType)
        };

        bool Fail(out string type)
        {
            type = string.Empty;
            return false;
        }

        bool SetAndReturnType(string valueToAssign, out string type)
        {
            type = valueToAssign;
            return true;
        }
    }
}