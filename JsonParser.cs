using JsonExceptions;

namespace Parser;
public sealed class JsonParser(IEnumerable<string> lines) {
    private readonly List<string> _lines = lines.ToList();
    private int _lineIndex = 0;
    private int _charIndex = 0;

    // Either _object or _array will be non-null, but not both
    private JsonObject? _object;
    private JsonArray? _array;
    public void Parse() {
        SkipWhitespace(TokenType.OpeningBrace);
        if (IsOpeningBrace()) {
            _object = ParseObject();
        } else if (IsOpeningBracket()) {
            _array = ParseArray();
        } else {
            throw new UnexpectedCharacterException(_lines[_lineIndex], _lineIndex, _charIndex);
        }
    }
    public T GetParsed<T>() {
        if (_object is not null) {
            return (T)(object)_object;
        }

        if (_array is not null) {
            return (T)(object)_array;
        }

        throw new InvalidOperationException("Invalid JSON input");
    }
    public T? Get<T>(string key) {
        if (_object is null) {
            throw new InvalidOperationException("The JSON input is an array, not an object");
        }

        return _object.Get<T>(key);
    }
    public T? Get<T>(int index) {
        if (_array is null) {
            throw new InvalidOperationException("The JSON input is an object, not an array");
        }

        return _array.Get<T>(index);
    }
    public override string ToString() {
        return _object is not null ? _object.ToString() : _array?.ToString() ?? "";
    }
    private bool IsOpeningBracket() {
        return _lines[_lineIndex][_charIndex] == '[';
    }
    private bool IsOpeningBrace() {
        return _lines[_lineIndex][_charIndex] == '{';
    }

    private JsonObject ParseObject() {
        if (_lines[_lineIndex].Length > 1) {
            SkipToNextChar();
        } else {
            _lineIndex++;
            _charIndex = 0;
        }

        JsonObject obj = new();
        // Opening brace must be followed by whitespace
        if (!IsWhiteSpace()) {
            throw new UnexpectedCharacterException(_lines[_lineIndex], _lineIndex, _charIndex);
        }

        SkipWhitespace(TokenType.Any);

        if (IsClosingBrace()) {
            return obj;
        }

        if (!IsQuote()) {
            throw new UnexpectedCharacterException(_lines[_lineIndex], _lineIndex, _charIndex);
        }

        while (true) {
            string key = ParseString();
            SkipToNextChar();

            SkipWhitespace(TokenType.Colon);
            SkipToNextChar();
            SkipWhitespace(TokenType.Any);
            AddValue(obj, key);

            SkipToNextChar();
            while (_lineIndex < _lines.Count && IsWhiteSpace()) {
                SkipToNextChar();
                if (_charIndex == _lines[_lineIndex].Length) {
                    _lineIndex++;
                    _charIndex = 0;
                }
            }

            if (_lineIndex == _lines.Count) {
                throw new UnexpectedEndOfInputException(_lineIndex);
            }

            if (!IsComma()) {
                break;
            }

            SkipToNextChar();
            SkipWhitespace(TokenType.Quote);
        }

        if (!IsClosingBrace()) {
            throw new MissingClosingBraceException(_lineIndex);
        }

        return obj;
    }

