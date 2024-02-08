namespace JsonExceptions;

public class DuplicateKeyException(string key, int lineIndex, int charIndex) :
    Exception($"Duplicate key {key} found at {lineIndex}:{charIndex}") {
}