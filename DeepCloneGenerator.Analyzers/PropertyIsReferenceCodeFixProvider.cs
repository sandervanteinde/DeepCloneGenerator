using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DeepCloneGenerator.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(PropertyIsReferenceCodeFixProvider))]
[Shared]
public class PropertyIsReferenceCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(PropertyIsReferenceWithoutCloneAnalyzer.DiagnosticId);
    
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics.Single();

        var diagnosticSpan = diagnostic.Location.SourceSpan;
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
            .ConfigureAwait(false);

        var diagnosticNode = root?.FindNode(diagnosticSpan);
        var type = diagnosticNode switch
        {
            PropertyDeclarationSyntax property => property.Type,
            _ => null
        };

        if (type is null)
        {
            return;
        }
        
        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Add [GenerateDeepClone] attribute",
                createChangedDocument: c => AddAttribute(context.Document, type, c)
            ),
            diagnostic
        );

    }

    private Task<Document> AddAttribute(Document document, TypeSyntax type, CancellationToken cancellationToken)
    {
        foreach (var doc in document.Project.Solution.Projects.SelectMany(proj => proj.Documents))
        {
            
        }
        return Task.FromResult(document);
    }
}