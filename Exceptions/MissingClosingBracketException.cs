namespace DynamicJsonParser;
public sealed class MissingClosingBracketException(int lineIndex) :
    Exception($"Missing closing bracket at line {lineIndex}") {
}