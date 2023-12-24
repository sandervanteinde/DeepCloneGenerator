namespace DeepCloneGenerator.Tests;

public partial class DictionaryCloningTests
{
    [Fact]
    public void Test()
    {
        var original = new ClassWithDictionary
        {
            Values = new Dictionary<string, string>
            {
                ["Hello"] = "World",
                ["Goodbye"] = "Mars"
            }
        };

        var clone = original.DeepClone();

        clone.Should()
            .BeExactClone(original);
    }

    [GenerateDeepClone]
    private partial class ClassWithDictionary
    {
        public required Dictionary<string, string> Values { get; init; }
    }
}