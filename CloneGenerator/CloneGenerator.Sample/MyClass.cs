namespace CloneGenerator.Sample;

[GenerateClone]
public partial class MyClass
{
    public string Test { get; }
    private readonly string _nonAutoProperty;
    public string AutoPropertyAccessor => _nonAutoProperty;
    public MyInnerClass Inner { get; }
    public int[] IntegerArray { get; }
    public MyInnerClass[] InnerArrayOfInnerClasses { get; }

    public MyClass()
    {
        Test = "Hello, world!";
        _nonAutoProperty = "Non auto property";
        Inner = new();
        IntegerArray = new[] { 1, 2, 3, 4 };
        InnerArrayOfInnerClasses = new[]
        {
            new MyInnerClass("Sander"),
            new MyInnerClass("van 't Einde")
        };
    }
}

[GenerateClone]
public partial class MyInnerClass
{
    public string Value { get; }

    public MyInnerClass(string text = "Hello, world!")
    {
        Value = "Hello, world!";
    }
}