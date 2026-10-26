<div align='center'>

<img src=https://raw.githubusercontent.com/Linq2GraphQL/Linq2GraphQL.Client/main/Logo.svg alt="logo" width=100 height=100 />

<h1>Linq2GraphQL.Client</h1>
<p>A straightforward Linq to GraphQL Client</p>

<h4> <a href="https://linq2graphql.com"> Documentation </a> <span> · </span> <a href="https://github.com/Linq2GraphQL/Linq2GraphQL.Client/issues"> Report Bug </a> <span> · </span> <a href="https://github.com/Linq2GraphQL/Linq2GraphQL.Client/issues"> Request Feature </a> </h4>

[![Build](https://github.com/Linq2GraphQL/Linq2GraphQL.Client/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/Linq2GraphQL/Linq2GraphQL.Client/actions/workflows/ci.yml)

</div>

# Introduction

Linq2GraphQL generates C# classes from the GraphQL schema and and togheter with the nuget package Linq2GraphQL.Client it
makes it possible to query the server using Linq expressions.

A simple query that will get the first 10 orders with the primitive properties of orders and the connected customer.

```cs
var orders = await sampleClient
    .Query
        .Orders(first: 10)
        .Include(e => e.Orders.Select(e => e.Customer))
        .Select(e => e.Orders)
        .ExecuteAsync();
```

A example mutation where we add a new customer and return the Customer Id.

```cs
 var customerId = await sampleClient
     .Mutation
     .AddCustomer(new CustomerInput
     {
         CustomerId = Guid.NewGuid(),
         CustomerName = "New Customer",
         Status = CustomerStatus.Active
     })
     .Select(e=> e.CustomerId)
     .ExecuteAsync();
```     

# Getting Started

## Generate Client code

There are two options to generate the client code from the GraphQL schema.
Use the online tool to <a href="https://linq2graphql.com/generate-client"> generate</a> or install
Linq2GraphQL.Generator as a tool.

Install/Update Tool:

    dotnet tool update Linq2GraphQL.Generator -g --prerelease

Usage:

      Linq2GraphQL.Generator <endpoint> [options]
    
    Arguments:
      <endpoint>  Endpoint of the GraphQL service
    
    Options:
      -o, --output <output>        Output folder, relative to current location [default: Linq2GraphQL_Generated]
      -n, --namespace <namespace>  Namespace of generated classes [default: YourNamespace]
      -c, --client <client>        Name of the generated client [default: GraphQLClient]
      -t, --token <token>          Bearertoken for authentication
      -s, --subscriptions          Include subscriptions (Exprimental)
      -es --enum-strategy          If AddUnknownOption all enums will have an additional Unknown option
      -nu --nullabel               Nullable client [default: false]
	  -d  --deprecated			   Include Deprecated as Obsolete
      -sc --schema                 Schema file (SDL) to generate from instead of an endpoint
      -ag --agent                  Agent mode: also emit the schema as SDL (see docs/design/agent-mode.md)
      -sa --standalone             Write a copy of the runtime into the output, see Standalone clients below
      -cf --config                 Json settings file, see Configuration file below

As an example:

    Linq2GraphQL https://spacex-production.up.railway.app/ -c="SpaceXClient" -n="SpaceX" -o="Generated"

Would generate a client from url *https://spacex-production.up.railway.app/* with the name *SpaceXClient* in the
namespace *SpaceX* to folder *Generated*

### Generating from a schema file

Instead of an endpoint you can point the generator at a schema written in SDL, for example one exported
from HotChocolate (`dotnet run -- schema export`) or taken from a schema registry. No server needs to be
running:

    Linq2GraphQL --schema schema.graphql -c="SpaceXClient" -n="SpaceX" -o="Generated"

The result is the same client the endpoint would give. An endpoint and a schema file are mutually
exclusive; passing both is an error.

## Configuration file

Every option above can also be set in a json file, which keeps a long command line out of your build
scripts and gives you somewhere to check the settings in:

    Linq2GraphQL --config linq2graphql.json

If the file is called `linq2graphql.json` and sits in the current directory it is picked up on its
own, so a repository with one in it just needs:

    Linq2GraphQL

The generator prints the path of the file it read. An explicit `--config` always wins over the
discovered one, and pointing `--config` at a file that does not exist is an error - only the default
file is allowed to be absent.

```json
{
  "endpoint": "https://spacex-production.up.railway.app/",
  "client": "SpaceXClient",
  "namespace": "SpaceX",
  "output": "Generated",
  "nullable": false,
  "subscriptions": false,
  "deprecated": false,
  "enumStrategy": "FailIfMissing",
  "scalarMappings": {
    "DateTime": "System.DateTime",
    "BigInt": "long",
    "Json": null
  }
}
```

| Setting          | Type   | Default                  | Command line          |
|------------------|--------|--------------------------|-----------------------|
| `endpoint`       | string | *required*               | `<endpoint>` argument |
| `schema`         | string | none                     | `-sc`, `--schema`     |
| `output`         | string | `Linq2GraphQL_Generated` | `-o`, `--output`      |
| `namespace`      | string | `YourNamespace`          | `-n`, `--namespace`   |
| `client`         | string | `GraphQLClient`          | `-c`, `--client`      |
| `token`          | string | none                     | `-t`, `--token`       |
| `subscriptions`  | bool   | `false`                  | `-s`, `--subscriptions` |
| `enumStrategy`   | string | `FailIfMissing`          | `-es`, `--enum-strategy` |
| `nullable`       | bool   | `false`                  | `-nu`, `--nullable`   |
| `deprecated`     | bool   | `false`                  | `-d`, `--deprecated`  |
| `agent`          | bool   | `false`                  | `-ag`, `--agent`      |
| `standalone`     | bool   | `false`                  | `-sa`, `--standalone` |
| `scalarMappings` | object | none                     | *no equivalent*       |
| `descriptionAttributes` | bool | `true`              | *no equivalent*       |

`endpoint` is required only in the sense that it has to come from somewhere - the file or the
command line argument - unless `schema` names an SDL file to generate from instead. `enumStrategy` takes `AddUnknownOption` to give every generated enum an extra
`Unknown` member; any other value means `FailIfMissing`. `token` is settable here for completeness,
but a bearer token is usually better passed as `-t` than checked into a file. `descriptionAttributes`
controls whether schema descriptions are also emitted as `[Description]` attributes, next to the
XML doc comments, so tools that read them at runtime (JSON schema exporters, OpenAPI, AI function
factories) see the schema's text; set it to `false` to leave them out.

Setting names are matched case insensitively. A name that is not in the table above fails the run,
so a typo like `scalarMapping` is reported rather than silently ignored. `scalarMappings` and
`descriptionAttributes` have no command line equivalent, so they are the settings a discovered file
supplies that you cannot override back off from the command line.

Everything in the file is optional, and an option you pass explicitly on the command line always wins
over the file - so you can keep the shared settings in the file and override one of them for a single
run:

    Linq2GraphQL --config linq2graphql.json -o="SomewhereElse"

### Mapping scalars to your own types

By default the generator maps the well known GraphQL scalars to CLR types (`Int` to `int`, `DateTime`
to `DateTimeOffset`, and so on) and generates a `CustomScalar` class for every scalar it does not
recognise. `scalarMappings` lets you override both halves of that:

* Map a scalar to a **simple type** and it is emitted as that type - no `CustomScalar` class is
  generated for it. `"BigInt": "long"` gives you `public long? Id { get; set; }`.
* Change an existing mapping the same way. `"DateTime": "System.DateTime"` emits `DateTime` instead
  of the default `DateTimeOffset`.
* Map a scalar to **null** to opt it out of the built-in mapping, so a `CustomScalar` class is
  generated for it instead and you control the conversion yourself.

Scalar names are matched case insensitively. The target must be one of the supported simple types -
`bool`, `byte`, `sbyte`, `char`, `short`, `ushort`, `int`, `uint`, `long`, `ulong`, `float`,
`double`, `decimal`, `string`, `Guid`, `Uri`, `DateTime`, `DateTimeOffset`, `DateOnly`, `TimeOnly`,
`TimeSpan` - written either as the C# keyword (`long`), the type name (`Int64`) or its full name
(`System.Int64`). Anything else fails the run with an error rather than generating a client that does
not compile.

### Standalone clients

A generated client normally references the `Linq2GraphQL.Client` package, and an app gets one version
of it however many clients it uses. When clients are shipped as packages of their own by different
teams, an app that references clients made with different generator versions ends up with whichever
runtime NuGet picks. With `--standalone` (`-sa`) the generator instead writes a copy of the runtime into
`Runtime/` in the output, under the namespace `<namespace>.Runtime`, so the client depends on nothing
from Linq2GraphQL:

    Linq2GraphQL https://spacex-production.up.railway.app/ -c="SpaceXClient" -n="SpaceX" -o="Generated" --standalone

`Runtime/RuntimeInfo.cs` records the runtime version and the packages the copy needs, and the generator
prints them as `PackageReference` lines to add instead of `Linq2GraphQL.Client`. Code using the client
does not change; runtime types you name yourself (`GraphClientOptions`, `GraphQueryExecutionException`)
come from `SpaceX.Runtime`. A runtime fix reaches the client only when you regenerate, the `Runtime`
folder is rewritten on every run (don't edit it), and `Linq2GraphQL.Client.AI` does not work with
standalone clients yet.

## Add Nuget

Latest
stable: [![Nuget](https://img.shields.io/nuget/v/Linq2GraphQL.Client.svg)](https://www.nuget.org/packages/Linq2GraphQL.Client)
<br/>
Latest
prerelease: [![Nuget](https://img.shields.io/nuget/vpre/Linq2GraphQL.Client.svg)](https://www.nuget.org/packages/Linq2GraphQL.Client)

    dotnet add package Linq2GraphQL.Client --prerelease

## Dependency Injection

The client adds a set of extensions to make it easier to add the client to dependency injection.
As an example this would add SpaceXClient to the container:

```cs
services
    .AddSpaceXClient(x =>
     {
         x.UseSafeMode = false;
     })
    .WithHttpClient(
        httpClient => 
        { 
            httpClient.BaseAddress = new Uri("https://spacex-production.up.railway.app/"); 
        });
```

## Request headers and access tokens

`ConfigureRequest` in the client options runs on every request the client sends, for headers that are the
same for everyone:

```cs
services.AddSpaceXClient(x =>
{
    x.ConfigureRequest = (request, ct) =>
    {
        request.Headers.Add("X-Api-Version", "2");
        return ValueTask.CompletedTask;
    };
});
```

For headers that depend on the current user, such as an on-behalf-of token, derive a client instead. A
derived client shares the original's connection and settings, adds its own hook on top, and leaves the
original unchanged, so it is cheap to create one per user or per request:

```cs
var userClient = spaceXClient.WithAccessToken(ct => tokens.GetAccessTokenForUserAsync(user, ct));
var launches = await userClient.Query.Launches().Select().ExecuteAsync();
```

`WithAccessToken` sets `Authorization: Bearer <token>` and asks the delegate for the token on every request;
`WithRequestConfiguration` takes any hook. The hooks also apply to subscriptions: server-sent events through
the HTTP pipeline, WebSocket upgrades by copying the headers onto the upgrade request (initial connect and
each reconnect — a refreshed token is picked up automatically). On Blazor WebAssembly, where the browser
forbids headers on WebSocket requests, set `GraphClientOptions.SendAuthorizationInConnectionInit` to send
the token in the `connection_init` payload instead.

## Agent mode (preview)

`Linq2GraphQL.Client.AI` lets an AI agent query your API in plain GraphQL, limited to the operations you
allow. Generate the client with `--agent` (`-ag`) so it carries the schema, then turn it into tools for
Microsoft.Extensions.AI:

```cs
var tools = spaceXClient.AsAgentTools(o =>
{
    o.AllowQuery(c => c.Query.Launches);
    o.AllowQuery(c => c.Query.Rockets);
    o.Describe<Launch>(l => l.Details, "Free-text mission summary; may be empty.");
    o.Instructions("Dates are UTC.");
    o.AccessToken = ct => tokens.GetAccessTokenForUserAsync(user, ct);
    o.OnQuery(log => logger.LogInformation("{Query} {Errors}", log.Query, log.ValidationErrors));
});

var response = await chatClient.GetResponseAsync(question, new ChatOptions { Tools = [..tools] });
```

The agent gets four tools: `search_schema`, `describe_operation`, `describe_type` and `graphql_query`. It
sees only the allowed operations and the types they reach, and every query is validated against that
scope before it is sent. The server stays responsible for authorization; give the agent a down-scoped or
on-behalf-of token.

`test/Linq2GraphQL.Agent.Sample` runs an agent against the test server. It uses Azure AI Foundry through
your Azure sign-in (`az login`, no keys), or any OpenAI-compatible endpoint such as Ollama:

```powershell
dotnet user-secrets --project test/Linq2GraphQL.Agent.Sample set Agent:Endpoint "https://<resource>.cognitiveservices.azure.com/"
dotnet user-secrets --project test/Linq2GraphQL.Agent.Sample set Agent:Deployment "<deployment>"
dotnet run --project test/Linq2GraphQL.Agent.Sample            # chat
dotnet run --project test/Linq2GraphQL.Agent.Sample -- eval    # run questions.json and report
```

For Ollama, set `Agent:Provider` to `OpenAICompatible` and `Agent:Endpoint` to `http://localhost:11434/v1/`.
Add `--verbose` to see each tool call. See [the design doc](docs/design/agent-mode.md) for the full picture.

## Safe Mode

Turning on *SafeMode* will make the client before the first request to do an introspection query to the endpoint. The
schema will be used to make sure that any auto included properties are available. This is an advanced feature that
require the endpoint to support introspection. By default safe mode is turned of.

# Error Handling

## Throwing Behavior (Default)

By default, `ExecuteAsync` throws `GraphQueryExecutionException` when the GraphQL response contains errors:

```cs
try
{
    var customer = await sampleClient
        .Query
        .Customer(id: "abc-123")
        .Select(e => e)
        .ExecuteAsync();
}
catch (GraphQueryExecutionException ex)
{
    foreach (var error in ex.Errors)
    {
        Console.WriteLine($"Error: {error.Message}");
        Console.WriteLine($"Code: {error.ErrorCode}");
    }
}
catch (GraphQueryRequestException ex)
{
    Console.WriteLine($"HTTP error: {ex.Message}");
}
```

`GraphQueryExecutionException` provides:
- **Errors** — list of `GraphQueryError` with `Message`, `Locations`, `Path`, `Extensions`
- **ErrorCode** — classified error code (Authentication, Forbidden, Validation, BadRequest, etc.)
- **Extensions** — full error extensions from the server (custom codes, status codes, etc.)
- **GraphQLQuery** / **GraphQLVariables** — the request that caused the error

## Result API (No Throw)

Use `ExecuteWithResultAsync` to get both data and errors without exceptions:

```cs
var result = await sampleClient
    .Query
    .Customer(id: "abc-123")
    .Select(e => e)
    .ExecuteWithResultAsync();

if (result.HasErrors)
{
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"{error.ErrorCode}: {error.Message}");
    }
}

if (result.HasData)
{
    Console.WriteLine(result.Data.CustomerName);
}
```

`GraphResult<T>` provides:
- **Data** — the response data (may be present even with partial errors)
- **Errors** — list of `GraphQueryError`
- **Extensions** — response-level extensions from the server
- **HasErrors** / **HasData** — quick checks
- **EnsureNoErrors()** — throws if errors exist (opt-in to throwing)

## Error Codes

`GraphQueryError.ErrorCode` classifies known error codes from popular GraphQL servers:

| Code | Enum |
|------|------|
| `UNAUTHENTICATED` | `GraphErrorCode.Authentication` |
| `FORBIDDEN` | `GraphErrorCode.Forbidden` |
| `BAD_USER_INPUT` | `GraphErrorCode.BadRequest` |
| `GRAPHQL_VALIDATION_FAILED` | `GraphErrorCode.Validation` |
| `INTERNAL_SERVER_ERROR` | `GraphErrorCode.InternalServerError` |
| `RATE_LIMITED` | `GraphErrorCode.RateLimited` |
| `TIMEOUT` | `GraphErrorCode.Timeout` |

Unrecognized codes return `GraphErrorCode.Unknown`.

## Cursor Paging

`NextPageWithResultAsync` and `PreviousPageWithResultAsync` follow the same pattern:

```cs
var pager = sampleClient
    .Query
    .Orders(first: 10)
    .AsPager();

var page = await pager.NextPageWithResultAsync();
if (page.HasErrors) { /* handle */ }
```

`GetAllPagesAsync` pages forward from the first page until the server reports no next page. Pages are fetched one at a time as you enumerate, so you can stop early; pass `maxPages` to cap the number of requests:

```cs
await foreach (var orders in pager.GetAllPagesAsync(maxPages: 50))
{
    // one page at a time
}
```

`GetAllPagesWithResultAsync` yields each page's `GraphResult<T>` and stops after the first page with errors.

# Subscriptions

`StartAsync` returns a cold `IObservable<TResult>`. Three transports are supported via `GraphClientOptions.SubscriptionProtocol`:

- `GraphQLWebSocket` (default) — `graphql-transport-ws` protocol: sends `connection_init` (with `ConnectionInitPayload` if configured), waits up to 10 s for `connection_ack`, then subscribes. Transient disconnects (connection lost, server-initiated close, or no frame at all for 30 s) reconnect and **resubscribe** on each new session; events during the gap are not replayed. Reconnection is bounded: up to `SubscriptionRetryAttempts` attempts with exponential backoff (1 s, 2 s, 4 s ... capped at 30 s) plus 300-3000 ms jitter; a successful `connection_ack` resets the budget. When the budget is spent the stream fails once with an `IOException` whose inner exception is the last failure. Protocol-level close codes `4004`, `4005`, `4400`, `4401`, `4406`, `4409`, `4429`, `4500` (and fatal internal `1xxx` codes) are fatal and never retried, per the graphql-ws reference client. `4403`, `4408`, `4499` and `4504` are retried.
- `ApolloWebSocket` — the legacy `graphql-ws` (Apollo) subprotocol: `connection_init` / `start` / `stop`, with `data` messages instead of `next`.
- `ServerSentEvents` — POSTs the subscription and reads the `text/event-stream` response (GraphQL-over-SSE, distinct-connections mode). Per-event `error` frames throw; a `complete` frame completes the stream. Unknown event types (e.g. `ka` keep-alives) are ignored per the WHATWG SSE spec.

Options:

- `SubscriptionMaxBufferedEvents` (default 128) and `SubscriptionOverflowPolicy` (`Fault` default, `DropOldest`, `KeepLatest`) control the buffer between transport and observer.
- `SubscriptionRetryAttempts` (WebSocket, default 5) bounds reconnect attempts after a dropped connection; `0` fails on the first drop.
- `SubscriptionIdleTimeout` (SSE only, default off) fails the subscription when no bytes arrive — including keep-alive comments — within the window.
- `ConnectionInitPayload` sets the `connection_init` payload for the WebSocket transports. Return any JSON object the server expects as connection parameters, e.g. `new Dictionary<string, object> { ["Authorization"] = $"Bearer {token}" }` (the graphql-ws/Apollo convention). Called before every (re)connect, before the socket opens. Supersedes the obsolete `WSConnectionInitPayload`, which is limited to a `GraphQLRequest` payload.
- `SendAuthorizationInConnectionInit` (default off) copies the `Authorization` header produced by `ConfigureRequest`/`WithAccessToken` into the `connection_init` payload — how a per-user token reaches the server on Blazor WebAssembly, where the browser does not allow headers on WebSocket requests. It merges into (and wins on key conflicts with) a `ConnectionInitPayload` that serializes to a JSON object.
- WebSocket upgrade requests carry the headers produced by `ConfigureRequest`/`WithAccessToken` plus `HttpClient.DefaultRequestHeaders` (a hook header wins on a name clash), except on browsers, where custom headers are impossible.

Register them through DI like any other options:

```cs
services.AddSampleClient(options =>
{
    options.SubscriptionProtocol = SubscriptionProtocol.ServerSentEvents;
    options.SubscriptionMaxBufferedEvents = 256;
    options.SubscriptionOverflowPolicy = SubscriptionOverflowPolicy.DropOldest;
    options.SubscriptionIdleTimeout = TimeSpan.FromSeconds(30);
});
```

Guarantees:

- Events are delivered in order, at most one message at a time (no concurrent observer callbacks).
- Each subscriber gets its own buffer, so one slow or overflowing observer does not affect another.
- A bounded buffer decouples transport from the observer; with the default `Fault` policy an overflow faults the subscription with `InvalidOperationException` instead of dropping events silently.
- The first terminal transport error (initial connection failure, server `connection_error`, fatal close code, or an exhausted reconnect budget) terminates the stream via `OnError`.
- A malformed GraphQL response for a single event also terminates the stream.

Not supported: replay of missed events, incremental delivery (`@defer`/`@stream` partial payloads).

```cs
var subscription = await sampleClient
    .Subscription
    .CustomerAdded()
    .Select()
    .StartAsync();

subscription.Subscribe(
    customer => Console.WriteLine(customer.CustomerName),
    error => Console.WriteLine(error.Message),
    () => Console.WriteLine("completed"));
```

# Acknowledgments

Linq2GraphQL is inspired by [GraphQLinq](https://github.com/Giorgi/GraphQLinq) , thank
you [Giorgi](https://github.com/Giorgi)

## Contributing

Are you a developer looking to contribute to this project? Please see our [Developer Guide](DEVELOPER.md) for
comprehensive information about:

- T4 template development workflow
- Code generation system architecture
- Troubleshooting common issues
- Best practices for template development
- Manual template regeneration process

## Development Workflow

**⚠️ Important for Developers:** When modifying T4 templates (`.tt` files), you must manually regenerate the
corresponding `.cs` files using Visual Studio's "Run Custom Tool" feature. See [DEVELOPER.md](DEVELOPER.md) for detailed
instructions.

[![Stargazers repo roster for @linq2graphql/linq2graphql.client](https://reporoster.com/stars/dark/linq2graphql/linq2graphql.client)](https://github.com/linq2graphql/linq2graphql.client/stargazers)