    private void AddValue(JsonObject obj, string key) {
        if (obj.ContainsKey(key)) {
            throw new DuplicateKeyException(key, _lineIndex, _charIndex);
        }

        switch (_lines[_lineIndex][_charIndex]) {
            case '{':
                JsonObject nestedObj = ParseObject();
                obj.Add(key, nestedObj);
                break;
            case '[':
                JsonArray nestedArray = ParseArray();
                obj.Add(key, nestedArray);
                break;
            case '"':
                obj.Add(key, ParseString());
                break;
            case 't':
            case 'f':
                obj.Add(key, ParseBoolean());
                break;
            case 'n':
                obj.Add(key);
                break;
            case '-':
            default:
                ParseNumber(obj, key);
                break;
        }
    }
    private JsonArray ParseArray() {
        if (_lines[_lineIndex].Length > 1) {
            SkipToNextChar();
        } else {
            _lineIndex++;
            _charIndex = 0;
        }

        JsonArray array = new();

        SkipWhitespace(TokenType.Any);

        if (IsClosingBracket()) {
            return array;
        }

        int key = 0;
        while (true) {
            SkipWhitespace(TokenType.Any);

            AddValue(key, array);

            SkipToNextChar();
            while (_lineIndex < _lines.Count && IsWhiteSpace()) {
                SkipToNextChar();
                if (_charIndex == _lines[_lineIndex].Length) {
                    _lineIndex++;
                    _charIndex = 0;
                }
            }

            if (_lineIndex == _lines.Count) {
                throw new UnexpectedEndOfInputException(_lineIndex);
            }

            if (!IsComma()) {
                break;
            }

            SkipToNextChar();
            SkipWhitespace(TokenType.Any);

            key++;
        }

        if (!IsClosingBracket()) {
            throw new MissingClosingBracketException(_lineIndex);
        }

        return array;
    }
    private void AddValue(int index, JsonArray arr) {
        switch (_lines[_lineIndex][_charIndex]) {
            case '{':
                JsonObject nestedObj = ParseObject();
                arr.Add(index, nestedObj);
                break;
            case '[':
                JsonArray nestedArray = ParseArray();
                arr.Add(index, nestedArray);
                break;
            case '"':
                arr.Add(index, ParseString());
                break;
            case 't':
            case 'f':
                arr.Add(index, ParseBoolean());
                break;
            case 'n':
                arr.Add(index);
                break;
            case '-':
            default:
                ParseNumber(index, arr);
                break;
        }
    }
    private bool IsClosingBracket() {
        return _lines[_lineIndex][_charIndex] == ']';
    }
    private void ParseNumber(int index, JsonArray array) {
        int start = _charIndex;
        while (true) {
            if (IsWhiteSpace() || IsComma() || IsClosingBracket()) {
                string number = _lines[_lineIndex][start.._charIndex];
                if (long.TryParse(number, out long longValue)) {
                    array.Add(index, longValue);
                    break;
                }

                if (decimal.TryParse(number, out decimal decimalValue)) {
                    array.Add(index, decimalValue);
                    break;
                }

                throw new UnexpectedCharacterException(_lines[_lineIndex], _lineIndex, _charIndex);
            }

            SkipToNextChar();
        }
    }

    /// <summary>
    /// Parses a number and adds it to the <see cref="JsonObject"/> with the given key.
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="key"></param>
    /// <exception cref="UnexpectedCharacterException"></exception>
    private void ParseNumber(JsonObject obj, string key) {
        int start = _charIndex;
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

                throw new UnexpectedCharacterException(_lines[_lineIndex], _lineIndex, _charIndex);
            }

            if (IsWhiteSpace() || IsComma() || IsClosingBrace() || IsClosingBracket()) {
                string number = _lines[_lineIndex][start.._charIndex];
                if (long.TryParse(number, out long longValue)) {
                    obj.Add(key, longValue);
                    break;
                }

                if (decimal.TryParse(number, out decimal decimalValue)) {
                    obj.Add(key, decimalValue);
                    break;
                }

                throw new UnexpectedCharacterException(_lines[_lineIndex], _lineIndex, _charIndex);
            }

