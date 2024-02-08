namespace JsonExceptions;
public sealed class MissingColonException : Exception {
    public MissingColonException(int lineIndex) : base($"Missing colon at line {lineIndex}") { }
}