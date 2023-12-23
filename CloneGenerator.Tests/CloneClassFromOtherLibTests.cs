using CloneGenerator.DummyLib;

namespace CloneGenerator.Tests;

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

    [GenerateClone]
    public partial class ClassContainingOtherLibClass
    {
        public required DummyClass Dummy { get; init; }
    }
}