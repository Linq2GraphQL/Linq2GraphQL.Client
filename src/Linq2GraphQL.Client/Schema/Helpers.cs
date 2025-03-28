namespace Linq2GraphQL.Client.Schema;

internal static class Helpers
{
    internal const string SchemaQuery = @"{
                                          __schema {
                                            types {
                                              name
                                              fields {
                                                name
                                              }
                                            }
                                          }
                                        }";

    internal const string SchemaQueryIncludeDeprecated = @"{
                                          __schema {
                                            types {
                                              name
                                              fields(includeDeprecated: true) {
                                                name
                                              }
                                            }
                                          }
                                        }";
}