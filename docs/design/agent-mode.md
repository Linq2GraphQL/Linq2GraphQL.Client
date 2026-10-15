# Linq2GraphQL Agent Mode Design

**Status:** proposal. Nothing is implemented yet beyond the groundwork listed under [Plan](#plan-and-risks).

## Summary

We propose an **agent mode** for Linq2GraphQL: an opt-in package that lets an in-process AI agent query GraphQL APIs by writing plain GraphQL. The developer decides, in typed C#, which operations the agent may call, which fields it may see and query, and what extra context it gets. The agent receives a simplified, scoped schema and a small set of tools. The server, with a down-scoped on-behalf-of token, is the security boundary; the client enforces the scope for the agent, so the agent can't shortcut it. This works against any GraphQL server that authorizes by token, and the agent never reaches more than its token allows.

**Why.** We believe there is room for a middle path between handing an agent the full, generic schema and hand-writing a dedicated tool per question. In our view the fully generic approach is naive: it spends tokens on everything, exposes operations nobody chose, and leaves the agent with descriptions written for developers. Dedicated tools are precise but slow to build and rigid. Just as important, this gives teams an easy on-ramp into building agentic apps on top of the clients they already have. If a team later moves parts of its agent to dedicated tools, that counts as success, not failure.

## Problem, goals and non-goals

Agents are good at writing GraphQL, but only if they know the schema. Handing them the full schema wastes tokens, exposes operations we don't want them to use, and hides intent behind developer-oriented descriptions. Hand-writing one tool per question doesn't scale and makes the agent rigid.

**Goals**

- Make it easy to expose *curated* GraphQL access to an agent, with little orchestration code.
- Let the agent ask flexible questions within a scope the developer controls.
- Keep the configuration typed against the generated client, so schema renames break the build.
- Support on-behalf-of (OBO) tokens so the agent never sees more than the current user may.
- Give teams an easy on-ramp: a team with an existing Linq2GraphQL client gets a working agent in an afternoon, with a clear path to dedicated tools where they pay off.

**Non-goals**

- Replacing server authorization. The client scopes what the agent *may see and query*; the server decides what it may *read*.
- Hosting, orchestration, approval workflows or multi-step paging frameworks.
- Exposing every operation automatically.
- **Mutations in the first version.** It starts query-only. Mutations come later, with their own design for confirmation and input safety (see [Mutations (later)](#mutations-later)).
- **MCP server support.** No concrete use case yet; the primary target is agents running in-process in .NET applications. The framework-independent core keeps it possible if one appears, since the C# MCP SDK can wrap `AIFunction`s.

## Use cases

The primary target is **agents built in .NET applications on top of a Linq2GraphQL client**, with the tools running in-process. Whether such an agent answers a user, runs in the background or is a sub-agent doesn't change the design; only the token source differs. The distinction that matters is in-process versus remote: remote agents change the auth and hosting story, so they are deferred. The other use cases are supported because the layering makes them nearly free, not because the design is built around them.

| Use case | Support | Notes |
| --- | --- | --- |
| **Agents in .NET applications**: interactive, background (nightly reports, ticket triage) or sub-agents of a larger agent | Primary | Interactive agents act for the signed-in user via OBO. Background agents have no user, so the token delegate uses the app's own identity with a narrow scope; approvals aren't possible there, which reinforces query-only. A sub-agent is the same setup with a narrower configuration. |
| **Single-shot natural-language query** (an "ask your data" box, one LLM call) | Later, via `ScopedSchema` | No agent loop: render the scoped schema and examples into a prompt, validate the returned query, execute it. |
| **User-written GraphQL with policy** (admin console, reporting tool) | Later, via `ScopedSchema` | No AI at all: the validator enforces allowed operations and omitted fields on queries people write. |
| **Coding agents writing Linq2GraphQL code** | Later idea | They would benefit from the schema tools while writing code; a CLI command fits better than MCP. |
| **Remote agents** (MCP, HTTP tools for hosted agents) | Deferred | A new adapter on the framework-independent core; auth and hosting need their own design. |
| **Human-readable schema docs** | Bonus | The Outline and Full levels double as compact documentation. |

## First version

The first version is deliberately small. The rest of this document describes the direction; items marked later are built when the pilots show they are needed.

| First version | Later |
| --- | --- |
| SDL output and `IGraphSchemaSource` | Dedicated tools with `o.Tool` (until then, `AIFunctionFactory.Create`) |
| Scoped schema: allow (including `AllowAllQueries` with `Except`), omit, describe, instructions | Bound arguments |
| Renderer, collapsing connections and filters | Public `ScopedSchema` API (non-breaking to add) |
| Validator: roots, fields, arguments and filters | Mutations, starting with dedicated mutation tools |
| The four tools | MCP and other adapters |
| Typed examples, `OnQuery`, token delegate | |
| Preview package for pilots, runnable sample, regression harness | |

The groundwork ships independently and is useful on its own: the per-request token hook, the schema cache-key fix, SDL input in the generator, and `[Description]` attributes.

## Approaches considered

| Approach | Verdict |
| --- | --- |
| **Generic**: the full schema, or a generated tool per operation | Rejected as naive (see Why): uncurated, token-heavy, developer-oriented descriptions. |
| **Dedicated typed tools only** (a fixed `Select` per question) | Precise but slow to build and rigid. Kept as the specialisation step (see [Path to dedicated tools](#path-to-dedicated-tools)) and for sensitive entities. |
| **Raw mode with a scoped schema** (agent writes GraphQL; client scopes and validates) | **Chosen.** Flexible for the agent, a small API for the developer, a simple security story once the server enforces authorization, and better queries than generic approaches. |
| **Client-side field hiding as security** | Rejected as the security mechanism: a blocklist that duplicates server authorization. `Omit` is still enforced by the validator, as defense in depth. |

## Architecture

The design has three layers. Each is useful and testable on its own.

| Layer | When | Contents |
| --- | --- | --- |
| **Schema SDL** (generated) | At client generation | The complete schema: types, fields, arguments, enums, descriptions, nullability, deprecation |
| **Agent configuration** (developer code) | At runtime, when the agent is built | Allowed roots, omitted fields, descriptions, instructions |
| **Renderer and tools** | Per agent call | The scoped schema: the SDL parsed and filtered through the configuration, rendered at a chosen detail level and served through the tools |

The schema comes from the generator rather than runtime introspection: it works when introspection is disabled in production, needs no extra request or credentials, can't disagree with the typed client, and schema changes show up in PR diffs. The agent configuration is runtime code because one client can serve several agents with different scopes.

### Schema source: SDL

The generator prints the introspected schema as standard SDL into one generated file. It is emitted only with the `--agent` flag, so clients that don't use agent mode are unchanged:

```csharp
// Generated/Client/SampleClientSchema.cs
public static partial class SampleClientSchema
{
    public const string Sdl = """
        type Query {
          "Search customers"
          customers(first: Int, after: String, where: CustomerFilterInput): CustomersConnection
          "Get one customer"
          customer(id: ID!): Customer
        }

        "A customer account"
        type Customer {
          id: ID!
          name: String!
          status: CustomerStatus!
          orders(first: Int): [Order!]!
          legacyCode: String @deprecated(reason: "Use code")
        }
        """;
}
```

SDL is complete (descriptions, nullability, arguments, enums, deprecation), adds no types to the generated client, diffs cleanly, and is parsed once with the validator's parser. A generated C# model was rejected: it needs model types in the core package and diffs poorly. Generating a client from SDL is already planned, so both inputs feed one schema model:

```
endpoint ──introspection──► Schema model ──► templates (C# client)
                                ▲     │
schema.graphql ──parse SDL──────┘     └──► SDL printer ──► SampleClientSchema.Sdl
```

**Compile-time check.** With `--agent`, the generated client implements a small interface from the **core** package, so the generated code still has no dependency on the AI package. `AsAgentTools` is constrained to it:

```csharp
// Linq2GraphQL.Client (core)
public interface IGraphSchemaSource
{
    static abstract string Sdl { get; }
}

// Generated with --agent
public partial class SampleClient : IGraphSchemaSource
{
    public static string Sdl => SampleClientSchema.Sdl;
}

// Linq2GraphQL.Client.AI
public static IList<AIFunction> AsAgentTools<TClient>(this TClient client, Action<AgentOptions<TClient>> configure)
    where TClient : IGraphSchemaSource
```

Calling `AsAgentTools` on a client generated without `--agent` doesn't compile. The compiler's message is generic (no conversion to `IGraphSchemaSource`), so the docs must explain it. No reflection or naming convention is needed to find the SDL.

**Any SDL works.** The generated constant is the default because it comes from the same introspection as the client, so the two can't drift. A second overload takes the SDL explicitly, for example a schema file exported from HotChocolate. It has no interface constraint, so it also works for clients generated without `--agent`:

```csharp
var tools = client.AsAgentTools(File.ReadAllText("schema.graphql"), o => { ... });
```

Typed configuration (`c => c.Query.Customers`, `c => c.LegacyCode`) is resolved to GraphQL names through the generated `[GraphQLMember]` attributes. A name that isn't in the SDL throws at setup, so a mismatched schema file is caught immediately.

### From SDL to scoped schema

The SDL is never edited as text. When `AsAgentTools` runs:

1. **Parse** the SDL into an in-memory schema, once per client (cached).
2. **Keep only allowed root fields.** A root type with none, such as `Mutation` when no mutation is allowed, disappears.
3. **Remove omitted fields.**
4. **Keep only reachable types**, following field return types and argument input types from the remaining roots. A type reachable only through an omitted field is dropped.
5. **Apply description overrides.**
6. **Simplify:** collapse connections and filter inputs, drop deprecated fields.

The result, the **scoped schema**, is built once per `AsAgentTools` call. The renderer, the query validator and the schema tools all run off it.

### Package layering

The AI package is layered so that the lasting work doesn't depend on any AI framework:

```
Core (no AI dependencies)                     Adapter (thin)
├─ scoped schema (SDL → allow/omit/describe)  └─ AsAgentTools: wraps the four
├─ renderer (Index / Outline / Full)              operations as Microsoft.Extensions.AI
├─ query validator                                AIFunctions
└─ the four operations as plain C# methods:
   Execute, DescribeOperation, DescribeType, Search
```

- **The core holds the lasting work**: scoped schema, renderer, validator, and the four operations as plain methods. It never references Microsoft.Extensions.AI.
- **`AsAgentTools` is a thin adapter** of a few dozen lines. New tool conventions, MCP or another framework mean a new adapter, not a redesign.
- **One package.** Later the core is exposed as a public `ScopedSchema` for use without an agent framework (below). If a separate package is ever needed, the types move and are forwarded with `[TypeForwardedTo]`.

```csharp
var schema = ScopedSchema.Create(client, o => { ... });   // same options as AsAgentTools

string prompt = $"""
    Write a GraphQL query for this question: {question}
    Schema:
    {schema.Render(Detail.Outline)}
    """;

string query = await llm.CompleteAsync(prompt);           // any LLM client

var result = schema.Validate(query);                      // allowed roots, omitted fields, filters
if (result.IsValid)
    data = await schema.ExecuteAsync(query);
```

## Public API

A single entry point on the generated client, returning ordinary `AIFunction`s that plug into any Microsoft.Extensions.AI-based agent:

```csharp
IList<AIFunction> tools = client.AsAgentTools(o =>
{
    // Operations: allow and describe in one place (queries only in the first version)
    o.AllowQuery(c => c.Query.Customers)
     .Describe("Search customers. Filter by name rather than listing all.")
     .Argument("first", "Max 50.");

    o.AllowQuery(c => c.Query.Customer)
     .Describe("Get one customer by id.");

    // Types, fields and enum values
    o.Describe<Customer>("A B2B customer account.");
    o.Describe<Customer>(c => c.Status, "ACTIVE = currently invoiced.");
    o.Describe(CustomerStatus.OnHold, "Paused by finance. Do not contact.");

    // Examples for common tasks, written with the typed client
    o.Example("Recent orders", "A customer's most recent orders",
        c => c.Query.Customer(id: "4711")
              .Select(cu => new
              {
                  cu.Name,
                  Orders = cu.Orders(first: 5).Select(or => new { or.Id, or.Total, or.CreatedAt })
              }));

    // Leave out noise, add global context
    o.Omit<Customer>(c => c.LegacyCode);
    o.Instructions("All dates are UTC.");

    // Token for the current user, down-scoped for agents
    o.AccessToken = ct => tokens.GetAgentTokenForUserAsync(currentUser, ct);
});

var options = new ChatOptions { Tools = [..tools] };
```

Rules:

- **Typed wherever possible.** Operations, types, fields and enum values are referenced through the client, so a rename breaks the build. `AsAgentTools<TClient>` is a generic extension, not generated code.
- **The scoped schema is enforced before sending:** allowed roots, and fields that exist and aren't omitted. Descriptions and instructions only shape what the agent is told.
- **`AllowAllQueries()` is explicit, never the default.** `o.AllowAllQueries().Except(c => c.Query.AuditLog, ...)` pre-fills the allow-list from the SDL for the fastest start. Everything else (rendering, schema tools, validation, `Omit`, descriptions, examples) still applies, so it isn't the naive generic approach. New roots become available after regeneration, visible in the SDL diff. On large schemas the Index groups roots by return type or caps the list and points to `search_schema`.
- **`Omit` removes fields from the scope**: noise, and data the agent shouldn't touch or its token can't read. Enforced for the agent; the server remains the boundary for everything else.
- **Descriptions overlay the schema.** `Describe` replaces the schema text, with an append option.
- **Fail fast.** Unknown names, or describing something omitted, throw when `AsAgentTools` runs.
- **Created per user or conversation**, so the token delegate captures who the agent acts for.
- **Examples for common tasks.** `o.Example(name, description, c => <typed query>)` maps an intent to a query, possibly across operations, written with the client as above. The compiler checks it, it is validated against the scoped schema at setup, and it is rendered to GraphQL (arguments as variables) for the Index and `describe_operation`, without a server call. A task that always runs the same way should be a dedicated tool.
- **Query logging.** `o.OnQuery(...)` sees every query, its status and, where available, the user's question.
- **Later:** bound arguments (`.Bind("tenantId", () => currentTenant)`, filled from context and hidden from the agent) and dedicated tools (`o.Tool`, see [Path to dedicated tools](#path-to-dedicated-tools)).

## Schema rendering and detail levels

The renderer turns the scoped schema into text the agent reads. It stays close to SDL, because models know it well and the agent writes GraphQL anyway. Simplicity comes from removing noise:

- Directives, deprecated fields, introspection types and omitted fields are removed.
- **Connections are collapsed**: `CustomerConnection`, `CustomerEdge` and `PageInfo` become `customers(...): [Customer] @paged`, with one shared rule explaining how to query paged fields.
- **HotChocolate filter and sort inputs are collapsed**: the many `...OperationFilterInput` types become "filterable: name, status, createdAt" plus one shared explanation of the operator syntax. On HotChocolate schemas this is likely most of the size.

Three detail levels:

| Level | Contains | Typical use |
| --- | --- | --- |
| **Index** | Allowed operations with one-line descriptions, type names, and example names with descriptions | Always in the `graphql_query` tool description |
| **Outline** | Adds fields and their types | Small and medium schemas inline |
| **Full** | Adds descriptions, arguments, enum value meanings and examples | Returned by `describe_operation` and `describe_type` |

The renderer is a pure function of (SDL, configuration, level), so its output can be snapshot-tested. A checked-in snapshot shows in review exactly what the agent will be told, in the same spirit as `regenerate-test-clients.ps1 -Check`.

## Agent tools

| Tool | Returns |
| --- | --- |
| `graphql_query(query, variables)` | The data, or GraphQL errors as a readable result. The query is validated against the scoped schema (see below); introspection (`__schema`, `__type`) is blocked. The **Index**, including example names and descriptions, is in its description. |
| `describe_operation(names[])` | Everything needed to call the named operations: arguments with types, defaults, descriptions and filter operators (bound arguments left out), the return type at Outline level with the paging pattern written out, a generated **starter query**, and the examples that use the operation. |
| `describe_type(names[])` | Full detail for the named types. Also returns the types of their fields at Outline level. |
| `search_schema(text)` | Matching operations, types, fields and examples, one line each. For when the agent doesn't know a name. |

Operations and types are separate tools because an operation needs richer output than a type, and names can collide (a root field `order` next to a type `Order`, or later the same name on `Query` and `Mutation`). Example output of `describe_operation(["customers"])`:

```
customers  – Search customers. Filter by name rather than listing all.
Arguments:
  first: Int = 10     Max 50.
  after: String       Cursor from pageInfo.endCursor
  where: CustomerFilterInput   filterable: name (eq, contains, startsWith), status (eq, in)
Returns: [Customer] @paged
  Customer { id: ID!  name: String!  status: CustomerStatus!  orders(first: Int): [Order!]! }
Starter query:
  query { customers(first: 10, where: { name: { contains: "…" } }) {
    nodes { id name status }  pageInfo { hasNextPage endCursor } } }
Examples: those that use this operation, e.g. "Unpaid invoices"
```

The starter query is generated from the scoped schema at no cost, and next to developer examples it is probably the strongest single lever for query quality. `describe_operation` also covers mutations later, so it needs no redesign when `graphql_mutate` arrives.

**All four tools are in the first version.** They work only on the scoped schema, and the three schema tools answer locally with no server calls. Large schemas are common, so the agent explores on demand: the Index lists only operations, types and examples, and details come from the describe and search tools. Small schemas can raise the inline level to Outline or Full.

Example flow for "which customers have unpaid invoices?": the agent sees `customers` in the index and calls `describe_operation(["customers"])` for its arguments and a starter query. It calls `search_schema("invoice")`, finds `Customer.invoices` and `Invoice.status`, and calls `describe_type(["Invoice", "InvoiceStatus"])`. Then it adapts the starter query and calls `graphql_query`.

**Validation.** Every query is checked against the scoped schema before it is sent: root fields must be allowed, and every selected field must exist on its type and not be omitted. The validator parses the query and walks it type by type, following fragments and inline fragments through their type conditions (interfaces and unions). Aliases don't matter, because the check uses real field names. Errors name the field and list what is available, for example "`legacyCode` is not available on `Customer`. Available: id, name, status, orders", so the agent can correct itself without a server round-trip. Arguments, input objects (including filters) and enum values are validated in the same walk in the first version; a depth limit can come later.

## Path to dedicated tools

Agent mode is meant as a starting point. The expected path for a team:

1. **Start broad.** `AsAgentTools` with a few allowed roots, or `AllowAllQueries()` (with `Except`) for a start with zero choices. The minimal setup must just work, so defaults for detail level, paging and instructions matter more than options.
2. **Observe.** The `OnQuery` hook logs which questions the agent gets and which queries it runs. Later, a usage report ("12 of 140 allowed roots used in the last 7 days") can generate the matching `AllowQuery` lines, turning `AllowAllQueries` into a starting point for curation rather than a permanent state.
3. **Specialise.** Frequent or critical questions become dedicated tools: in the first version with `AIFunctionFactory.Create` and a fixed `Select`, later with `o.Tool(...)` in the same builder (below).

Both kinds of tools are `AIFunction`s, so they mix freely in the same `ChatOptions` and a team can move one question at a time. Setup errors must say what to do, because new teams hit them first.

### Dedicated tools in the builder (later)

A dedicated tool fixes the query shape; the agent only fills its parameters:

```csharp
var tools = client.AsAgentTools(o =>
{
    o.AllowQuery(c => c.Query.Customers);

    o.Tool("get_recent_orders", "A customer's most recent orders",
        (string customerId, int count) => c => c.Query.Customer(id: customerId)
            .Select(cu => new
            {
                cu.Name,
                Orders = cu.Orders(first: count).Select(or => new { or.Id, or.Total, or.CreatedAt })
            }))
     .Parameter("count", "Max 20.");
});
// → graphql_query, describe_operation, describe_type, search_schema, get_recent_orders
```

- **Same infrastructure as the generic tools:** token delegate, `OnQuery` logging, readable errors, compact JSON, and capped paging (`hasMore` reported).
- **Not limited by the scope.** Allow and `Omit` govern what the agent composes; a dedicated tool is composed by the developer, so it may expose fields the generic tools don't. It is the route for sensitive entities.
- **The Index points to dedicated tools** ("prefer `get_recent_orders` for a customer's recent orders"), so promoting a question also changes how the agent routes it.
- **Promotion is nearly mechanical:** an example's fixed values become the tool's parameters.

## Query quality

The design should already beat generic approaches; these measures address the weak spots most likely to cause bad queries. The pilots show which matter most; error quality and filter validation come first, and the harness guards against regressions.

| Weak spot | What we do |
| --- | --- |
| **Regressions** | An **evaluation harness** as regression protection, not as the go/no-go decision (that comes from the pilots). Its questions come from real logged pilot questions, anonymised where needed, plus the developer examples. It runs against the test server whenever the renderer or validator changes, recording answer rate, tool calls and tokens, so a "better" format can't silently make things worse. It lives in the repo as a separate project, outside CI since it needs an API key. |
| **Error recovery** | All errors are returned at once, not just the first. Every error states what is wrong, where (the path in the query) and what to do instead: valid alternatives and "did you mean" suggestions for near-miss names (`custmer` → `customer`). |
| **Filter syntax** | The exact syntax is shown once as a rule with a worked example, and `describe_operation` and `describe_type` list the operators per field type (strings: `eq`, `contains`, `startsWith`; numbers: `eq`, `gt`, `lt`). Arguments and input objects are validated in the first version, so a wrong filter gets a precise error. |
| **Paging** | Paged fields are rendered with the exact query pattern next to them. A targeted error catches the classic mistake: `customers { id }` returns "`customers` is paged. Use `customers { nodes { id } pageInfo { hasNextPage endCursor } }`". |
| **Exploration cost on large schemas** | `describe_operation` returns a generated starter query, so the agent often needs no further exploration. `search_schema` ranks name matches above description matches and returns field paths (`Customer.invoices`), not only type names. `describe_type` also returns the types of a type's fields at Outline level, saving a round-trip per hop. Examples for common tasks (see [Public API](#public-api)) are the biggest lever, and the docs should push them. |

## Security model

Responsibilities are split three ways:

| Part | Responsibility |
| --- | --- |
| **Server** | Protection: field and operation authorization by scope, e.g. HotChocolate `[Authorize(Policy = "pii")]` on sensitive fields |
| **Token** | Identity and narrowing: the current user's token exchanged (OBO) for a down-scoped agent scope, e.g. `crm.agent` instead of `crm.read crm.pii` |
| **Client** | Scope enforcement for the agent: the agent reaches the API only through the tools, with no raw HTTP tool; every query is validated against allowed roots and non-omitted fields before it is sent; introspection is blocked; `graphql_query` is query-only; later, bound arguments take values out of the agent's hands |

**The agent can't shortcut the client.** Whatever the agent is prompted to do, a disallowed root or an omitted field never leaves the process, and the typed configuration keeps the scope from drifting silently from the schema. The server is still needed for what the client can't cover: other code using the same token, a bug in the validator, and defense in depth.

The effective rights are an intersection: what *this user* may see **and** what *agents* may see. Data the user may legitimately see but that should not reach an LLM provider (personal identifiers, health data, internal notes) is excluded by the agent scope on the server, so aliases, fragments or guessed field names cannot get around it. A new sensitive field is protected by putting the policy on it where the schema is defined.

None of this requires owning the server. Any GraphQL API that authorizes by token scope enforces the same way. Owning it only means you can define an agent-specific scope yourself; otherwise you pick the narrowest scope the API offers.

**On-behalf-of.** The library does not perform token exchange; Microsoft.Identity.Web's `ITokenAcquisition` and others already do. It provides a provider-neutral seam: a per-request token delegate, set when the agent tools are created and capturing the user explicitly. It works with any identity provider; Entra ID with Microsoft.Identity.Web is a documented example, not a dependency. This avoids two known traps of doing it in an `HttpClient` `DelegatingHandler`: handlers from `IHttpClientFactory` run in their own DI scope, and ambient `HttpContext` is gone once an agent turn runs off the request thread.

**What remains.** Prompt injection can still make the agent read anything its scope allows and repeat it in an answer. The down-scoped token limits the damage and the root allow-list narrows it further. A separate agent identity also gives server-side audit and rate limiting for agent traffic.

**When the available scopes are too broad** (for example an API that only offers all-or-nothing access), the agent can reach everything the token allows. Then expose sensitive entities through dedicated tools (`o.Tool(...)` with an explicit `Select`), which are allow-lists by construction, and keep the generic tools for roots where everything may be shown.

## Mutations (later)

Mutations are not in the first version, but four decisions are made now so the first version doesn't block a good design later.

**Decided now**

1. **A separate tool for writes.** The query tool is `graphql_query`, query-only by contract; writes get `graphql_mutate`. Approvals and hints apply per tool, so one combined tool would prevent approval for writes only.
2. **Room in the configuration API.** `AllowMutation(...)` will need settings (approval, bound arguments, input validation), so the builder types allow for them.
3. **`Omit` and `Describe` work on input types and fields**, needed for filters now and mutation input later.
4. **Bound arguments come before mutations**, since mutations depend on them.

**For when mutations are built**

- **Approval:** the host confirms; the mutation tool is marked as requiring approval, possibly with a prepare-then-confirm mode.
- **One write per call:** exactly one root mutation field, so each approval covers one change.
- **Retries:** never automatic; results say succeeded, failed or unknown; idempotency keys where the server has them.
- **Input safety:** bound arguments, omitted input fields, optional `.Validate(input => …)`.
- **Prompt injection** is the biggest write risk (text the agent read can instruct a write): approval, bound arguments, narrow scopes, and plain docs.
- **Scopes:** separate write scopes on the server (e.g. `crm.agent.write`).
- **Audit:** server-side via the agent identity, plus an optional `OnMutation` hook.
- **Evaluation:** mutation scenarios in the harness, against a resettable test server.

**Dedicated mutation tools may come first.** A dedicated tool is the safest first step for writes: the developer fixes the mutation's shape and the agent only fills parameters, which removes most of the input-safety and one-write-per-call concerns. `o.Tool` for mutations could ship before the generic `graphql_mutate` tool. The first version stays query-only; this is the likely order once writes are added.

Subscriptions stay out entirely; they don't fit the request/response shape of tool calls.

## Required changes in core and generator

**Core (`Linq2GraphQL.Client`), useful on their own**

- **Per-request token or header hook.** Today auth works only through the `HttpClient` pipeline; `GraphClientOptions` has no request hook. Typed users get a cleaner OBO story too.
- **Schema cache key.** The safe-mode schema cache is keyed only by base address (`GraphClient.cs`), so two clients against the same endpoint with different credentials share one cached schema, and `includeDeprecated` is ignored. Agent mode no longer depends on introspection, but this should be fixed regardless.
- **`IGraphSchemaSource` interface** with a static abstract `Sdl` property. Implemented by clients generated with `--agent`; it lets `AsAgentTools` require agent mode at compile time.

**Generator**

- **SDL input** (already planned): generate a client from a `.graphql` file, mapped into the existing `Schema` model so the templates don't change. It brings the GraphQL-Parser library into the generator; as a `dotnet tool`, that never reaches users' applications.
- **A single `--agent` flag** (also in `--config`): emits the SDL constant through the parser's SDL printer and `IGraphSchemaSource` on the client. Both are dependency-free, so the client compiles with or without the AI package.
- **Two more introspection fields**, `possibleTypes` and `defaultValue`, so endpoint and SDL inputs produce the same result.
- **`[Description]` attributes, always emitted** on types, properties, enum values and method parameters, with an opt-out in `--config`. One-time diff on upgrade.

**New package `Linq2GraphQL.Client.AI`**

- Depends on `Microsoft.Extensions.AI.Abstractions` and GraphQL-Parser for query validation. Neither is added to the core package.
- Contains the configuration API, the renderer, the query validator and the four tools, in two layers: a core with no AI framework dependency (public later as `ScopedSchema`), and a thin Microsoft.Extensions.AI adapter (see [Package layering](#package-layering)).

## Plan and risks

**Plan**

1. **Core prerequisites:** the per-request token hook and the schema cache-key fix.
2. **SDL input in the generator:** the already planned feature. It introduces GraphQL-Parser.
3. **SDL output:** the `--agent` flag and the two extra introspection fields, built on step 2. Always-on `[Description]` attributes can ship independently of agent mode.
4. **Preview and pilots:** the first version as a prerelease package (`-preview`), complete enough to run end to end, with a runnable sample against the test server. Usefulness depends on the use case, so it is evaluated in one or two pilot apps with a real schema and real users, ideally different ones (interactive and background, small and large schema). The evaluation is part of the product: `OnQuery` logs (questions, queries, validation errors, tool calls per answer), simple user feedback per answer in the pilot app, and which questions the team moves to dedicated tools. The pilots can start before step 3 by using an exported `schema.graphql`.
5. **Decide from the pilots:** did the teams get a working agent quickly, were users satisfied with the answers, where did it fail (filters, paging, large schemas), and what did they specialise? If it holds up, release `Linq2GraphQL.Client.AI`. If not, the core and generator improvements still stand.

**Risks**

- **Query quality has known weak spots.** The scoped schema, agent-oriented descriptions and targeted validation errors should give agents a better experience than generic approaches. The pilots verify the parts most likely to go wrong: filter syntax after HotChocolate's filter inputs are collapsed, paging conventions, the number of schema-tool calls on large schemas before a query is written, and whether the agent fixes a query from a validation error in one attempt. Mitigations are described under [Query quality](#query-quality).
- **Tool conventions are moving.** How models handle tools and large contexts is changing quickly, so the tool shape (detail levels, four tools) may need to adapt. The scoped schema, renderer and validator are kept independent of any AI framework; only a thin adapter depends on Microsoft.Extensions.AI, whose basic `AIFunction` type has been stable since its GA. HotChocolate's own AI work is overlap rather than churn: it is server-side, while this is client-side and works with any GraphQL server.
