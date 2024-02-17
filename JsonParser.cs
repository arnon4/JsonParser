using JsonExceptions;

namespace DynamicJsonParser;
public sealed class JsonParser(IEnumerable<string> lines) {
    private readonly List<string> _lines = lines.ToList();
    private int _lineIndex = 0;
    private int _columnIndex = 0;

    // Either _object or _array will be non-null, but not both
    private JsonObject? _object;
    private JsonArray? _array;
    public void Parse() {
        SkipWhitespace(TokenType.Any);
        Stack<JsonEntity> stack = new();
        Stack<string> keys = new();
        int arrayIndex = 0;
        bool hasDanglingComma = false;

        while (_lineIndex < _lines.Count) {
            if (IsOpeningBrace()) {
                stack.Push(new JsonObject());
                AdvanceCharIndex();
            } else if (IsOpeningBracket()) {
                stack.Push(new JsonArray());
                AdvanceCharIndex();
            } else {
                if (stack.Count == 0) {
                    throw new UnexpectedCharacterException(_lines[_lineIndex], _lineIndex, _columnIndex);
                }

                if (stack.Peek() is JsonObject j) {
                    if (!IsQuote() && !IsClosingBrace()) {
                        throw new UnexpectedCharacterException(_lines[_lineIndex], _lineIndex, _columnIndex);
                    }

                    if (IsClosingBrace()) {
                        if (hasDanglingComma) {
                            throw new UnexpectedCharacterException(_lines[_lineIndex], _lineIndex, _columnIndex);
                        }

                        stack.Pop();
                        if (stack.Count == 0) {
                            _object = j;
                            return;
                        }

                        if (stack.Peek() is JsonObject parentObj) {
                            if (keys.Count == 0) {
                                throw new UnexpectedCharacterException(_lines[_lineIndex], _lineIndex, _columnIndex);
                            }
                            parentObj.Add(keys.Pop(), j);
                        } else {
                            JsonArray parentArr = (JsonArray)stack.Peek();
                            parentArr.Add(arrayIndex, j);
                            arrayIndex++;
                        }
                    }

                    if (keys.Count == 0) {
                        if (!IsQuote()) {
                            throw new UnexpectedCharacterException(_lines[_lineIndex], _lineIndex, _columnIndex);
                        }
                        keys.Push(ParseString());
                        AdvanceCharIndex();
                        SkipWhitespace(TokenType.Colon);
                    } else {
                       AddValue(j, keys.Pop());
                       AdvanceCharIndex();
                       if (IsComma()) {
                           AdvanceCharIndex();
                           hasDanglingComma = true;
                       } else {
                           hasDanglingComma = false;
                       } 
                    }
                }

                if (stack.Peek() is JsonArray a) {
                    if (!IsQuote() && !IsClosingBracket()) {
                        throw new UnexpectedCharacterException(_lines[_lineIndex], _lineIndex, _columnIndex);
                    }

                    if (IsClosingBracket()) {
                        stack.Pop();
                        arrayIndex = 0;
                        if (stack.Count == 0) {
                            _array = a;
                            return;
                        }

                        if (stack.Peek() is JsonObject parentObj) {
                            if (keys.Count == 0) {
                                throw new UnexpectedCharacterException(_lines[_lineIndex], _lineIndex, _columnIndex);
                            }
                            parentObj.Add(keys.Pop(), a);
                        } else {
                            JsonArray parentArr = (JsonArray)stack.Peek();
                            parentArr.Add(arrayIndex, a);
                            arrayIndex++;
                        }
                    }

                    AddValue(arrayIndex, a);
                    AdvanceCharIndex();
                    if (IsComma()) {
                        AdvanceCharIndex();
                        hasDanglingComma = true;
                    } else {
                        hasDanglingComma = false;
                    }
                }
            }
        }
    }
    public T? Get<T>(string key) {
        if (_object is null) {
            return default;
        }

        return _object.Get<T>(key);
    }
    public T? Get<T>(int index) {
        if (_array is null) {
            return default;
        }

        return _array.Get<T>(index);
    }
    private bool IsOpeningBracket() {
        return _lines[_lineIndex][_columnIndex] == '[';
    }
    private bool IsOpeningBrace() {
        return _lines[_lineIndex][_columnIndex] == '{';
    }
    private void AddValue(JsonObject obj, string key) {
        if (obj.ContainsKey(key)) {
            throw new DuplicateKeyException(key, _lineIndex, _columnIndex);
        }

        switch (_lines[_lineIndex][_columnIndex]) {
            case '{':
            case '[':
            break;
            case '"':
            obj.Add(key, ParseString());
            break;
            case 't':
            case 'f':
            obj.Add(key, ParseBoolean());
            break;
            case 'n':
            ParseNull();
            obj.Add(key);
            break;
            case '-':
            default:
            ParseNumber(obj, key);
            break;
        }
    }
    private void AddValue(int index, JsonArray arr) {
        switch (_lines[_lineIndex][_columnIndex]) {
            case '{':
            case '[':
            break;
            case '"':
            arr.Add(index, ParseString());
            break;
            case 't':
            case 'f':
            arr.Add(index, ParseBoolean());
            break;
            case 'n':
            ParseNull();
            arr.Add(index);
            break;
            case '-':
            default:
            ParseNumber(index, arr);
            break;
        }
    }
    private bool IsClosingBracket() {
        return _lines[_lineIndex][_columnIndex] == ']';
    }
    private void ParseNumber(int index, JsonArray array) {
        int start = _columnIndex;
        int startLine = _lineIndex;
        while (true) {
            if (_lineIndex > startLine) {
                string number = _lines[startLine][start..];
                if (long.TryParse(number, out long longValue)) {
                    array.Add(index, longValue);
                    break;
                }

                if (decimal.TryParse(number, out decimal decimalValue)) {
                    array.Add(index, decimalValue);
                    break;
                }

                throw new UnexpectedCharacterException(_lines[_lineIndex], _lineIndex, _columnIndex);
            }

            if (IsWhiteSpace() || IsComma() || IsClosingBracket()) {
                string number = _lines[_lineIndex][start.._columnIndex];
                if (long.TryParse(number, out long longValue)) {
                    array.Add(index, longValue);
                    break;
                }

                if (decimal.TryParse(number, out decimal decimalValue)) {
                    array.Add(index, decimalValue);
                    break;
                }

                throw new UnexpectedCharacterException(_lines[_lineIndex], _lineIndex, _columnIndex);
            }

            AdvanceCharIndex();
        }

        _columnIndex--;
    }

