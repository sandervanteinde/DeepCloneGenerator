using DeepCloneGenerator;

namespace DeepCloneGenerator.Tests;

public partial class EnumerableCloningTest
{
    [Fact]
    public void TestSingle()
    {
        var original = new ClassWithEnumerable
        {
            Strings = new[]
            {
                "Hello",
                "World"
            }
        };

        var clone = original.DeepClone();

        clone.Should()
            .BeExactClone(original);
    }

    [Fact]
    public void TestNested()
    {
        var original = new ClassWithEnumerableInEnumerable
        {
            Strings = new[]
            {
                new[]
                {
                    "Hello"
                },
                new[]
                {
                    "World"
                }
            }
        };

        var clone = original.DeepClone();

        clone.Should()
            .BeExactClone(original);
    }

    [GenerateClone]
    private partial class ClassWithEnumerable
    {
        public required IEnumerable<string> Strings { get; init; }
    }

    [GenerateClone]
    private partial class ClassWithEnumerableInEnumerable
    {
        public required IEnumerable<IEnumerable<string>> Strings { get; init; }
    }
}