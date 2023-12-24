namespace DeepCloneGenerator.Tests;

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

    [GenerateDeepClone]
    public partial class ClassWithRequiredField
    {
        public required string MyRequiredField;
    }
}