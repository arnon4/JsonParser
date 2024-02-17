namespace DynamicJsonParser;

public abstract class JsonEntity {
    internal abstract string Serialize();
    public sealed override string ToString() {
        return Serialize();
    }
}