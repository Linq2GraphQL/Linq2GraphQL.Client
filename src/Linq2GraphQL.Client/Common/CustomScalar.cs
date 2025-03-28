namespace Linq2GraphQL.Client
{
    public class CustomScalar
    {
        internal object InternalValue { get; set; }

        public override string ToString()
        {
            return InternalValue?.ToString();
        }

        public virtual object Value
        {
            get { return InternalValue; }

            set { InternalValue = value; }

        }
    }
}
