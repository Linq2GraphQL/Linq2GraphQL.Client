// The shared test files import the package namespaces. Nothing here references those packages, so the
// namespaces are declared empty to keep the imports valid, and the standalone copy is imported instead.
global using Linq2GraphQL.TestClientStandalone;
global using Linq2GraphQL.TestClientStandalone.Runtime;
global using Linq2GraphQL.TestClientStandalone.Runtime.Common;
global using Linq2GraphQL.TestClientStandalone.Runtime.Subscriptions;

namespace Linq2GraphQL.Client
{
    namespace Common
    {
    }

    namespace Subscriptions
    {
    }
}

namespace Linq2GraphQL.TestClient
{
}
