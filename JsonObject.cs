namespace Parser;

using System.Text;
using JsonExceptions;
public sealed class JsonObject {
    private readonly Dictionary<string, string> _strings = [];
    private readonly Dictionary<string, long> _longs = [];
    private readonly Dictionary<string, decimal> _decimals = [];
    private readonly Dictionary<string, bool> _bools = [];
    private readonly Dictionary<string, object?> _nulls = [];
    private readonly Dictionary<string, JsonObject> _objects = [];
    private readonly Dictionary<string, JsonArray> _arrays = [];
    public void Add(string key, string value) {
        _strings.Add(key, value);
    }
    public void Add(string key, long value) {
        _longs.Add(key, value);
    }
    public void Add(string key, decimal value) {
        _decimals.Add(key, value);
    }
    public void Add(string key, bool value) {
        _bools.Add(key, value);
    }
    public void Add(string key, object? value) {
        _nulls.Add(key, value);
    }
    public void Add(string key, JsonObject value) {
        _objects.Add(key, value);
    }
    public void Add(string key, JsonArray value) {
        _arrays.Add(key, value);
    }
    public bool ContainsKey(string key) {
        return _strings.ContainsKey(key) || _longs.ContainsKey(key) ||
        _decimals.ContainsKey(key) || _bools.ContainsKey(key) ||
        _nulls.ContainsKey(key) || _objects.ContainsKey(key) ||
        _arrays.ContainsKey(key);
    }
    public T? Get<T>(string key) {
        try {
            return typeof(T) switch {
                Type t when t == typeof(string) => (T)(object)_strings[key],
                Type t when t == typeof(long) || t == typeof(int) || t == typeof(byte) || t == typeof(short) => (T)(object)_longs[key],
                Type t when t == typeof(decimal) || t == typeof(double) || t == typeof(float) => (T)(object)_decimals[key],
                Type t when t == typeof(bool) => (T)(object)_bools[key],
                Type t when t == typeof(JsonObject) => (T)(object)_objects[key],
                Type t when t == typeof(JsonArray) => (T)(object)_arrays[key],
                _ => (T?)_nulls[key]
            };
        } catch {
            throw new ValueNotFoundException(typeof(T), key);
        }
    }
    public override string ToString() {
        Dictionary<string, string> values = [];
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
            values.Add(key, value.ToString());
        }
        foreach (var (key, value) in _arrays) {
            values.Add(key, value.ToString());
        }

        if (values.Count == 0) {
            return "{}";
        }

        StringBuilder sb = new();
        sb.Append('{');
        foreach (var (key, value) in values) {
            sb.Append($"\"{key}\": {value}, ");
        }
        sb.Remove(sb.Length - 2, 2);
        sb.Append('}');
        return sb.ToString();
    }
}