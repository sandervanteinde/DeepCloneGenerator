using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static CloneGenerator.CloneGenerator;

namespace CloneGenerator;

public class CloneGeneratorClassContext : IDisposable
{
    private readonly CloneGeneratorContext _context;
    private readonly INamedTypeSymbol _classSymbol;
    private readonly StringWriter _stringWriter;
    private readonly IndentedTextWriter _writer;
    private readonly IEnumerator<string> _uniqueVariableNames = UniqueVariableNames().GetEnumerator();

    public CloneGeneratorClassContext(CloneGeneratorContext context, INamedTypeSymbol classSymbol)
    {
        _context = context;
        _classSymbol = classSymbol;
        _stringWriter = new StringWriter();
        var writer = new IndentedTextWriter(_stringWriter)
        {
            Indent = 3
        };
        _writer = writer;
    }

    public void Do(SourceProductionContext ctx)
    {
        foreach (var member in _classSymbol.GetMembers()
                     .Where(symbol => symbol.CanBeReferencedByName))
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

        var className = _classSymbol.Name;
        var @namespace = _classSymbol.ContainingNamespace.ToDisplayString();

        ctx.AddSource(
            $"{className}.g.cs",
            $$"""
              namespace {{@namespace}}
              {
                  partial class {{className}} : {{Namespace}}.{{InterfaceName}}<{{className}}>
                  {
                      private {{className}}({{className}} {{CtorVariableName}})
                      {
                          {{_stringWriter}}
                      }
                      public {{className}} Clone()
                      {
                          return new {{className}}(this);
                      }
                  }
              }
              """
        );
    }

    private void WriteAssignment(ISymbol symbol, ITypeSymbol returnType)
    {
        var variableName = $"{CtorVariableName}.{symbol.Name}";
        
        var variableNameToAssign = WriteCloneLogic(variableName, returnType);
        _writer.Write("this.");
        _writer.Write(symbol.Name);
        _writer.Write(" = ");
        _writer.Write(variableNameToAssign);
        _writer.WriteLine(';');
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
            var variableNameToAssign = NextUniqueVariableName();
            var elementTypeAsName = arraySymbol.ElementType.ToDisplayString();
            _writer.WriteLine($"var {variableNameToAssign} = {variableName} is null ? null : new {elementTypeAsName}[{variableName}.Length];");
            _writer.WriteLine($"for(var i = 0; i < {variableNameToAssign}?.Length; i++)");
            _writer.WriteLine('{');
            _writer.Indent++;
            var elementVariableName = WriteCloneLogic($"{variableName}[i]", arraySymbol.ElementType);
            _writer.WriteLine($"{variableNameToAssign}[i] = {elementVariableName};");
            _writer.Indent--;
            _writer.WriteLine("}");
            return variableNameToAssign;
        }
        var returnTypeName = returnType.ToDisplayString();
        var isCloneMethodAvailable = _context.ClassesInAssemblyGeneratingClone.Contains(returnTypeName)
            || returnType.AllInterfaces.Any(c => c.Name == InterfaceName);
        return $"{variableName}{(isCloneMethodAvailable ? $"?.{CloneMethodName}()" : string.Empty)}";
    }

    private static IEnumerable<string> UniqueVariableNames()
    {
        var i = 1;

        while (i < 10_000)
        {
            yield return $"__codeGeneratedTemporaryVariable{i++}";
        }
    }

    public void Dispose()
    {
        _writer.Dispose();
        _uniqueVariableNames.Dispose();
    }
}