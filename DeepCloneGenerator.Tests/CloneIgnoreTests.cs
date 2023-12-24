using DeepCloneGenerator;

namespace DeepCloneGenerator.Tests;

public partial class CloneIgnoreTests
{
    [Fact]
    public void Test()
    {
        var original = new ClassWithIgnore
        {
            PropertyToIgnore = "Ignore me",
            PropertyToClone = "Do not skip me"
        };

        var clone = original.DeepClone();

        clone.PropertyToIgnore.Should()
            .BeNull();
        clone.PropertyToClone.Should()
            .Be(original.PropertyToClone);
    }

    [GenerateClone]
    public partial class ClassWithIgnore
    {
        [CloneIgnore] public required string PropertyToIgnore { get; init; }

        public required string PropertyToClone { get; init; }
    }
}