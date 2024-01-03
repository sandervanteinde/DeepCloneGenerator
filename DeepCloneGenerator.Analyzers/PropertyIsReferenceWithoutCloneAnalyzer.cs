using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DeepCloneGenerator.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class PropertyIsReferenceWithoutCloneAnalyzer : DiagnosticAnalyzer
{
    internal const string DiagnosticId = "DEEPCL0001";
    private const string Title = "Property is refenrece without deep clone attribute";

    private const string MessageFormat = "{0} has the [GenerateDeepClone] attribute but member {1} is of type {2}  without the [GenerateDeepClone] resulting in a copy by reference instead of deep clone";
    private const string Description = "Add the [GenerateDeepClone] attribute to classes which are referenced by other classes which have the [GenerateDeepClone] attribute.";
    private const string Category = "Design";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId, Title, MessageFormat, Category,
        DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);
    
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        
        context.EnableConcurrentExecution();
        
        context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.Property, SymbolKind.Field);
    }

    private void AnalyzeSymbol(SymbolAnalysisContext context)
    {
        if (!context.Symbol.IsFieldInClassWithDeepCloneAttribute())
        {
            return;
        }

        var type = context.Symbol switch
        {
            IPropertySymbol propertySymbol => propertySymbol.Type,
            IFieldSymbol fieldSymbol => fieldSymbol.Type,
            _ => throw new NotSupportedException("Only property and field symbols should pass here")
        };

        if (!type.IsReferenceType || type.HasDeepCloneAttribute())
        {
            return;
        }

        var isSpecialTypeWhichShouldBeIgnored = type.OriginalDefinition.SpecialType switch
        {
            SpecialType.None => false,
            SpecialType.System_Object => false,
            SpecialType.System_Enum => true,
            SpecialType.System_MulticastDelegate => true,
            SpecialType.System_Delegate => true,
            SpecialType.System_ValueType => true,
            SpecialType.System_Void => true,
            SpecialType.System_Boolean => true,
            SpecialType.System_Char => true,
            SpecialType.System_SByte => true,
            SpecialType.System_Byte => true,
            SpecialType.System_Int16 => true,
            SpecialType.System_UInt16 => true,
            SpecialType.System_Int32 => true,
            SpecialType.System_UInt32 => true,
            SpecialType.System_Int64 => true,
            SpecialType.System_UInt64 => true,
            SpecialType.System_Decimal => true,
            SpecialType.System_Single => true,
            SpecialType.System_Double => true,
            SpecialType.System_String => true,
            SpecialType.System_IntPtr => true,
            SpecialType.System_UIntPtr => true,
            SpecialType.System_Array => true,
            SpecialType.System_Collections_IEnumerable => false,
            SpecialType.System_Collections_Generic_IEnumerable_T => true,
            SpecialType.System_Collections_Generic_IList_T => true,
            SpecialType.System_Collections_Generic_ICollection_T => true,
            SpecialType.System_Collections_IEnumerator => true,
            SpecialType.System_Collections_Generic_IEnumerator_T => true,
            SpecialType.System_Collections_Generic_IReadOnlyList_T => true,
            SpecialType.System_Collections_Generic_IReadOnlyCollection_T => true,
            SpecialType.System_Nullable_T => true,
            SpecialType.System_DateTime => true,
            SpecialType.System_Runtime_CompilerServices_IsVolatile => false,
            SpecialType.System_IDisposable => false,
            SpecialType.System_TypedReference => false,
            SpecialType.System_ArgIterator => false,
            SpecialType.System_RuntimeArgumentHandle => false,
            SpecialType.System_RuntimeFieldHandle => false,
            SpecialType.System_RuntimeMethodHandle => false,
            SpecialType.System_RuntimeTypeHandle => false,
            SpecialType.System_IAsyncResult => false,
            SpecialType.System_AsyncCallback => false,
            SpecialType.System_Runtime_CompilerServices_RuntimeFeature => false,
            SpecialType.System_Runtime_CompilerServices_PreserveBaseOverridesAttribute => false
        };

        if (isSpecialTypeWhichShouldBeIgnored)
        {
            return;
        }
            

        var diagnostic = Diagnostic.Create(Rule, context.Symbol.Locations.First(), context.Symbol.ContainingType.Name, context.Symbol.Name, type.Name);
        context.ReportDiagnostic(diagnostic);

    }
}