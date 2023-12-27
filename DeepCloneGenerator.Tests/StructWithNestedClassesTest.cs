namespace DeepCloneGenerator.Tests;

public partial class StructWithNestedClassesTest
{
    [Fact]
    public void Test()
    {
        var original = new StructWithClass
        {
            Dummy = new DummyClass
            {
                Value = 1337
            }
        };

        var clone = original.DeepClone();

        clone.Dummy.Should()
            .BeExactClone(original.Dummy);
    }

    [GenerateDeepClone]
    private partial class DummyClass
    {
        public required int Value { get; init; }
    }

    [GenerateDeepClone]
    private readonly partial struct StructWithClass
    {
        public required DummyClass Dummy { get; init; }
    }
}