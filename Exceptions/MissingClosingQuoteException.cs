namespace JsonExceptions;
public sealed class MissingClosingQuoteException : Exception {
    public MissingClosingQuoteException(int lineIndex) : base($"Missing closing quote at line {lineIndex}") { }
}