    /// <summary>
    /// Parses a number and adds it to the <see cref="JsonObject"/> with the given key.
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="key"></param>
    /// <exception cref="UnexpectedCharacterException"></exception>
    private void ParseNumber(JsonObject obj, string key) {
        int start = _columnIndex;
        int startLine = _lineIndex;
        while (true) {
            if (_lineIndex > startLine) {
                string number = _lines[startLine][start..];
                if (long.TryParse(number, out long longValue)) {
                    obj.Add(key, longValue);
                    break;
                }

                if (decimal.TryParse(number, out decimal decimalValue)) {
                    obj.Add(key, decimalValue);
                    break;
                }

                throw new UnexpectedCharacterException(_lines[_lineIndex], _lineIndex, _columnIndex);
            }

            if (IsWhiteSpace() || IsComma() || IsClosingBrace() || IsClosingBracket()) {
                string number = _lines[_lineIndex][start.._columnIndex];
                if (long.TryParse(number, out long longValue)) {
                    obj.Add(key, longValue);
                    break;
                }

                if (decimal.TryParse(number, out decimal decimalValue)) {
                    obj.Add(key, decimalValue);
                    break;
                }

                throw new UnexpectedCharacterException(_lines[_lineIndex], _lineIndex, _columnIndex);
            }

            AdvanceCharIndex();
        }

        // bring _charIndex back to the last character of the number
        _columnIndex--;
    }
    private bool IsComma() {
        return _lines[_lineIndex][_columnIndex] == ',';
    }
    private bool IsQuote() {
        return _lines[_lineIndex][_columnIndex] == '"';
    }
    private bool IsClosingBrace() {
        return _lines[_lineIndex][_columnIndex] == '}';
    }
    private void ParseNull() {
        if (_lines[_lineIndex].Substring(_columnIndex, 4) == "null") {
            _columnIndex += 3;
            return;
        }

        throw new UnexpectedCharacterException(_lines[_lineIndex], _lineIndex, _columnIndex);
    }

