namespace DynamicJsonParser;

using System;
using System.Text;
public sealed class JsonObject : JsonEntity {
    private readonly Dictionary<string, string> _strings = [];
    private readonly Dictionary<string, long> _longs = [];
    private readonly Dictionary<string, decimal> _decimals = [];
    private readonly Dictionary<string, bool> _bools = [];
    private readonly HashSet<string> _nulls = [];
    private readonly Dictionary<string, JsonObject> _objects = [];
    private readonly Dictionary<string, JsonArray> _arrays = [];
    internal void Add(string key, string value) {
        _strings.Add(key, value);
    }
    internal void Add(string key, long value) {
        _longs.Add(key, value);
    }
    internal void Add(string key, decimal value) {
        _decimals.Add(key, value);
    }
    internal void Add(string key, bool value) {
        _bools.Add(key, value);
    }
    internal void Add(string key) {
        _nulls.Add(key);
    }
    internal void Add(string key, JsonObject value) {
        _objects.Add(key, value);
    }
    internal void Add(string key, JsonArray value) {
        _arrays.Add(key, value);
    }

    /// <summary>
    /// Checks if the specified key exists in the <see cref="JsonObject"/>.
    /// </summary>
    /// <param name="key">The key to be searched for.</param>
    /// <returns><see langword="true"/> if the key exists, otherwise <see langword="false"/>.</returns>
    public bool ContainsKey(string key) {
        return _strings.ContainsKey(key) || _longs.ContainsKey(key) ||
        _decimals.ContainsKey(key) || _bools.ContainsKey(key) ||
        _nulls.Contains(key) || _objects.ContainsKey(key) ||
        _arrays.ContainsKey(key);
    }

    /// <summary>
    /// Returns the value at the specified key. If the key is not found, returns <see langword="null"/>.
    /// It is recommended to use <see cref="ContainsKey"/> to check if the key exists.
    /// </summary>
    /// <typeparam name="T">The type of the returned object.</typeparam>
    /// <param name="key">The key associated with the return value.</param>
    /// <returns>The object at the given key, or <see langword="null"/> if it isn't found.</returns>
    public T? Get<T>(string key) {
        try {
            return typeof(T) switch {
                Type t when t == typeof(string) => (T)(object)_strings[key],
                Type t when t == typeof(long) || t == typeof(int) || t == typeof(byte) || t == typeof(short) => (T)Convert.ChangeType(_longs[key], typeof(T)),
                Type t when t == typeof(decimal) || t == typeof(double) || t == typeof(float) => (T)Convert.ChangeType(_decimals[key], typeof(T)),
                Type t when t == typeof(bool) => (T)(object)_bools[key],
                Type t when t == typeof(JsonObject) => (T)(object)_objects[key],
                Type t when t == typeof(JsonArray) => (T)(object)_arrays[key],
                _ => default
            };
        } catch {
            return default;
        }
    }
    /// <summary>
    /// Returns a list of all the keys in the <see cref="JsonObject"/>.
    /// </summary>
    /// <returns>A list containing every top-level key in this object.</returns>
    public List<string> Keys() {
        return [
            .. _strings.Keys,
            .. _longs.Keys,
            .. _decimals.Keys,
            .. _bools.Keys,
            .. _nulls,
            .. _objects.Keys,
            .. _arrays.Keys,
        ];
    }
    /// <summary>
    /// Returns the type of the object at the specified key. If the key is not found, returns <see langword="null"/>.
    /// It is recommended to use <see cref="ContainsKey"/> to check if the key exists.
    /// </summary>
    /// <param name="key">The position at which the value is checked.</param>
    /// <returns>The <see cref="System.Type"/> of the value associated with the given key.</returns>
    public Type? Type(string key) {
        if (_strings.ContainsKey(key)) {
            return typeof(string);
        }
        if (_longs.ContainsKey(key)) {
            return typeof(long);
        }
        if (_decimals.ContainsKey(key)) {
            return typeof(decimal);
        }
        if (_bools.ContainsKey(key)) {
            return typeof(bool);
        }
        if (_nulls.Contains(key)) {
            return typeof(object);
        }
        if (_objects.ContainsKey(key)) {
            return typeof(JsonObject);
        }
        if (_arrays.ContainsKey(key)) {
            return typeof(JsonArray);
        }
        return null;
    }
    /// <summary>
    /// Returns the number of elements in the <see cref="JsonObject"/>.
    /// </summary>
    /// <returns>The number of elements in this object.</returns>
    public int Count() {
        return _strings.Count + _longs.Count + _decimals.Count + _bools.Count +
        _nulls.Count + _objects.Count + _arrays.Count;
    }
    internal override string Serialize() {
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
        foreach (var key in _nulls) {
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