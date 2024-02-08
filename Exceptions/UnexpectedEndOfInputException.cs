namespace JsonExceptions;

public sealed class UnexpectedEndOfInputException : Exception {
    public UnexpectedEndOfInputException(int line) : base($"Unexpected end of input at line {line}") { }
}