    /// <summary>
    /// Parses a string between two unescaped quotes. Sets <see cref="_columnIndex"/> to the index of the closing quote.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="MissingClosingQuoteException"></exception>
    /// <exception cref="UnexpectedEndOfInputException"></exception>
    /// <exception cref="UnexpectedCharacterException"></exception>
    /// <returns>the parsed string</returns>
    private string ParseString() {
        AdvanceCharIndex();
        int start = _columnIndex;
        while (true) {
            if (_lines[_lineIndex][_columnIndex] == '"') {
                if (_lines[_lineIndex][_columnIndex - 1] != '\\') {
                    break;
                }

                int backslashes = 0;
                for (int i = _columnIndex - 1; i > start; i--) {
                    if (_lines[_lineIndex][i] == '\\') {
                        backslashes++;
                    } else {
                        break;
                    }
                }

                if (backslashes % 2 == 0) {
                    break;
                }

                AdvanceCharIndex();
                if (_columnIndex == _lines[_lineIndex].Length) {
                    throw new MissingClosingQuoteException(_lineIndex);
                }
            }

            AdvanceCharIndex();
        }

        return _lines[_lineIndex][start.._columnIndex];
    }

    /// <summary>
    /// Parses a boolean value. Sets <see cref="_columnIndex"/> to the index of the last character of the boolean value.
    /// </summary>
    /// <exception cref="UnexpectedCharacterException"></exception>
    /// <returns><see cref="true"/> or <see cref="false"/></returns>
    private bool ParseBoolean() {
        if (_lines[_lineIndex][_columnIndex] == 't') {
            if (_lines[_lineIndex].Substring(_columnIndex, 4) == "true") {
                _columnIndex += 3;
                return true;
            }
        } else {
            if (_lines[_lineIndex].Substring(_columnIndex, 5) == "false") {
                _columnIndex += 4;
                return false;
            }
        }

        throw new UnexpectedCharacterException(_lines[_lineIndex], _lineIndex, _columnIndex);
    }

    /// <summary>
    /// Skips whitespace until the next non-whitespace character is found.
    /// If the end of the input is reached, an exception is thrown based on the token type. 
    /// </summary>
    /// <param name="token">determines the error to be thrown if we don't find a character before the end of input.</param>
    private void SkipWhitespace(TokenType token) {
        while (IsWhiteSpace()) {
            AdvanceCharIndex();
            if (_columnIndex == _lines[_lineIndex].Length) {
                _lineIndex++;
                _columnIndex = 0;

                if (_lineIndex == _lines.Count) {
                    throw token switch {
                        TokenType.ClosingBracket => new MissingClosingBracketException(_lineIndex),
                        TokenType.Quote => new MissingClosingQuoteException(_lineIndex),
                        TokenType.Colon => new MissingColonException(_lineIndex),
                        TokenType.ClosingBrace => new MissingClosingBraceException(_lineIndex),
                        _ => new UnexpectedEndOfInputException(_lineIndex)
                    };
                }
            }
        }

        if (token != TokenType.Any) {
            char c = _lines[_lineIndex][_columnIndex];
            switch (token) {
                case TokenType.OpeningBracket when c != '[':
                throw new UnexpectedCharacterException(_lines[_lineIndex], _lineIndex, _columnIndex);
                case TokenType.ClosingBracket when c != ']':
                throw new UnexpectedCharacterException(_lines[_lineIndex], _lineIndex, _columnIndex);
                case TokenType.Quote when c != '"':
                throw new UnexpectedCharacterException(_lines[_lineIndex], _lineIndex, _columnIndex);
                case TokenType.Colon when c != ':':
                throw new UnexpectedCharacterException(_lines[_lineIndex], _lineIndex, _columnIndex);
                case TokenType.OpeningBrace when c != '{':
                throw new UnexpectedCharacterException(_lines[_lineIndex], _lineIndex, _columnIndex);
                case TokenType.ClosingBrace when c != '}':
                throw new UnexpectedCharacterException(_lines[_lineIndex], _lineIndex, _columnIndex);
                case TokenType.Comma when c != ',':
                throw new UnexpectedCharacterException(_lines[_lineIndex], _lineIndex, _columnIndex);
            }
        }
    }
    private bool IsWhiteSpace() {
        char c = _lines[_lineIndex][_columnIndex];
        return char.IsWhiteSpace(c) || c == '\n' || c == '\r' || c == '\t';
    }
    private void AdvanceCharIndex() {
        _columnIndex++;
        if (_columnIndex == _lines[_lineIndex].Length) {
            _lineIndex++;
            _columnIndex = 0;
        }
    }
}
public enum TokenType {
    OpeningBrace,
    ClosingBrace,
    OpeningBracket,
    ClosingBracket,
    Colon,
    Comma,
    Quote,
    Any
}