namespace Linq2GraphQL.Client
{
    public class CustomScalar
    {
        internal string InternalValue { get; set; }

        public override string ToString()
        {
            return base.ToString();
        }

        public virtual string Value
        {
            get { return InternalValue; }

            set { InternalValue = value; }

        }
    }
}
