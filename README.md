# Deep clone generator

Deep clone generator is a library that utilizes source generator to generate a `DeepClone()` method which clones classes
by its values.

## Usage

1. Install package using `dotnet add DeepCloneGenerator`
2. Add the `[GenerateDeepClone]` attribute on top of a `partial class` that you want to generate a `DeepClone()` method
   for
3. Done! You now have a `DeepClone()` method

## Supported type members

The following type members are supported:

- Fields
- required Fields
- Properties
- required Properties

## Supported types

The following types are supported for deep cloning:

- Any class annotated with `[GenerateDeepClone]`
- Any built-in value type
- Any concrete class which implements `ICollection<T>` and has a parameterless ctor (like `List<T>` and `HashSet<T>`)
- Any `IEnumerable<T>` (the clone will have a List<T> with the values of the original cloned)
- Any concrete class which implements `IDictionary<T, V>` and has a parameterless ctor (like `Dictionary<T, V>`)
- Any struct with only value types in it (because that works by default in C#)

## Known limitations

- Records are currently not supported
- Structs are currently not supported (they are cloned when part of a class, but not deeply cloned, so if your struct
  only contains value types it will work)
- When collections are made with certain characteristics, those characteristics are currently lost (e.g.: an
  EqualityComparer for a Dictionary)
- Classes with primary constructors are not yet supported

## Future plans
- Add analyzers which will inform of known limitations as warnings when DeepClone functionality is not working as intended