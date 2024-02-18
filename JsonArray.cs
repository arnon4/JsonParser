namespace DynamicJsonParser;

using System.Text;

/// <summary>
/// Represents a JSON array.
/// </summary>
public sealed class JsonArray : JsonEntity {
    private readonly Dictionary<int, string> _strings = [];
    private readonly Dictionary<int, long> _longs = [];
    private readonly Dictionary<int, decimal> _decimals = [];
    private readonly Dictionary<int, bool> _bools = [];
    private readonly List<int> _nulls = [];
    private readonly Dictionary<int, JsonObject> _objects = [];
    private readonly Dictionary<int, JsonArray> _arrays = [];
    internal void Add(int key, string value) {
        _strings.Add(key, value);
    }
    internal void Add(int key, long value) {
        _longs.Add(key, value);
    }
    internal void Add(int key, decimal value) {
        _decimals.Add(key, value);
    }
    internal void Add(int key, bool value) {
        _bools.Add(key, value);
    }
    internal void Add(int key) {
        _nulls.Add(key);
    }
    internal void Add(int key, JsonObject value) {
        _objects.Add(key, value);
    }
    internal void Add(int key, JsonArray value) {
        _arrays.Add(key, value);
    }
    /// <summary>
    /// Retrieves the value at the specified index. If the index is not found, returns <see langword="null"/>.
    /// It is recommended to use <see cref="Count"/> to check if the index exists.
    /// </summary>
    /// <typeparam name="T">The object type at the given index.</typeparam>
    /// <param name="index">The position from which the object is retrieved.</param>
    /// <returns>An object of type <typeparamref name="T"/>, or <see langword="null"/> if it doesn't exist.</returns>
    public T? Get<T>(int index) {
        try {
            return typeof(T) switch {
                Type t when t == typeof(string) => (T)(object)_strings[index],
                Type t when t == typeof(long) || t == typeof(int) || t == typeof(byte) || t == typeof(short) => (T)Convert.ChangeType(_longs[index], typeof(T)),
                Type t when t == typeof(decimal) || t == typeof(double) || t == typeof(float) => (T)Convert.ChangeType(_decimals[index], typeof(T)),
                Type t when t == typeof(bool) => (T)(object)_bools[index],
                Type t when t == typeof(JsonObject) => (T)(object)_objects[index],
                Type t when t == typeof(JsonArray) => (T)(object)_arrays[index],
                _ => default
            };
        } catch {
            return default;
        }
    }
    /// <summary>
    /// Returns the type of the object at the specified index. If the index is not found, returns <see langword="null"/>.
    /// It is recommended to use <see cref="Count"/> to check if the index exists.
    /// </summary>
    /// <param name="index">The position at which the type is checked.</param>
    /// <returns>The <see cref="System.Type"/> of the object at the requested index, or <see langword="null"/> if it doesn't exist.</returns>
    public Type? Type(int index) {
        if (_strings.ContainsKey(index)) {
            return typeof(string);
        }
        if (_longs.ContainsKey(index)) {
            return typeof(long);
        }
        if (_decimals.ContainsKey(index)) {
            return typeof(decimal);
        }
        if (_bools.ContainsKey(index)) {
            return typeof(bool);
        }
        if (_nulls.Contains(index)) {
            return typeof(object);
        }
        if (_objects.ContainsKey(index)) {
            return typeof(JsonObject);
        }
        if (_arrays.ContainsKey(index)) {
            return typeof(JsonArray);
        }
        return null;
    }
    /// <summary>
    /// Returns the number of elements in the <see cref="JsonArray"/>.
    /// </summary>
    /// <returns>The number of elements in this object.</returns>
    public int Count() {
        return _strings.Count + _longs.Count +
            _decimals.Count + _bools.Count +
            _nulls.Count + _objects.Count + _arrays.Count;
    }
    /// <summary>
    /// Returns a new <see cref="JsonArray"/> with all the unique items in the original <see cref="JsonArray"/>.
    /// </summary>
    /// <returns>A <see cref="JsonArray"/> with duplicate values removed.</returns>
    public JsonArray Unique() {
        JsonArray unique = new();
        foreach (var item in _strings.Values.ToHashSet()) {
            unique.Add(unique.Count(), item);
        }
        foreach (var item in _longs.Values.ToHashSet()) {
            unique.Add(unique.Count(), item);
        }
        foreach (var item in _decimals.Values.ToHashSet()) {
            unique.Add(unique.Count(), item);
        }
        foreach (var item in _bools.Values.ToHashSet()) {
            unique.Add(unique.Count(), item);
        }
        foreach (var item in _nulls) {
            unique.Add(unique.Count(), item);
        }
        foreach (var item in _objects.Values.ToHashSet()) {
            unique.Add(unique.Count(), item);
        }
        foreach (var item in _arrays.Values.ToHashSet()) {
            unique.Add(unique.Count(), item);
        }
        return unique;
    }
    internal override string Serialize() {
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
        foreach (var key in _nulls) {
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