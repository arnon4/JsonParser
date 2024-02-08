namespace JsonExceptions;

public sealed class UnexpectedCharacterException : Exception {
    public UnexpectedCharacterException(string line, int lineIndex, int columnIndex) :
        base($"Unexpected character '{line[lineIndex]}' at {lineIndex + 1}:{columnIndex + 1}") { }
}