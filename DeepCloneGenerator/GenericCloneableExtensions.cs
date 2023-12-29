namespace DeepCloneGenerator;

public static class GenericCloneableExtensions
{
    public static TSelf DeepClone<TSelf, TOne>(this ISourceGeneratedCloneableWithGenerics<TSelf, TOne> self)
        where TSelf : ISourceGeneratedCloneableWithGenerics<TSelf, TOne>
        where TOne : ISourceGeneratedCloneable<TOne>
    {
        return self.DeepClone(static one => one.DeepClone());
    }

    public static TSelf DeepClone<TSelf, TOne, TTwo>(this ISourceGeneratedCloneableWithGenerics<TSelf, TOne, TTwo> self)
        where TSelf : ISourceGeneratedCloneableWithGenerics<TSelf, TOne, TTwo>
        where TOne : ISourceGeneratedCloneable<TOne>
        where TTwo : ISourceGeneratedCloneable<TTwo>
    {
        return self.DeepClone(static one => one.DeepClone(), static two => two.DeepClone());
    }

    public static TSelf DeepClone<TSelf, TOne, TTwo>(this ISourceGeneratedCloneableWithGenerics<TSelf, TOne, TTwo> self, Func<TOne, TOne> oneMapper)
        where TSelf : ISourceGeneratedCloneableWithGenerics<TSelf, TOne, TTwo>
        where TTwo : ISourceGeneratedCloneable<TTwo>
    {
        return self.DeepClone(oneMapper, two => two.DeepClone());
    }

    public static TSelf DeepClone<TSelf, TOne, TTwo>(this ISourceGeneratedCloneableWithGenerics<TSelf, TOne, TTwo> self, Func<TTwo, TTwo> twoMapper)
        where TSelf : ISourceGeneratedCloneableWithGenerics<TSelf, TOne, TTwo>
        where TOne : ISourceGeneratedCloneable<TOne>
    {
        return self.DeepClone(one => one.DeepClone(), twoMapper);
    }
}