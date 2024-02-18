namespace DynamicJsonParser;

/// <summary>
/// Represents a JSON entity, either an object or an array.
/// </summary>
public abstract class JsonEntity {
    internal abstract string Serialize();
    public sealed override string ToString() {
        return Serialize();
    }
}