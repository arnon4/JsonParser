namespace DynamicJsonParser;

public abstract class JsonEntity {
    internal abstract string Serialize();
    public override string ToString() {
        return Serialize();
    }
}