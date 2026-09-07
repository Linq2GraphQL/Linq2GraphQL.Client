namespace Linq2GraphQL.Generator
{
    public class GeneratorSettings
    {
        public static GeneratorSettings Current { get; set; }

        public bool Nullable { get; set; }

        /// <summary>
        /// GraphQL scalar name to CLR type for this run: the built-in table with any
        /// <c>scalarMappings</c> overrides from the settings file applied on top.
        /// </summary>
        public Dictionary<string, (string Name, Type type)> TypeMapping { get; set; } =
            Helpers.CreateTypeMapping();
    }
}
