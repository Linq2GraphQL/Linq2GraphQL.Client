namespace Linq2GraphQL.Client;

public class CustomScalar
{
    internal object InternalValue { get; set; }

    public virtual object Value
    {
        get => InternalValue;

        set => InternalValue = value;
    }

    public override string ToString()
    {
        return InternalValue?.ToString();
    }
}