namespace DeepCloneGenerator.Tests;

public partial class RecordTests
{
    [Fact]
    public void Test()
    {
        var original = new RecordWithSimpleProperties("One", Two: 2);

        var clone = original.DeepClone();

        clone.Should()
            .BeExactClone(original);
    }

    [Fact]
    public void TestWithNestedValue()
    {
        var original = new RecordWithNestedDeepClonedValue(new SimpleValue { Test = 345 });

        var clone = original.DeepClone();

        clone.Should()
            .BeExactClone(original);
    }

    [GenerateDeepClone]
    private partial record RecordWithSimpleProperties(string One, int Two);

    [GenerateDeepClone]
    private partial class SimpleValue
    {
        public required int Test { get; init; }
    }

    [GenerateDeepClone]
    private partial record RecordWithNestedDeepClonedValue(SimpleValue Value);
}