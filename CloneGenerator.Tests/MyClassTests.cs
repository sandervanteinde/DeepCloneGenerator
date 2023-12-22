using CloneGenerator.Sample;
using FluentAssertions;

namespace CloneGenerator.Tests;

public class MyClassTests
{
    [Fact]
    public void Clone()
    {
        var sut = new MyClass();

        var clone = sut.Clone();

        clone.Should()
            .NotBeSameAs(sut);
        clone.Inner.Should()
            .NotBeSameAs(sut.Inner, "because we want to deep clone everything");
        clone.IntegerArray.Should()
            .NotBeSameAs(sut.IntegerArray);
        clone.Should()
            .BeEquivalentTo(sut);
    }
}