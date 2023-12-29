using DeepCloneGenerator.DummyLib;

namespace DeepCloneGenerator.Tests;

public partial class GenericTests
{
    [Fact]
    public void OneGenericTest()
    {
        var original = new OneGenericClass<int>
        {
            One = 1
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
            One = new DummyClass
            {
                Value = "Some value"
            }
        };

        var clone = original.DeepClone();

        clone.Should()
            .BeExactClone(original);
    }

    [GenerateDeepClone]
    private partial class OneGenericClass<TOne>
    {
        public required TOne One { get; init; }
    }

    [GenerateDeepClone]
    private partial class OnesGenericClass<TOne>
    {
        public required List<TOne> Ones { get; init; }
    }
}