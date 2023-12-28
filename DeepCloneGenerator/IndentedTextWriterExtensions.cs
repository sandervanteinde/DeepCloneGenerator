using System.CodeDom.Compiler;

namespace DeepCloneGenerator;

internal static class IndentedTextWriterExtensions
{
    public static void WriteDebugComment(this IndentedTextWriter writer, string comment)
    {
#if DEBUG
        writer.Write("/*");
        writer.Write(comment);
        writer.Write("*/");
#endif
    }

    public static void WriteDebugCommentLine(this IndentedTextWriter writer, string comment)
    {
#if DEBUG
        writer.WriteLine($"// {comment}");
#endif
    }

    public static void WriteParameterValue<T>(this IndentedTextWriter writer, T paramValue, string argumentName)
    {
#if DEBUG
        writer.WriteDebugCommentLine($"{argumentName}: {paramValue}");
#endif
    }

    public static void WriteInLineParameterValue<T>(this IndentedTextWriter writer, T paramValue, string argumentName)
    {
#if DEBUG
        writer.WriteDebugComment($"{argumentName}: {paramValue}");
#endif
    }
}