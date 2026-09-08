# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

Linq2GraphQL turns LINQ expressions into GraphQL query text. It has two halves:

- **`src/Linq2GraphQL.Generator`** — a `dotnet tool` (`Linq2GraphQL`) that introspects a GraphQL endpoint and emits a strongly-typed C# client via T4 templates.
- **`src/Linq2GraphQL.Client`** (+ `Linq2GraphQL.Client.Subscriptions`) — the runtime NuGet packages the generated code depends on. They translate `Include`/`Select` lambdas into a query tree, serialize it, execute it, and deserialize the response.

Both are published to NuGet. Everything under `test/`, `docs/` and `StartGG/` exists to exercise them.

## Commands

All builds target **.NET 10**. Central package management (`Directory.Packages.props`) plus lock files (`RestorePackagesWithLockFile`) are in force — after changing a `PackageReference`, restore without `--locked-mode` once to refresh `packages.lock.json`, and commit it.

```powershell
# What CI runs (Linq2GraphQL.CI.slnf excludes docs/ and StartGG/)
dotnet restore Linq2GraphQL.CI.slnf --locked-mode
dotnet build   Linq2GraphQL.CI.slnf --no-restore
dotnet test    Linq2GraphQL.CI.slnf --no-build

# Also CI: verify the checked-in test clients still match the generator
./scripts/regenerate-test-clients.ps1 -Check

# Refresh them after a template or test-schema change
./scripts/regenerate-test-clients.ps1

# Single test / class
dotnet test test/Linq2GraphQL.Tests --filter "FullyQualifiedName~QueryTests.Hello_WithNoName_HelloWorld"
dotnet test test/Linq2GraphQL.Tests --filter "FullyQualifiedName~ExpressionParserTests"

# Run the generator against a live endpoint
dotnet run --project src/Linq2GraphQL.Generator -- <endpoint> -c=ClientName -n=Namespace -o=Generated

# Scratch/manual playground (needs TestServer running on https://localhost:7184)
dotnet run --project test/Linq2GraphQL.TestServer
dotnet run --project test/Linq2GraphQL.Console
```

`Linq2GraphQL.Release.slnf` is the pack/publish subset; versioning is Nerdbank.GitVersioning (`version.json`) — do not hand-edit version numbers.

## Runtime architecture (src/Linq2GraphQL.Client)

The query pipeline is a chain of three stages; understanding it is usually the whole job:

1. **Build** — generated `QueryMethods`/`MutationMethods` return `GraphQuery<T>` (`GraphBase<T,TGraph>`), seeded with a root `QueryNode` carrying the field name and `ArgumentValue`s.
2. **Parse** — `Include(...)` and `Select(...)` lambdas go through `Utilities.ParseExpression` → `Visitors/QueryExpressionVisitor`. This is the heart of the library. It has two modes: `ResolvePath` handles expressions that *name* a field (member chains, `[GraphQLMember]` method calls, LINQ operators from `Visitors/LinqOperator`) and returns/creates the node; anything else (object initializers, comparisons, string calls) is walked as a plain `ExpressionVisitor` so every field mentioned still lands in the query. Lambda parameters are bound to nodes in a `scopes` dictionary **keyed by ParameterExpression reference**, not name, so nested lambdas reusing a name stay distinct.
3. **Execute** — `GraphBaseExecute` lazily initializes (`SetAllUniqueVariableNames`, `AddPrimitiveChildren`), renders the query text from the `QueryNode` tree, and `QueryExecutor<T>` POSTs it and unwraps `data`/`errors`/`extensions`.

Key details that bite:

