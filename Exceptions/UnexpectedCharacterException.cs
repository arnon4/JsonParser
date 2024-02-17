namespace JsonExceptions;

public sealed class UnexpectedCharacterException(string line, int lineIndex, int columnIndex) :
    Exception($"Unexpected character '{line[columnIndex]}' at {lineIndex + 1}:{columnIndex + 1}") {
}