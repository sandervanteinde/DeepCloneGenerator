
namespace DeepCloneGenerator.Tests
{
    using TotallyDifferentNamespace;

    public partial class ArrayCloningTests
    {
        [Fact]
        public void Test()
        {
            var original = new ClassWithArray
            {
                Integers = new[] { 1, 2, 3, 4, 5, 6 }
            };

            var clone = original.DeepClone();

            clone.Should()
                .BeExactClone(original);
        }

        [Fact]
        public void OtherNamespaceTest()
        {
            var original = new ClassWithArrayFromDifferentNamespace { OtherNamespaceProp = new[] { EnumFromOtherNamespace.A, EnumFromOtherNamespace.B } };
            var clone = original.DeepClone();
            clone.Should()
                .BeExactClone(original);
        }

        [GenerateDeepClone]
        private partial class ClassWithArray
        {
            public int[] Integers { get; init; }
        }

        [GenerateDeepClone]
        private partial class ClassWithArrayFromDifferentNamespace
        {
            public EnumFromOtherNamespace[] OtherNamespaceProp { get; init; }
        }
    }
}

namespace TotallyDifferentNamespace
{
    public enum EnumFromOtherNamespace
    {
        A,
        B
    }
}