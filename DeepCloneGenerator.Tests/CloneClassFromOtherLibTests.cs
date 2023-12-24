using DeepCloneGenerator.DummyLib;

namespace DeepCloneGenerator.Tests;

public partial class CloneClassFromOtherLibTests
{
    [Fact]
    public void Test()
    {
        var original = new ClassContainingOtherLibClass
        {
            Dummy = new DummyClass
            {
                Value = "Hello, world!"
            }
        };

        var clone = original.DeepClone();

        clone.Should()
            .BeExactClone(original);
    }

    [GenerateDeepClone]
    public partial class ClassContainingOtherLibClass
    {
        public required DummyClass Dummy { get; init; }
    }
}