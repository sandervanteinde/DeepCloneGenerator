namespace DeepCloneGenerator.Tests;

public partial class ClassInheritanceTests
{
    [Fact]
    public void TestWithParentHavingAttribute()
    {
        var original = new ParentClass("abstract property value")
        {
            BaseProperty = "Hello",
            ParentProperty = "World"
        };

        var clone = original.DeepClone();

        clone.Should()
            .BeExactClone(original);
    }

    [Fact]
    public void TestWithParentOfParentHavingAttribute()
    {
        var original = new ParentOfParentClass { BaseProperty = "Hello", ParentProperty = "World", ParentOfParentProperty = "!" };
        var clone = original.DeepClone();
        clone.Should()
            .BeExactClone(original);
    }

    [Fact]
    public void TestWithParentWithBaseNotHavingAttribute()
    {
        var original = new ParentWithBaseWithoutAttribute
        {
            BaseProperty = "Hello",
            ParentProperty = "World!"
        };

        var clone = original.DeepClone();

        clone.Should()
            .BeExactClone(original);
    }

    [Fact]
    public void TestWithParentOfParentBeingAssignedToParent()
    {
        ParentClass original = new ParentOfParentClass { BaseProperty = "Hello", ParentProperty = "World", ParentOfParentProperty = "!" };

        var clone = original.DeepClone();
        clone.Should()
            .BeExactClone(original);
    }

    [GenerateDeepClone]
    private abstract partial class BaseClass
    {
        public required string BaseProperty { get; init; }
        public abstract string AbstractProperty { get; }
    }

    private abstract class BaseWithoutAttribute
    {
        public required string BaseProperty { get; init; }
        public abstract string AbstractProperty { get; }
    }

    [GenerateDeepClone]
    private partial class ParentWithBaseWithoutAttribute : BaseWithoutAttribute
    {
        public required string ParentProperty { get; init; }
        public override string AbstractProperty => "Auto property";
    }

    [GenerateDeepClone]
    private partial class ParentClass : BaseClass
    {
        public ParentClass(string abstractPropertyValue)
        {
            AbstractProperty = abstractPropertyValue;
        }

        public required string ParentProperty { get; init; }
        public override string AbstractProperty { get; }
    }

    [GenerateDeepClone]
    private partial class ParentOfParentClass : ParentClass
    {
        public ParentOfParentClass()
            : base("Parent of parent")
        {
        }

        public required string ParentOfParentProperty { get; init; }
    }
}