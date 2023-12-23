using FluentAssertions;

namespace CloneGenerator.Tests;

public partial class RequiredFieldCloningTests
{
    [Fact]
    public void Test()
    {
        var original = new ClassWithRequiredField
        {
            MyRequiredField = "Hello, world!"
        };

        var clone = original.DeepClone();

        clone.Should()
            .NotBeSameAs(original)
            .And
            .BeEquivalentTo(original);
    }

    [GenerateClone]
    public partial class ClassWithRequiredField
    {
        public required string MyRequiredField;
    }
}