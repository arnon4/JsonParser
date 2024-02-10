namespace JsonExceptions;

public sealed class DuplicateKeyException(string key, int lineIndex, int charIndex) :
    Exception($"Duplicate key {key} found at {lineIndex}:{charIndex}") {
}