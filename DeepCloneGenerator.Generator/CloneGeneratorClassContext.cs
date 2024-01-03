using System.CodeDom.Compiler;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static DeepCloneGenerator.CloneGenerator;

namespace DeepCloneGenerator;

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
        var simpleTypeName = _classSymbol.Name;
        var typeName = ClassSymbolName();
        _writer.WriteLine("#nullable enable");
        _writer.WriteLineNoTabs("#pragma warning disable CS8618");

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

                    _writer.WriteDebugCommentLine($"itemType {itemType}");
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

        _writer.Write($"partial {type} {typeName}");

        _writer.WriteInLineParameterValue(_classSymbol.IsAbstract, nameof(_classSymbol.IsAbstract));

        if (!_classSymbol.IsAbstract)
        {
            _writer.Write($" : {Namespace}.{InterfaceName}<{typeName}>");

            if (_classSymbol.IsGenericType)
            {
                _writer.Write(", ");
                _writer.Write($"{Namespace}.{GenericInterfaceName}<{typeName}, {string.Join(", ", _classSymbol.TypeArguments.Select(c => c.Name))}>");
            }
        }

        _writer.WriteLine();
        _writer.WriteLine(value: '{');
        _writer.Indent++;

        var hasCtorDefined = HasCtorDefined(_classSymbol);
        _writer.WriteParameterValue(hasCtorDefined, nameof(hasCtorDefined));

        if (!HasCtorDefined(_classSymbol))
        {
            _writer.WriteLine($"public {simpleTypeName}() {{ }}");
            _writer.WriteLine();
        }

        var hasRequiredMembers = HasRequiredMembers(_classSymbol);
        _writer.WriteParameterValue(hasRequiredMembers, nameof(hasRequiredMembers));

        if (hasRequiredMembers)
        {
            _writer.WriteLine("[System.Diagnostics.CodeAnalysis.SetsRequiredMembers]");
        }

        var ctorAccessibility = _classSymbol.IsSealed
            ? "private"
            : "protected";
        _writer.Write($"{ctorAccessibility} {simpleTypeName}({typeName} {CtorVariableName}");

        ForEachTypeArgument(
            (typeArgument, index) => { _writer.Write($", {GenericTypeMapperType(typeArgument)} {GenericTypeMapperArgumentName(index)}"); }
        );

        _writer.WriteLine(value: ')');

        var hasBaseClass = TryGetBaseClass(out var baseClass);
        var isBaseClassDeepCloneable = hasBaseClass && IsTypeDeepCloneable(baseClass!);

        _writer.WriteParameterValue(isBaseClassDeepCloneable, nameof(isBaseClassDeepCloneable));

        if (isBaseClassDeepCloneable)
        {
            _writer.Indent++;
            WriteBaseCloneableInvocation(CtorVariableName, baseClass!);
            _writer.Indent--;
        }

        _writer.WriteLine(value: '{');
        _writer.Indent++;
        var nonCompilerGeneratedMembers = _classSymbol.GetMembers()
            .Where(symbol => symbol.CanBeReferencedByName);

        if (hasBaseClass && !isBaseClassDeepCloneable)
        {
            while (baseClass is not null)
            {
                var accessibleBaseClassMembers = baseClass.GetMembers()
                    .Where(symbol => symbol.CanBeReferencedByName && symbol.DeclaredAccessibility is not Accessibility.Private);
                nonCompilerGeneratedMembers = nonCompilerGeneratedMembers.Concat(accessibleBaseClassMembers);
                baseClass = baseClass.BaseType;
            }
        }

        foreach (var member in nonCompilerGeneratedMembers)
        {
            if (member.GetAttributes()
                .Any(c => c.AttributeClass?.Name == IgnoreCloneAttribute))
            {
                _writer.WriteDebugCommentLine($"Member {member.Name} skipped due to {IgnoreCloneAttribute}");
                continue;
            }

            var syntaxNode = member.DeclaringSyntaxReferences
                .FirstOrDefault()
                ?.GetSyntax();

            switch (member, syntaxNode)
            {
                case (IFieldSymbol { CanBeReferencedByName: true, Type: var returnType } field, _):
                    WriteAssignment(field, returnType);
                    break;
                case (IPropertySymbol { Type: var returnType, IsAbstract: false } propertySymbol, PropertyDeclarationSyntax
                {
                    AccessorList: not null
                }):
                {
                    if (propertySymbol.IsAutoProperty())
                    {
                        WriteAssignment(propertySymbol, returnType);
                    }

                    break;
                }
                case (IPropertySymbol { Type: var returnType, IsAbstract: false } propertySymbol, ParameterSyntax):
                    if (propertySymbol.IsAutoProperty())
                    {
                        WriteAssignment(propertySymbol, returnType);
                    }

                    break;
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

        if (!_classSymbol.IsAbstract)
        {
            _writer.Write("public ");

            if (type == "class" && !_classSymbol.IsGenericType)
            {
                _writer.Write(
                    hasBaseClass && isBaseClassDeepCloneable && baseClass?.IsAbstract is not true
                    && baseClass?.TypeArguments.Length == _classSymbol.TypeArguments.Length
                        ? "override "
                        : "virtual "
                );
            }

            _writer.Write($"{typeName} {CloneMethodName}(");

            ForEachTypeArgument(
                (typeArgument, index) =>
                {
                    if (index > 0)
                    {
                        _writer.Write(", ");
                    }

                    _writer.Write($"{GenericTypeMapperType(typeArgument)} {GenericTypeMapperArgumentName(index)}");
                }
            );
            _writer.WriteLine(value: ')');
            _writer.WriteLine(value: '{');
            _writer.Indent++;
            _writer.Write($"return new {typeName}(this");
            ForEachTypeArgument(
                (_, index) =>
                {
                    _writer.Write(", ");
                    _writer.Write($"mapper{index}");
                }
            );
            _writer.WriteLine(");");
            _writer.Indent--;
            _writer.WriteLine(value: '}');

            if (_classSymbol.IsGenericType)
            {
                _writer.WriteLine($"{typeName} {Namespace}.{InterfaceName}<{typeName}>.{CloneMethodName}()");
                _writer.WriteLine(value: '{');
                _writer.Indent++;
                _writer.Write($"return {CloneMethodName}(");
                ForEachTypeArgument(
                    (_, index) =>
                    {
                        if (index > 0)
                        {
                            _writer.Write(", ");
                        }

                        _writer.Write("static self => self");
                    }
                );
                _writer.WriteLine(");");
                _writer.Indent--;
                _writer.WriteLine("}");
            }
        }

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
            .Append(simpleTypeName);

        var fileName = string.Join(".", fileNameElements);

        var sourceCode = _mandatoryNamespaces.Count == 0
            ? _stringWriter.ToString()
            : $"""
               {string.Join(_stringWriter.NewLine, _mandatoryNamespaces.OrderBy(c => c).Select(c => $"using {c};"))}

               {_stringWriter}
               """;

        ctx.AddSource(
            $"{fileName.Replace(oldChar: '<', newChar: '{').Replace(oldChar: '>', newChar: '}')}.g.cs",
            sourceCode
        );
    }

    private void WriteBaseCloneableInvocation(string ctorVariableName, INamedTypeSymbol namedTypeSymbol)
    {
        if (namedTypeSymbol.TypeArguments.Length == 0)
        {
            _writer.WriteLine($": base({ctorVariableName})");
            return;
        }

        _writer.Write($": base({ctorVariableName}");

        for (var i = 0; i < namedTypeSymbol.TypeArguments.Length; i++)
        {
            _writer.WriteLine($", static mapper{i} =>");
            _writer.WriteLine(value: '{');
            _writer.Indent++;
            var variableName = WriteCloneLogic($"mapper{i}", namedTypeSymbol.TypeArguments[i]);
            _writer.WriteLine($"return {variableName};");
            _writer.Indent--;
            _writer.Write(value: '}');
        }

        _writer.WriteLine(value: ')');
    }

    private void ForEachTypeArgument(Action<ITypeSymbol, int> callback)
    {
        if (!_classSymbol.IsGenericType)
        {
            return;
        }

        for (var index = 0; index < _classSymbol.TypeArguments.Length; index++)
        {
            var typeArgument = _classSymbol.TypeArguments[index];
            callback.Invoke(typeArgument, index);
        }
    }

    private string ClassSymbolName()
    {
        if (!_classSymbol.IsGenericType)
        {
            return _classSymbol.Name;
        }

        return $"{_classSymbol.Name}<{string.Join(", ", _classSymbol.TypeArguments.Select(c => c.Name))}>";
    }

    private bool TryGetBaseClass(out INamedTypeSymbol? namedTypeSymbol)
    {
        // object is the only type without a base type, so if this is object this will not match
        if (_classSymbol is { BaseType: { ContainingNamespace.Name: not "System", Name: not "Object" and not "ValueType" } actualBaseType })
        {
            namedTypeSymbol = actualBaseType;
            return true;
        }

        namedTypeSymbol = default!;
        return false;
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

    private static string GenericTypeMapperType(ITypeSymbol type)
    {
        return $"Func<{type}, {type}>";
    }

    private static string GenericTypeMapperArgumentName(int index)
    {
        return $"mapper{index}";
    }

    private string GetGenericTypeMapperVariableName(ITypeParameterSymbol typeSymbol)
    {
        return GenericTypeMapperArgumentName(_classSymbol.TypeArguments.IndexOf(typeSymbol));
    }

    private string WriteCloneLogic(string variableName, ITypeSymbol returnType)
    {
        if (returnType is IArrayTypeSymbol arraySymbol)
        {
            return WriteArrayClone(variableName, arraySymbol);
        }

        if (IsDictionaryWithParameterlessConstructor(returnType, out var keyType, out var valueType))
        {
            return WriteDictionaryClone(variableName, (INamedTypeSymbol)returnType, keyType!, valueType!);
        }

        if (IsCollectionWithParameterlessConstructor(returnType, out var elementType))
        {
            return WriteCollectionClone(variableName, (INamedTypeSymbol)returnType, elementType!);
        }

        if (IsEnumerableType(returnType, out var enumerableType))
        {
            return WriteEnumerableClone($"(({enumerableType!.ToDisplayString()}){variableName})", enumerableType);
        }

        if (returnType is ITypeParameterSymbol typeParameter)
        {
            var mapperName = GetGenericTypeMapperVariableName(typeParameter);
            return $"{mapperName}.Invoke({variableName})";
        }

        if (IsGenericDeepCloneable(returnType, out var implementedInterface))
        {
            return WriteGenericDeepCloneLogic(variableName, implementedInterface!);
        }

        if (IsTypeDeepCloneable(returnType))
        {
            var isNullableType = returnType.NullableAnnotation is not NullableAnnotation.NotAnnotated;
            return isNullableType
                ? $"{variableName}?.{CloneMethodName}()"
                : $"{variableName}.{CloneMethodName}()";
        }

        return variableName;

        return $"{variableName}{(IsTypeDeepCloneable(returnType) 
            ? $"?.{CloneMethodName}()" 
            : string.Empty)}";
    }

    private string WriteGenericDeepCloneLogic(string variableName, INamedTypeSymbol implementedInterface)
    {
        _writer.WriteDebugCommentLine($"{implementedInterface.ToDisplayString()} detected");
        var typeArguments = implementedInterface.TypeArguments;
        var typeArgumentsVariables = new List<string>();

        foreach (var typeArgument in typeArguments)
        {
            var mapper = NextUniqueVariableName();
            var mapperArgument = NextUniqueVariableName();
            _writer.WriteLine($"var {mapper} = static ({typeArgument.ToDisplayString()} {mapperArgument}) =>");
            _writer.WriteLine(value: '{');
            _writer.Indent++;
            var cloneVariableName = WriteCloneLogic(mapperArgument, typeArgument);
            _writer.WriteLine($"return {cloneVariableName};");
            _writer.Indent--;
            _writer.WriteLine("};");
            typeArgumentsVariables.Add(mapper);
        }

        return $"{variableName}.DeepClone({string.Join(", ", typeArgumentsVariables)})";
    }

    private bool IsGenericDeepCloneable(ITypeSymbol returnType, out INamedTypeSymbol? typeSymbol)
    {
        typeSymbol = returnType.AllInterfaces
            .FirstOrDefault(c => c.Name is GenericInterfaceName);

        if (typeSymbol is not null)
        {
            return true;
        }

        // find if it is going to be generated
        var originalDefinition = returnType.OriginalDefinition.ToDisplayString();

        if (_context.ClassesInAssemblyGeneratingClone.Contains(originalDefinition))
        {
            typeSymbol = (INamedTypeSymbol)returnType;
            return true;
        }

        return false;
    }

    private bool IsTypeDeepCloneable(ITypeSymbol type)
    {
        // if (_classSymbol.Name == "Property")
        // {
        //     return false;
        // }
        var typeName = type.ToDisplayString();
        return _context.ClassesInAssemblyGeneratingClone.Contains(typeName)
            || type.AllInterfaces.Any(c => c.Name == InterfaceName)
            || (!type.Equals(type.OriginalDefinition, SymbolEqualityComparer.Default) && type.OriginalDefinition is INamedTypeSymbol { TypeArguments.Length: > 0 } && IsTypeDeepCloneable(type.OriginalDefinition));
    }

    private string WriteDictionaryClone(string variableName, INamedTypeSymbol collectionType, ITypeSymbol keyType, ITypeSymbol valueType)
    {
        var variableNameToAssign = NextUniqueVariableName();
        _writer.WriteLine($"var {variableNameToAssign} = new {collectionType.ToDisplayString()}();");
        var iteratorVariableName = NextUniqueVariableName();
        _writer.WriteLine($"foreach(var {iteratorVariableName} in {variableName})");
        _writer.WriteLine(value: '{');
        _writer.Indent++;
        var keyVariableName = WriteCloneLogic($"{iteratorVariableName}.Key", keyType);
        var valueVariableName = WriteCloneLogic($"{iteratorVariableName}.Value", valueType);
        _writer.WriteLine($"{variableNameToAssign}[{keyVariableName}] = {valueVariableName};");
        _writer.Indent--;
        _writer.WriteLine(value: '}');

        return variableNameToAssign;
    }

    private string WriteCollectionClone(string variableName, INamedTypeSymbol collectionType, ITypeSymbol elementType)
    {
        var variableNameToAssign = NextUniqueVariableName();
        _writer.WriteLine($"{collectionType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)} {variableNameToAssign};");
        _writer.WriteLine($"if({variableName} is null)");
        _writer.WriteLine('{');
        _writer.Indent++;
        _writer.WriteLine($"{variableNameToAssign} = null!;");
        _writer.Indent--;
        _writer.WriteLine('}');
        _writer.WriteLine("else");
        _writer.WriteLine('{');
        _writer.Indent++;
        _writer.WriteLine($"{variableNameToAssign} = new {collectionType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat.RemoveMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier))}();");
        var iteratorVariableName = NextUniqueVariableName();
        _writer.WriteLine($"foreach(var {iteratorVariableName} in {variableName})");
        _writer.WriteLine(value: '{');
        _writer.Indent++;
        var elementVariableName = WriteCloneLogic(iteratorVariableName, elementType);
        _writer.WriteLine($"{variableNameToAssign}.Add({elementVariableName});");
        _writer.Indent--;
        _writer.WriteLine(value: '}');
        _writer.Indent--;
        _writer.WriteLine(value: '}');

        return variableNameToAssign;
    }

    private string WriteArrayClone(string variableName, IArrayTypeSymbol arrayType)
    {
        var elementType = arrayType.ElementType;
        var variableNameToAssign = NextUniqueVariableName();
        var depth = GetDepth(elementType);

        var elementTypeAsName = elementType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)
            .TrimEnd('[', ']');
        var lengths = new List<string>(arrayType.Rank);

        _writer.WriteLine($"{arrayType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)} {variableNameToAssign};");
        _writer.WriteLine($"if(ReferenceEquals({variableName}, null))");
        _writer.WriteLine(value: '{');
        _writer.Indent++;
        _writer.WriteLine($"{variableNameToAssign} = null!;");
        _writer.Indent--;
        _writer.WriteLine(value: '}');
        _writer.WriteLine("else");
        _writer.WriteLine('{');
        _writer.Indent++;

        for (var i = 0; i < arrayType.Rank; i++)
        {
            var lengthVariableName = NextUniqueVariableName();
            _writer.WriteLine($"var {lengthVariableName} = {variableName}.GetLength({i});");
            lengths.Add(lengthVariableName);
        }

        _writer.Write($"{variableNameToAssign} = new {elementTypeAsName}[{string.Join(", ", lengths)}]");

        var additionalBracketCount = depth - 1;

        for (var j = 0; j < additionalBracketCount; j++)
        {
            _writer.Write(value: "[]");
        }

        var iteratorNames = lengths
            .Select(_ => NextUniqueVariableName())
            .ToList();

        _writer.WriteLine(value: ';');

        for (var i = 0; i < iteratorNames.Count; i++)
        {
            var iteratorName = iteratorNames[i];
            _writer.WriteLine($"for(var {iteratorName} = 0; {iteratorName} < {lengths[i]}; {iteratorName}++)");
            _writer.WriteLine(value: '{');
            _writer.Indent++;
        }

        var index = string.Join(", ", iteratorNames);
        var elementVariableName = WriteCloneLogic($"{variableName}[{index}]", elementType);
        _writer.WriteLine($"{variableNameToAssign}[{index}] = {elementVariableName};");

        for (var i = 0; i < iteratorNames.Count; i++)
        {
            _writer.Indent--;
            _writer.WriteLine(value: '}');
        }

        _writer.Indent--;
        _writer.WriteLine(value: '}');

        return variableNameToAssign;

        static int GetDepth(ITypeSymbol currentElementType)
        {
            var currentElement = currentElementType;
            var count = 1;

            while (currentElement is IArrayTypeSymbol innerArray)
            {
                count++;
                currentElement = innerArray.ElementType;
            }

            return count;
        }
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

    private static bool IsDictionaryWithParameterlessConstructor(ITypeSymbol symbol, out ITypeSymbol? keyType, out ITypeSymbol? elementType)
    {
        keyType = null;
        elementType = null;

        if (symbol is not INamedTypeSymbol namedTypeSymbol)
        {
            return false;
        }

        if (!namedTypeSymbol.Constructors.Any(c => c.Parameters.Length == 0 && c.DeclaredAccessibility == Accessibility.Public))
        {
            return false;
        }

        var collectionInterface = namedTypeSymbol.AllInterfaces.FirstOrDefault(
            c => c.ToDisplayString()
                .StartsWith("System.Collections.Generic.IDictionary")
        );

        if (collectionInterface is null)
        {
            elementType = null;
            return false;
        }

        keyType = collectionInterface.TypeArguments[index: 0];
        elementType = collectionInterface.TypeArguments[index: 1];
        return true;
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
            yield return $"temp{i++}";
        }
    }

    private static bool HasCtorDefined(INamedTypeSymbol symbol)
    {
        return symbol.Constructors.Any(c => c.DeclaringSyntaxReferences.Length > 0);
    }

    private static bool HasRequiredMembers(INamespaceOrTypeSymbol symbol)
    {
        return symbol.GetMembers()
                .Any(member => member is IFieldSymbol { IsRequired: true } or IPropertySymbol { IsRequired: true })
            || (symbol is ITypeSymbol { BaseType: { } baseType } && HasRequiredMembers(baseType));
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