namespace DeepCloneGenerator.Tests;

public partial class ClassWithPropertyTests
{
    [Fact]
    public void Test()
    {
        var original = new ClassWithNonAutoProperty
        {
            Hours = 2,
            EnumValue = EnumValue.Three
        };

        var clone = original.DeepClone();

        clone.Should()
            .BeExactClone(original);
    }

    public enum EnumValue
    {
        One,
        Two,
        Three
    }

    [GenerateDeepClone]
    private partial class ClassWithNonAutoProperty
    {
        public required int Hours { get; set; }
        public required EnumValue EnumValue { get; set; }
        public int Minutes
        {
            get { return Hours * 60; }
        }

        public int Seconds
        {
            get { return Minutes * 60; }
        }
    }
}