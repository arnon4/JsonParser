namespace JsonExceptions;

public sealed class UnexpectedEndOfInputException(int line) :
    Exception($"Unexpected end of input at line {line}") {
}
