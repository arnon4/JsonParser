namespace JsonExceptions;

public class ValueNotFoundException : Exception {
    public ValueNotFoundException(Type T, int i) : base($"A value of type {T} does not exist at index {i}") { }
    public ValueNotFoundException(Type T, string key) : base($"A value of type {T} does not exist at key {key}") { }
}