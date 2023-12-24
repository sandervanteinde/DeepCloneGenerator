using DeepCloneGenerator;

namespace DeepCloneGenerator.Tests;

public partial class ArrayCloningTests
{
    [Fact]
    public void Test()
    {
        var original = new ClassWithArray
        {
            Integers = new[] { 1, 2, 3, 4, 5, 6 }
        };

        var clone = original.DeepClone();

        clone.Should()
            .BeExactClone(original);
    }

    [GenerateClone]
    private partial class ClassWithArray
    {
        public int[] Integers { get; init; }
    }
}