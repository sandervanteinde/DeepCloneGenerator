namespace DeepCloneGenerator.Tests;

public partial class ClassWithPropertyTests
{
    [Fact]
    public void Test()
    {
        var original = new ClassWithNonAutoProperty
        {
            Hours = 2
        };

        var clone = original.DeepClone();

        clone.Should()
            .BeExactClone(original);
    }

    [GenerateDeepClone]
    private partial class ClassWithNonAutoProperty
    {
        public required int Hours { get; set; }
        public int Minutes
        {
            get { return Hours * 60; }
            set{}
        }

        public int Seconds
        {
            get { return Minutes * 60; }
            set{}
        }
    }
}