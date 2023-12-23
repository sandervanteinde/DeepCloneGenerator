namespace CloneGenerator.Tests;

public partial class PropertyCloningTests
{
    [Fact]
    public void Test()
    {
        var sut = new PropertyTest { Input = "Hello, world!" };

        var clone = sut.DeepClone();
    }

    [GenerateClone]
    public partial class PropertyTest
    {
        public string Input { get; init; }
    }
}