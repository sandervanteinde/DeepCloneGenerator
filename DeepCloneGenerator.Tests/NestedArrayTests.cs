namespace DeepCloneGenerator.Tests;

public partial class NestedArrayTests
{
    [Fact]
    public void Test()
    {
        var rand = Random.Shared;
        var original = new NestedArrayClass
        {
            NestedArray = Enumerable.Range(start: 0, rand.Next(maxValue: 3) + 1)
                .Select(
                    _ => Enumerable.Range(start: 0, rand.Next(maxValue: 3) + 1)
                        .Select(_ => rand.Next())
                        .ToArray()
                )
                .ToArray(),
            DeeplyNestedArray = Enumerable.Range(start: 0, rand.Next(maxValue: 3) + 1)
                .Select(
                    _ => Enumerable.Range(start: 0, rand.Next(maxValue: 3) + 1)
                        .Select(
                            _ => Enumerable.Range(start: 0, rand.Next(maxValue: 3) + 1)
                                .Select(
                                    _ => Enumerable.Range(start: 0, rand.Next(maxValue: 3) + 1)
                                        .Select(_ => rand.Next())
                                        .ToArray()
                                )
                                .ToArray()
                        )
                        .ToArray()
                )
                .ToArray()
        };

        var clone = original.DeepClone();

        clone.Should()
            .BeExactClone(original);
    }

    [GenerateDeepClone]
    private partial class NestedArrayClass
    {
        public required int[][] NestedArray { get; init; }
        public required int[][][][] DeeplyNestedArray { get; init; }
    }
}