- **`QueryNode`** (`QueryNode.cs`) is the whole intermediate representation. When a field is requested more than once with different arguments, `Utilities.GetArgumentsId` derives a hash suffix used as a GraphQL **alias** — the same computation must reproduce the alias when deserializing the response, so changing argument hashing breaks reads as well as writes.
- **Safe mode** (`GraphClientOptions.UseSafeMode`) makes the client run an introspection query once (cached in `IMemoryCache`, keyed by base address) so auto-included primitive fields can be validated against the real schema. Off by default; the test fixtures turn it **on**, which is why tests require `IMemoryCache` in the container.
- **Errors** have two surfaces: `ExecuteAsync` throws `GraphQueryExecutionException`; `ExecuteWithResultAsync` returns `GraphResult<T>` with `Errors`/`Extensions`. `QueryExecutor.ProcessResponseFull` is the single place both go through — keep them in sync.
- **Attributes** (`Attributes/`) are the contract between generated code and the runtime: `GraphQLMemberAttribute` maps a CLR member to its GraphQL name and marks interface/extension members, `GraphQLArgumentAttribute` carries the GraphQL type string used to emit variable declarations.
- **Subscriptions** live in a separate package with two transports (`WSClient` for graphql-ws, `SSEClient` for server-sent events). Only SSE works under the test host.

## Code generation (src/Linq2GraphQL.Generator)

`ClientGenerator.GenerateAsync` posts an introspection query (`General.IntrospectionQuery`, or the `IncludeDeprecated` variant), deserializes into `GraphQLSchema/RootSchema`, and drives one T4 template per output kind (`Templates/{Client,Class,Interface,Methods,Enum,Scalars}`). Templates return `FileEntry` objects; `Program.cs` writes them with `ReplaceLineEndings("\n")` — generated files are always LF, keep it that way.

**T4 workflow (from DEVELOPER.md — read it before touching templates):** each template is three files — `X.tt` (source, edit this), `X.tt.cs` (hand-written partial with constructor params and helpers), and `X.g.cs` (preprocessed output, **generated at build time, gitignored**). The generator's csproj runs the pinned `dotnet-t4` local tool (`.config/dotnet-tools.json`) over every `Templates\**\*.tt` before compiling, so editing a `.tt` and building is the whole loop and stale template logic cannot be built. Compile errors inside template code are reported against the `.tt` file and line. A template in a new folder needs a `T4Template` item with a `TemplateNamespace` in the csproj.

`GeneratorSettings.Current.Nullable` is ambient static state read from inside templates; the nullable and non-nullable clients differ mainly in nullable annotations and `#pragma warning disable CS8618`.

`GeneratorSettings.Current.TypeMapping` is the GraphQL-scalar-to-CLR-type table for the run: `Helpers.DefaultTypeMapping` with any `scalarMappings` from the `--config` settings file applied on top (`GeneratorConfig`). It is read in exactly two places - `BaseType.GetCoreType` for the emitted type name and `Schema.GetCustomScalars`, which treats absence from the table as "generate a `CustomScalar` class for this scalar" - so mapping a scalar to a simple type also suppresses its generated class, and mapping it to `null` does the reverse. Override targets are validated against `Helpers.SupportedTargetTypes`, an allow-list, because a null `CoreType.CSharpType` silently changes nullability and the input-factory template rather than failing.


## Tests (test/)

`Linq2GraphQL.Tests` is xUnit + Shouldly + Moq. It spins up the real GraphQL server in-process with `WebApplicationFactory<Program>` — `Linq2GraphQL.TestServer` is HotChocolate over the POCOs in `TestServer.Shared`, and `TestServerNullable` is the same schema for the nullable client. `SampleClientFixture` / `SampleClientNullableFixture` wire the generated client to that in-memory host (safe mode on, SSE subscriptions), and test classes take them via `IClassFixture<>`. So most tests are end-to-end: an assertion failure can come from the expression parser, the query text, or the server's own resolvers.

`Linq2GraphQL.TestClient` / `TestClientNullable` hold **checked-in generated output**. They are not regenerated by the build — run `./scripts/regenerate-test-clients.ps1` (it boots both test servers over plain HTTP, regenerates both clients with the flags that produced the committed output, and writes them back) and commit the diff. The `generated-clients` CI job runs the same script with `-Check` and fails if the committed output no longer matches the generator, so a template or schema change that you forget to regenerate is caught in CI rather than passing against stale output.

`docs/`, `docs/StarWars.Client` and `StartGG/` are the Blazor documentation site and sample clients; they are outside the CI solution filter and can drift.
