namespace DeepCloneGenerator;

public interface ISourceGeneratedCloneable<out TSelf>
{
    TSelf DeepClone();
}

public interface ISourceGeneratedCloneableWithGenerics<out TSelf, T1>
    where TSelf : ISourceGeneratedCloneableWithGenerics<TSelf, T1>
{
    TSelf DeepClone(Func<T1, T1> arg1);
}

public interface ISourceGeneratedCloneableWithGenerics<out TSelf, T1, T2>
    where TSelf : ISourceGeneratedCloneableWithGenerics<TSelf, T1, T2>
{
    TSelf DeepClone(Func<T1, T1> arg1, Func<T2, T2> arg2);
}