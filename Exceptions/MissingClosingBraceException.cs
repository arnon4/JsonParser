namespace JsonExceptions;
public sealed class MissingClosingBraceException(int lineIndex) :
    Exception($"Missing closing brace at line {lineIndex}") {
}