using DeepCloneGenerator;

namespace DeepCloneGenerator.DummyLib;

[GenerateClone]
public partial class DummyClass
{
    public required string Value { get; init; }
}