            SkipToNextChar();
        }

        // bring _charIndex back to the last character of the number
        _charIndex--;
    }
    private bool IsComma() {
        return _lines[_lineIndex][_charIndex] == ',';
    }
    private bool IsQuote() {
        return _lines[_lineIndex][_charIndex] == '"';
    }
    private bool IsClosingBrace() {
        return _lines[_lineIndex][_charIndex] == '}';
    }
    private object? ParseNull() {
        if (_lines[_lineIndex].Substring(_charIndex, 4) == "null") {
            _charIndex += 3;
            return null;
        }

        throw new UnexpectedCharacterException(_lines[_lineIndex], _lineIndex, _charIndex);
    }

    /// <summary>
    /// Parses a string between two unescaped quotes. Sets <see cref="_charIndex"/> to the index of the closing quote.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="MissingClosingQuoteException"></exception>
    /// <exception cref="UnexpectedEndOfInputException"></exception>
    /// <exception cref="UnexpectedCharacterException"></exception>
    /// <returns>the parsed string</returns>
    private string ParseString() {
        SkipToNextChar();
        int start = _charIndex;
        while (true) {
            if (_lines[_lineIndex][_charIndex] == '"') {
                if (_lines[_lineIndex][_charIndex - 1] != '\\') {
                    break;
                }

                int backslashes = 0;
                for (int i = _charIndex - 1; i > start; i--) {
                    if (_lines[_lineIndex][i] == '\\') {
                        backslashes++;
                    } else {
                        break;
                    }
                }

                if (backslashes % 2 == 0) {
                    break;
                }

                SkipToNextChar();
                if (_charIndex == _lines[_lineIndex].Length) {
                    throw new MissingClosingQuoteException(_lineIndex);
                }
            }

            SkipToNextChar();
        }

        return _lines[_lineIndex][start.._charIndex];
    }

    /// <summary>
    /// Parses a boolean value. Sets <see cref="_charIndex"/> to the index of the last character of the boolean value.
    /// </summary>
    /// <exception cref="UnexpectedCharacterException"></exception>
    /// <returns><see cref="true"/> or <see cref="false"/></returns>
    private bool ParseBoolean() {
        if (_lines[_lineIndex][_charIndex] == 't') {
            if (_lines[_lineIndex].Substring(_charIndex, 4) == "true") {
                _charIndex += 3;
                return true;
            }
        } else {
            if (_lines[_lineIndex].Substring(_charIndex, 5) == "false") {
                _charIndex += 4;
                return false;
            }
        }

        throw new UnexpectedCharacterException(_lines[_lineIndex], _lineIndex, _charIndex);
    }

    /// <summary>
    /// Skips whitespace until the next non-whitespace character is found.
    /// If the end of the input is reached, an exception is thrown based on the token type. 
    /// </summary>
    /// <param name="token">determines the error to be thrown if we don't find a character before the end of input.</param>
    private void SkipWhitespace(TokenType token) {
        while (IsWhiteSpace()) {
            SkipToNextChar();
            if (_charIndex == _lines[_lineIndex].Length) {
                _lineIndex++;
                _charIndex = 0;

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
            char c = _lines[_lineIndex][_charIndex];
            switch (token) {
                case TokenType.OpeningBracket when c != '[':
                    throw new UnexpectedCharacterException(_lines[_lineIndex], _lineIndex, _charIndex);
                case TokenType.ClosingBracket when c != ']':
                    throw new UnexpectedCharacterException(_lines[_lineIndex], _lineIndex, _charIndex);
                case TokenType.Quote when c != '"':
                    throw new UnexpectedCharacterException(_lines[_lineIndex], _lineIndex, _charIndex);
                case TokenType.Colon when c != ':':
                    throw new UnexpectedCharacterException(_lines[_lineIndex], _lineIndex, _charIndex);
                case TokenType.OpeningBrace when c != '{':
                    throw new UnexpectedCharacterException(_lines[_lineIndex], _lineIndex, _charIndex);
                case TokenType.ClosingBrace when c != '}':
                    throw new UnexpectedCharacterException(_lines[_lineIndex], _lineIndex, _charIndex);
                case TokenType.Comma when c != ',':
                    throw new UnexpectedCharacterException(_lines[_lineIndex], _lineIndex, _charIndex);
            }
        }
    }
    private bool IsWhiteSpace() {
        char c = _lines[_lineIndex][_charIndex];
        return char.IsWhiteSpace(c) || c == '\n' || c == '\r' || c == '\t';
    }
    private void SkipToNextChar() {
        _charIndex++;
        if (_charIndex == _lines[_lineIndex].Length) {
            _lineIndex++;
            _charIndex = 0;
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