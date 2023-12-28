namespace DeepCloneGenerator.Tests;

public partial class DimensionalArraysTests
{
    [Fact]
    public void Test()
    {
        var original = new ClassWithDimensionalArrays
        {
            TwoDimensionalArray = new[,]
            {
                { 1, 2, 3 },
                { 4, 5, 6 }
            },
            ThreeDimensionalArray = new[,,]
            {
                {
                    { 7, 8, 9 },
                    { 10, 11, 12 },
                    { 13, 14, 15 },
                    { 16, 17, 18 }
                }
            }
        };

        var clone = original.DeepClone();

        clone.Should()
            .BeExactClone(original);
    }

    [GenerateDeepClone]
    private partial class ClassWithDimensionalArrays
    {
        public required int[,] TwoDimensionalArray { get; init; }
        public required int[,,] ThreeDimensionalArray { get; init; }
    }
}