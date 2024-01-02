using DeepCloneGenerator.DummyLib;

namespace DeepCloneGenerator.Tests;

public partial class GenericTests
{
    [Fact]
    public void OneGenericTest()
    {
        var original = new OneGenericClass<int>
        {
            Value = 1
        };

        var clone = original.DeepClone(c => c);

        clone.Should()
            .BeExactClone(original);
    }

    [Fact]
    public void OneGenericTestWithDummyValue()
    {
        var original = new OneGenericClass<DummyClass>
        {
            Value = new DummyClass
            {
                Value = "Some value"
            }
        };

        var clone = original.DeepClone();

        clone.Should()
            .BeExactClone(original);
    }

    [Fact]
    public void ClassContainingGenericTest()
    {
        var original = new ClassContainingGeneric
        {
            Test = new()
            {
                Value = new()
                {
                    Value = "Test"
                }
            }
        };

        var clone = original.DeepClone();
        clone.Should()
            .BeExactClone(original);
    }

    [Fact]
    public void ClassExtendingGenericTest()
    {
        var original = new ClassExtendingGeneric
        {
            Value = 123
        };

        var clone = original.DeepClone();

        clone.Should()
            .BeExactClone(original);
    }

    [GenerateDeepClone]
    private partial class ClassExtendingGeneric : OneGenericClass<int>
    {
        public ClassExtendingGeneric()
        {
            
        }
    }

    [GenerateDeepClone]
    private partial class OneGenericClass<TValue>
    {
        public required TValue Value { get; init; }
    }

    [GenerateDeepClone]
    private partial class OnesGenericClass<TOne>
    {
        public required List<TOne> Ones { get; init; }
    }

    [GenerateDeepClone]
    private partial class ClassContainingGeneric
    {
        public required OneGenericClass<OneGenericClass<string>> Test { get; init; }
    }
}