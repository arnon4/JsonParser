using JsonExceptions;

namespace DynamicJsonParser;
public sealed class JsonParser(IEnumerable<string> lines) {
    private readonly List<string> _lines = lines.ToList();
    private int _lineIndex = 0;
    private int _columnIndex = 0;

    // Either _object or _array will be non-null, but not both
    private JsonObject? _object;
    private JsonArray? _array;
    /// <summary>
    /// Parses the JSON input and stores it internally either as a <see cref="JsonObject"/> or a <see cref="JsonArray"/>.
    /// </summary>
    /// <exception cref="UnexpectedCharacterException"></exception>
    public void Parse() {
        SkipWhitespace(TokenType.Any);
        Stack<JsonEntity> elements = [];
        Stack<string> keys = [];
        Stack<int> arrayIndicies = [];
        bool danglingComma = false;

        while (_lineIndex < _lines.Count) {
            char c = _lines[_lineIndex][_columnIndex];
            if (c == '{') {
                elements.Push(new JsonObject());
                AdvanceColumnIndex();
                SkipWhitespace(TokenType.Any);
                if (IsQuote()) {
                    keys.Push(ParseString());
                    AdvanceColumnIndex();
                    SkipWhitespace(TokenType.Colon);
                    AdvanceColumnIndex();
                    SkipWhitespace(TokenType.Any);
                }
            } else if (c == '[') {
                elements.Push(new JsonArray());
                AdvanceColumnIndex();
                SkipWhitespace(TokenType.Any);
                arrayIndicies.Push(0);
            } else if (c == '}') {
                if (elements.Count == 0) {
                    throw new UnexpectedCharacterException(_lines[_lineIndex], _lineIndex, _columnIndex);
                }

                if (danglingComma) {
                    throw new UnexpectedCharacterException(_lines[_lineIndex], _lineIndex, _columnIndex);
                }

                JsonObject objValue = (JsonObject)elements.Pop();
                if (elements.Count == 0) {
                    if (keys.Count > 0 || arrayIndicies.Count > 0) {
                        throw new UnexpectedCharacterException(_lines[_lineIndex], _lineIndex, _columnIndex);
                    }

                    if (objValue is JsonObject finalObj) {
                        _object = finalObj;
                        return;
                    }
                    throw new UnexpectedCharacterException(_lines[_lineIndex], _lineIndex, _columnIndex);
                }

                if (elements.Peek() is JsonObject obj) {
                    if (keys.Count == 0) {
                        throw new UnexpectedCharacterException(_lines[_lineIndex], _lineIndex, _columnIndex);
                    }

                    obj.Add(keys.Pop(), objValue);
                } else {
                    var arrayIndex = arrayIndicies.Pop();
                    ((JsonArray)elements.Peek()).Add(arrayIndex, objValue);
                    arrayIndicies.Push(arrayIndex + 1);
                }

                AdvanceColumnIndex();
                SkipWhitespace(TokenType.Any);
            } else if (c == ']') {
                if (elements.Count == 0) {
                    throw new UnexpectedCharacterException(_lines[_lineIndex], _lineIndex, _columnIndex);
                }

                if (danglingComma) {
                    throw new UnexpectedCharacterException(_lines[_lineIndex], _lineIndex, _columnIndex);
                }

                JsonArray arrValue = (JsonArray)elements.Pop();
                arrayIndicies.Pop();
                if (elements.Count == 0) {
                    if (keys.Count > 0 || arrayIndicies.Count > 0) {
                        throw new UnexpectedCharacterException(_lines[_lineIndex], _lineIndex, _columnIndex);
                    }

                    if (arrValue is JsonArray finalArray) {
                        _array = finalArray;
                        return;
                    }
                    throw new UnexpectedCharacterException(_lines[_lineIndex], _lineIndex, _columnIndex);
                }

                if (elements.Peek() is JsonObject jsonObj) {
                    if (keys.Count == 0) {
                        throw new UnexpectedCharacterException(_lines[_lineIndex], _lineIndex, _columnIndex);
                    }

                    jsonObj.Add(keys.Pop(), arrValue);
                } else if (elements.Peek() is JsonArray jsonArr) {
                    var arrayIndex = arrayIndicies.Pop();
                    jsonArr.Add(arrayIndex, arrValue);
                    arrayIndicies.Push(arrayIndex + 1);
                }

                AdvanceColumnIndex();
                SkipWhitespace(TokenType.Any);
            } else if (c == ',') {
                if (elements.Count == 0) {
                    throw new UnexpectedCharacterException(_lines[_lineIndex], _lineIndex, _columnIndex);
                }

                danglingComma = true;
                AdvanceColumnIndex();
                SkipWhitespace(TokenType.Any);
                if (elements.Peek() is JsonObject) {
                    if (!IsQuote()) {
                        throw new UnexpectedCharacterException(_lines[_lineIndex], _lineIndex, _columnIndex);
                    }
                    keys.Push(ParseString());
                    AdvanceColumnIndex();
                    SkipWhitespace(TokenType.Colon);
                    AdvanceColumnIndex();
                    SkipWhitespace(TokenType.Any);
                }
            } else {
                if (elements.Count == 0) {
                    throw new UnexpectedCharacterException(_lines[_lineIndex], _lineIndex, _columnIndex);
                }

                if (elements.Peek() is JsonObject jsonObject) {
                    if (keys.Count == 0) {
                        throw new UnexpectedCharacterException(_lines[_lineIndex], _lineIndex, _columnIndex);
                    }

                    AddValue(jsonObject, keys.Pop());
                    AdvanceColumnIndex();
                    SkipWhitespace(TokenType.Any);
                } else {
                    var arrayIndex = arrayIndicies.Pop();
                    AddValue(arrayIndex, (JsonArray)elements.Peek());
                    arrayIndicies.Push(arrayIndex + 1);
                    AdvanceColumnIndex();
                    SkipWhitespace(TokenType.Any);
                }

                danglingComma = false;
            }
        }
    }
    public T? Get<T>() {
        if (typeof(T) != typeof(JsonObject) && typeof(T) != typeof(JsonArray)) {
            throw new ArgumentException("Type must be JsonObject or JsonArray", nameof(T));
        }

        return typeof(T) switch {
            Type t when t == typeof(JsonObject) => _object is null ? default : (T)(object)_object,
            Type t when t == typeof(JsonArray) => _array is null ? default : (T)(object)_array,
            _ => default
        };
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

            AdvanceColumnIndex();
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

            AdvanceColumnIndex();
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
        AdvanceColumnIndex();
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

                AdvanceColumnIndex();
                if (_columnIndex == _lines[_lineIndex].Length) {
                    throw new MissingClosingQuoteException(_lineIndex);
                }
            }

            AdvanceColumnIndex();
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
            AdvanceColumnIndex();
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
    private void AdvanceColumnIndex() {
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