using System.Numerics;
using System.Text;

namespace DynamicJsonParser;
internal class SimdParser {
    private readonly string _json;
    private int _index = 0;
    private static readonly int VECTOR_SIZE = Vector<short>.Count;

    private readonly Vector<short> _quoteVector = new(ConvertStringToShortArray(new string('"', VECTOR_SIZE)));
    private readonly Vector<short> _openingBracketVector = new(ConvertStringToShortArray(new string('[', VECTOR_SIZE)));

    private JsonObject? _object;
    private JsonArray? _array;
    private bool _isParsed = false;

    public SimdParser(List<string> lines) {
        StringBuilder sb = new();
        lines.ForEach(line => sb.Append(line));
        _json = sb.ToString();

        Parse();
    }
    private void Parse() {
        List<ValueTuple<int, int>> stringLocations = [(-1, -1)];
        short[] result = new short[VECTOR_SIZE];

        for (int i = 0; i < _json.Length; i += VECTOR_SIZE) {
            int length = Math.Min(VECTOR_SIZE, _json.Length - i);

            FindQuotes(result, i, length);

            FindStrings(stringLocations, result, i, length);
        }
    }
    private void FindStrings(List<ValueTuple<int, int>> stringLocations, short[] result, int index, int length) {
        ValueTuple<int, int> value = stringLocations.Last();
        if (value.Item2 is not -1) {
            value = (-1, -1);
            stringLocations.Add(value);
        }

        var start = value.Item1;

        for (int j = index; j < index + length; j++) {
            if (result[j] == 1 && QuoteIsValid(j)) {
                if (start == -1) {
                    start = j;
                    continue;
                }

                stringLocations.Add(new(start, j));
                start = -1;
            }
        }
    }
    private bool QuoteIsValid(int start) {
        int backslashCount = 0;
        for (int i = start; i >= 0; i--) {
            if (_json[i] == '\\') {
                backslashCount++;
            } else {
                break;
            }
        }
        return backslashCount % 2 == 0;
    }

    private void FindQuotes(short[] result, int index, int length) {
        var stringBytes = ConvertStringToShortArray(_json.Substring(index, length));
        Vector<short> stringVector = new(stringBytes);

        (stringVector & _quoteVector).CopyTo(result);
    }
    private static short[] ConvertStringToShortArray(string str) {
        short[] result = new short[str.Length];
        char[] chars = str.ToCharArray();

        Buffer.BlockCopy(chars, 0, result, 0, chars.Length * sizeof(char));
        return result;
    }
}