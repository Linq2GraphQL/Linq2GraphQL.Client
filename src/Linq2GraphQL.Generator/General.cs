namespace Linq2GraphQL.Generator;



public static class General
{
    public const string IntrospectionQuery = @"fragment OfTypeFields on __Type {
  name
  kind
}

fragment OfTypeReqursive on __Type {
  ...OfTypeFields
  ofType {
    ...OfTypeFields
    ofType {
      ...OfTypeFields
      ofType {
        ...OfTypeFields
        ofType {
          ...OfTypeFields
          ofType {
            ...OfTypeFields
          }
        }
      }
    }
  }
}

fragment BaseType on __Type {
  name
  description
  kind

  ...OfTypeReqursive
}

query Into {
  __schema {
    types {
     ...BaseType

      interfaces {
        name
      }
      enumValues {
        name
        description
      }

      fields {
        name
        description
        type {
          ...BaseType
        }

        args {
          name
          description
          type {
            ...BaseType
          }
        }
      }
      inputFields {
        name
        description
        type {
          ...BaseType
        }
      }
    }
    queryType {
      name
    }
    mutationType {
      name
    }
    subscriptionType {
      name
    }
  }
}
";

    public const string IntrospectionQueryIncludeDeprecated = @"fragment OfTypeFields on __Type {
  name
  kind
}

fragment OfTypeReqursive on __Type {
  ...OfTypeFields
  ofType {
    ...OfTypeFields
    ofType {
      ...OfTypeFields
      ofType {
        ...OfTypeFields
        ofType {
          ...OfTypeFields
          ofType {
            ...OfTypeFields
          }
        }
      }
    }
  }
}

fragment BaseType on __Type {
  name
  description
  kind

  ...OfTypeReqursive
}

query Into {
  __schema {
    types {
     ...BaseType

      interfaces {
        name
      }
      enumValues(includeDeprecated: true) {
        name
        description
        isDeprecated
        deprecationReason
      }

      fields(includeDeprecated: true) {
        name
        description
        isDeprecated
        deprecationReason
        type {
          ...BaseType
        }

        args {
          name
          description
          type {
            ...BaseType
          }
        }
      }
      inputFields(includeDeprecated: true) {
        name
        description
        isDeprecated
        deprecationReason
        type {
          ...BaseType
        }
      }
    }
    queryType {
      name
    }
    mutationType {
      name
    }
    subscriptionType {
      name
    }
  }
}
";
}