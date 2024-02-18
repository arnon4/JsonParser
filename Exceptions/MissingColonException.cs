namespace JsonExceptions;
public sealed class MissingColonException(int lineIndex) :
    Exception($"Missing colon at line {lineIndex}") {
}