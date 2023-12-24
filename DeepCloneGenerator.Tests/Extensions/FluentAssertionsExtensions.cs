using FluentAssertions.Equivalency;
using FluentAssertions.Primitives;

namespace DeepCloneGenerator.Tests.Extensions;

public static class FluentAssertionsExtensions
{
    public static AndConstraint<ObjectAssertions> BeExactClone(this ObjectAssertions objectAssertions, object expected)
    {
        return objectAssertions
            .BeEquivalentTo(
                expected, opt => opt
                    .Using(new Test())
            )
            .And
            .NotBeSameAs(expected);
    }

    private class Test : IEquivalencyStep
    {
        public EquivalencyResult Handle(Comparands comparands, IEquivalencyValidationContext context, IEquivalencyValidator nestedValidator)
        {
            var subject = comparands.Subject;
            var expectation = comparands.Expectation;

            if (!comparands.RuntimeType.IsClass || comparands.RuntimeType == typeof(string))
            {
                return EquivalencyResult.ContinueWithNext;
            }

            subject.Should()
                .NotBeSameAs(expectation);

            return EquivalencyResult.ContinueWithNext;
        }
    }
}