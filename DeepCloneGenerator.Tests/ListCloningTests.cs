using DeepCloneGenerator;

namespace DeepCloneGenerator.Tests;

public partial class CloningTests
{
    [Fact]
    public void Test()
    {
        var original = new ClassWithList
        {
            Numbers = [1, 2, 3, 4]
        };

        var clone = original.DeepClone();

        clone.Should()
            .BeExactClone(original);
    }

    [GenerateClone]
    public partial class ClassWithList
    {
        public required List<int> Numbers { get; init; }
    }
}