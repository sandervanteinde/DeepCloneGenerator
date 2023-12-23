using FluentAssertions;

namespace CloneGenerator.Tests;

public partial class FieldCloningTests
{
    [Fact]
    public void Test()
    {
        var field = new FieldTests
        {
            MyField = "Hello, world!"
        };

        var clone = field.DeepClone();

        clone.Should()
            .BeEquivalentTo(field)
            .And
            .NotBeSameAs(field);
    }

    [GenerateClone]
    private partial class FieldTests
    {
        public string MyField;
    }
}