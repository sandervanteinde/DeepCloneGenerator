using System.Diagnostics.CodeAnalysis;

namespace DeepCloneGenerator.Tests;

public partial class GenericDeepCloneableTest
{
    [Fact]
    public void Test()
    {
        var original = new TestClassReferencingGeneric
        {
            Value = new()
            {
                Value = 1
            }
        };

        var clone = original.DeepClone();

        clone.Should()
            .BeExactClone(original);
    }
    
    [GenerateDeepClone]
    private partial class TestClassReferencingGeneric
    {
        public required ReferenceWithGenericSourceGenerator<int> Value { get; init; }
    }



    private class ReferenceWithGenericSourceGenerator<T> : ISourceGeneratedCloneableWithGenerics<ReferenceWithGenericSourceGenerator<T>, T>
    {
        public required T Value { get; init; }
        public ReferenceWithGenericSourceGenerator(){ }

        [SetsRequiredMembers]
        protected ReferenceWithGenericSourceGenerator(ReferenceWithGenericSourceGenerator<T> original, Func<T, T> mapper0)
        {
            Value = mapper0.Invoke(original.Value);
        }
        public ReferenceWithGenericSourceGenerator<T> DeepClone(Func<T, T> arg1)
        {
            return new ReferenceWithGenericSourceGenerator<T>(this, arg1);
        }
    }
}