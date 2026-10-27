// The shared test files import the package namespaces. Nothing here references those packages, so the
// namespaces are declared empty to keep the imports valid, and the standalone copy is imported instead.
global using Linq2GraphQL.TestClientStandalone;
global using Linq2GraphQL.TestClientStandalone.Runtime;
global using Linq2GraphQL.TestClientStandalone.Runtime.Common;
global using Linq2GraphQL.TestClientStandalone.Runtime.Subscriptions;
global using Linq2GraphQL.TestClientStandalone.Runtime.AI;
global using Linq2GraphQL.TestClientStandalone.Runtime.AI.Schema;
global using Linq2GraphQL.TestClientStandalone.Runtime.AI.Tools;

namespace Linq2GraphQL.Client
{
    namespace Common
    {
    }

    namespace AI
    {
        namespace Schema
        {
        }

        namespace Tools
        {
        }
    }

    namespace Subscriptions
    {
    }
}

namespace Linq2GraphQL.TestClient
{
}
