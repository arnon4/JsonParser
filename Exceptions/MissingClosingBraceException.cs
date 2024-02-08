namespace JsonExceptions;
public sealed class MissingClosingBraceException : Exception {
    public MissingClosingBraceException(int lineIndex) : base($"Missing closing brace at line {lineIndex}") { }
}