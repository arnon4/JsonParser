namespace JsonExceptions;
public sealed class MissingClosingQuoteException(int lineIndex) :
    Exception($"Missing closing quote at line {lineIndex}") {
}