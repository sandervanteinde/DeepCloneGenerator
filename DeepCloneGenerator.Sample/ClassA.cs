using System.Collections.Generic;

namespace DeepCloneGenerator.Analyzers.Sample;

[GenerateDeepClone]
public partial class ClassA
{
    public B B { get; set; }
    public IEnumerable<int> Values { get; set; }
    
    [CloneIgnore]
    private B _ignoreThis;
}