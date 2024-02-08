namespace Parser;

using System.Text;
using JsonExceptions;
public sealed class JsonArray {
    private readonly Dictionary<int, string> _strings = [];
    private readonly Dictionary<int, long> _longs = [];
    private readonly Dictionary<int, decimal> _decimals = [];
    private readonly Dictionary<int, bool> _bools = [];
    private readonly Dictionary<int, object?> _nulls = [];
    private readonly Dictionary<int, JsonObject> _objects = [];
    private readonly Dictionary<int, JsonArray> _arrays = [];
    public void Add(int key, string value) {
        _strings.Add(key, value);
    }
    public void Add(int key, long value) {
        _longs.Add(key, value);
    }
    public void Add(int key, decimal value) {
        _decimals.Add(key, value);
    }
    public void Add(int key, bool value) {
        _bools.Add(key, value);
    }
    public void Add(int key, object? value) {
        _nulls.Add(key, value);
    }
    public void Add(int key, JsonObject value) {
        _objects.Add(key, value);
    }
    public void Add(int key, JsonArray value) {
        _arrays.Add(key, value);
    }
    public T? Get<T>(int index) {
        try {
            return typeof(T) switch {
                Type t when t == typeof(string) => (T)(object)_strings[index],
                Type t when t == typeof(long) || t == typeof(int) || t == typeof(byte) || t == typeof(short) => (T)(object)_longs[index],
                Type t when t == typeof(decimal) || t == typeof(double) || t == typeof(float) => (T)(object)_decimals[index],
                Type t when t == typeof(bool) => (T)(object)_bools[index],
                Type t when t == typeof(JsonObject) => (T)(object)_objects[index],
                Type t when t == typeof(JsonArray) => (T)(object)_arrays[index],
                _ => (T?)_nulls[index]
            };
        } catch {
            throw new ValueNotFoundException(typeof(T), index);
        }
    }
    public override string ToString() {
        Dictionary<int, string> values = [];
        foreach (var (key, value) in _strings) {
            values.Add(key, $"\"{value}\"");
        }
        foreach (var (key, value) in _longs) {
            values.Add(key, value.ToString());
        }
        foreach (var (key, value) in _decimals) {
            values.Add(key, value.ToString());
        }
        foreach (var (key, value) in _bools) {
            values.Add(key, value.ToString().ToLower());
        }
        foreach (var (key, value) in _nulls) {
            values.Add(key, "null");
        }
        foreach (var (key, value) in _objects) {
            values.Add(key, value.ToString()!);
        }
        foreach (var (key, value) in _arrays) {
            values.Add(key, value.ToString());
        }
        if (values.Count == 0) {
            return "[]";
        }

        StringBuilder sb = new();
        sb.Append('[');
        foreach (var (key, value) in values) {
            sb.Append(value);
            sb.Append(", ");
        }
        sb.Remove(sb.Length - 2, 2);
        sb.Append(']');
        return sb.ToString();
    }
}