namespace DeepCloneGenerator.Tests;

public partial class ClassWithConstantTests
{

    public void Test()
    {
        var original = new ClassWithConstant
        {
            TestValue = "Hello, world!"
        };
        
        var clone = original.DeepClone();

        clone.Should()
            .BeExactClone(original);
    }
    [GenerateDeepClone]
    public partial class ClassWithConstant
    {
        public const string Test = nameof(Test);
        public string TestValue { get; set; } = Test;
    }
}