namespace DeepCloneGenerator.Tests;

public partial class PropertyCloningTests
{
    [Fact]
    public void Test()
    {
        var original = new PropertyTest { Input = "Hello, world!" };

        var clone = original.DeepClone();

        clone.Should()
            .BeExactClone(original);
    }

    [GenerateDeepClone]
    public partial class PropertyTest
    {
        public string Input { get; init; }
    }
}