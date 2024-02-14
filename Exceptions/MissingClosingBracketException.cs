namespace DynamicJsonParser;
public sealed class MissingClosingBracketException : Exception {
    public MissingClosingBracketException(int lineIndex) : base($"Missing closing bracket at line {lineIndex}") { }
}