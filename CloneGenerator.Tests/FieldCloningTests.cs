using FluentAssertions;

namespace CloneGenerator.Tests;

public partial class FieldCloningTests
{
    [Fact]
    public void Test()
    {
        var original = new FieldTests
        {
            MyField = "Hello, world!"
        };

        var clone = original.DeepClone();

        clone.Should()
            .BeEquivalentTo(original);
    }

    [GenerateClone]
    private partial class FieldTests
    {
        public string MyField;
    }
}