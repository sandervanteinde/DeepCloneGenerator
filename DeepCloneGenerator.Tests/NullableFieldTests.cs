namespace DeepCloneGenerator.Tests;

public partial class NullableFieldTests
{
    [Fact]
    public void TestNullableCollection()
    {
        var original = new ClassWithNullableCollection
        {
            Items = null
        };
        
        var clone = original.DeepClone();

        clone.Should()
            .BeExactClone(original);
    }

    [GenerateDeepClone]
    private partial class ClassWithNullableCollection
    {
        public List<int>? Items { get; init; }
    }
}