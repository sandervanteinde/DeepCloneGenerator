using System;
using System.CodeDom.Compiler;
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
        var returnTypeName = returnType.ToDisplayString();
        var isCloneMethodAvailable = _context.ClassesInAssemblyGeneratingClone.Contains(returnTypeName)
            || returnType.AllInterfaces.Any(c => c.Name == InterfaceName);

        _writer.WriteLine($"this.{symbol.Name} = {CtorVariableName}.{symbol.Name}{(isCloneMethodAvailable ? $".{CloneMethodName}()" : string.Empty)};");
    }

    public void Dispose()
    {
        _writer.Dispose();
    }
}