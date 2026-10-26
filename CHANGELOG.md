# Changelog

All notable changes to the Linq2GraphQL client packages are documented here.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Added

- **Standalone clients** (`--standalone`, `-sa`, or `"standalone": true` in the settings file): the
  generator writes a copy of the runtime into `Runtime/` under `<namespace>.Runtime`, so the client
  references no Linq2GraphQL package. Clients generated with different generator versions can then be
  used in one app. `Runtime/RuntimeInfo.cs` records the runtime version and the packages it needs, which
  the generator also prints. `Linq2GraphQL.Client.AI` does not support standalone clients yet.

- `GraphSubscriptionExecute.StartWithErrorsAsync()` returns `IObservable<GraphResult<T>>`.
  Each event carries its data AND its GraphQL `errors`. A `next` event with field errors
  no longer terminates the subscription: partial data and later events keep flowing.
  `StartAsync()` is unchanged and still ends on any event error (`OnError`).
- `GraphClientOptions.SubscriptionMaxBufferedEvents` and `SubscriptionOverflowPolicy`
  (`Fault`, `DropOldest`, `KeepLatest`) control the buffer between the transport and
  the observer. Each subscriber gets its own buffer.
- `GraphClientOptions.SubscriptionIdleTimeout` (SSE) fails a stalled stream. The timer runs
  only while a read waits for data, so time spent handling an event between reads never
  counts as idle; the error states the configured timeout exactly (`750 ms`, `30 s`).
- `GraphClientOptions.SubscriptionRetryAttempts` (WebSocket, default 5) bounds reconnects.
- `QueryExecutor.ProcessResponse(JsonElement, ...)` and `ProcessResponse(byte[], ...)`
  overloads for streaming callers.
- **WebSocket upgrades now run the request hooks** (`ConfigureRequest`,
  `WithRequestConfiguration`, `WithAccessToken`): the headers they produce are copied onto
  the upgrade request of the initial connect and of every reconnect, so a refreshed token
  is picked up on reconnect. `HttpClient.DefaultRequestHeaders` are sent too (a hook header
  of the same name wins), as they are for queries. Handshake-owned headers (`Host`,
  `Sec-WebSocket-*`, ...) are skipped; on browsers, where custom WebSocket headers are
  impossible, the hooks still run and the `Authorization` value is available for
  `SendAuthorizationInConnectionInit` (#117). The hooks count against the connect
  timeout, which is now 30 s (Websocket.Client's 5 s default covered only the socket
  connect). An exception thrown by a hook (or by `ConnectionInitPayload`) reaches the
  subscription's error path (`OnError`, `WhenConnected`) as itself, not wrapped in
  Websocket.Client's `WebsocketException`; on a reconnect it is the inner exception of the
  terminal `IOException`.
- `GraphClientOptions.ConnectionInitPayload` (and the matching `GraphClient` property)
  set the WebSocket `connection_init` payload. Unlike the old option it accepts any JSON
  object, e.g. `new Dictionary<string, object> { ["Authorization"] = $"Bearer {token}" }`
  (the graphql-ws/Apollo connection-parameter convention) or a server-specific
  `authToken`. Called before every (re)connect, before the socket opens (within the connect
  timeout), so a slow token fetch cannot hit the server's `connection_init` wait.
- `GraphClientOptions.SendAuthorizationInConnectionInit` (default off) merges the
  `Authorization` header produced by the request hooks into the `connection_init`
  payload — how a per-user token reaches the server on Blazor WebAssembly, where the
  browser does not allow headers on WebSocket requests. Wins on key conflicts with the
  `ConnectionInitPayload`, which must serialize to a JSON object (dictionary, anonymous
  object, POCO, `JsonObject`); any other shape throws.

### Deprecated

- `GraphClientOptions.WSConnectionInitPayload` / `GraphClient.WSConnectionInitPayload`: use
  `ConnectionInitPayload`, which can return any JSON object, not just a `GraphQLRequest`.
  The old option keeps working (and falls back) until removed; `ConnectionInitPayload`
  wins when both are set.

### Changed

Behaviour changes that callers may notice. None of them changes a public signature.

- **`GraphClient.SerializerOptions` is now a shared, frozen instance.** Every client in
  the process reuses one `JsonSerializerOptions`; post-construction mutation of the
  returned options throws `InvalidOperationException`. `GraphClientOptions` does not
  expose serializer settings, so there is no supported way to customise them. If you
  relied on mutating `SerializerOptions`, open an issue describing the setting you need.
- **Extension-field reads use the shared client options.** `GetMethodValue` /
  `GetFirstMethodValue` deserialize with `ClientJson.Default` instead of .NET's default
  options. Only read-side behaviour is shared; `WhenWritingNull` does not affect reads.
- **`__AdditionalProperties` is null until first used.** `GraphQLTypeBase` no longer
  eagerly allocates the extension-data dictionary for every entity; guard with `?.` or
  assign before use when reading extension fields.
- **WebSocket handshake is no longer awaited by `StartAsync`.** Connection/ack failures
  surface as `OnError` on the subscription stream (previously thrown from `StartAsync`).
  Per-message parse errors also terminate the stream via `OnError` (previously silently
  swallowed). Applies to the WS transport; SSE already behaved this way.
- **An empty HTTP 200 body now yields `Data = default` instead of throwing.** The body
  used to be parsed with `JsonDocument.Parse`, which threw on an empty response. Callers
  that relied on the exception to detect a broken response must check
  `GraphResult.HasData` / `HasErrors` explicitly.
- **`QueryExecutor.ProcessResponseFull(string, name)` / `ProcessResponse(string, ...)`: an
  empty string no longer throws.** They used to parse with `JsonDocument.Parse`, which threw
  `JsonException` on `""`; they now return a result with `Data = default` and no errors, the
  same as an empty HTTP body. A `null` string throws `ArgumentNullException`; malformed or
  non-object JSON still throws `JsonException`.
- **One envelope parser for every response path.** Typed queries, `GraphClient.ExecuteAsync`
  (GraphQL text), the public `ProcessResponse` / `ProcessResponseFull` overloads (`string`,
  `byte[]`, `JsonElement`) and subscription payloads all parse `data` / `errors` /
  `extensions` with the internal `GraphEnvelopeReader` (`Utf8JsonReader`, no
  `JsonDocument`), so they can no longer disagree. The `JsonElement` overloads now reject
  a non-object root with `JsonException` instead of `InvalidOperationException`.
- **WebSocket: unknown message types are ignored** instead of terminating the
  subscription with `InvalidDataException`. This matches the SSE transport's tolerance for
  unknown event types and keeps forward compatibility with proxies/servers.
- **A failed initial WebSocket connect or handshake no longer retries in the background.** The
  exception surfaces from `StartAsync`; previously the transport's `Error` disconnect
  also started the bounded reconnect loop, re-running request hooks up to five times and
  reporting a second, delayed error for a failure the caller had already handled.
- **WebSocket close codes follow the graphql-ws reference client.** Fatal: `4004`, `4005`,
  `4400`, `4401`, `4406`, `4409`, `4429`, `4500` and fatal internal `1xxx` codes.
  Retryable: `4403`, `4408`, `4499` and `4504`. Previously `4408` was treated as fatal.
  After a fatal close the transport no longer reconnects in the background.
- **WebSocket reconnect is owned by the subscription** (not by Websocket.Client's own
  loop, which retries forever and goes silent after an error). A dropped connection
  retries up to `SubscriptionRetryAttempts` times with exponential backoff plus
  300-3000 ms jitter (graphql-ws default). A successful `connection_ack` resets the
  budget. When the budget is spent, the subscription fails once with an `IOException`
  whose inner exception is the last failure. Set to 0 to fail on the first drop. Every
  attempt opens a fresh socket.
- A WebSocket connection that delivers no frame for 30 s is treated as lost and
  reconnected (the same window as the previous `ReconnectTimeout`, now enforced by the
  subscription itself).

### Fixed

- The safe-mode schema cache key includes the schema type, so clients with their own runtime copy
  that share an `IMemoryCache` and an endpoint no longer read each other's cached schema
  (`InvalidCastException`).
- A server `complete` frame now completes the typed subscription observable. Previously
  the observable stayed open after the server finished the stream.
- A `Fault` overflow in one subscriber no longer starves other subscribers of the same
  observable.
- `WSConnectionInitPayload` default returns a completed task instead of a null task.
- `SSEClient` dispose race: the response handed off under the lock is now used via the
  local, so `Dispose()` during connect can no longer cause an NRE or a spurious
  failure report.
- Safe mode's one-time setup now honours the `CancellationToken` passed to `ExecuteAsync`,
  `ExecuteWithResultAsync` and the pager (#123). Before, a cancelled first query still
  waited for the setup lock and the schema query. A cancelled schema query is not cached:
  the next query runs it again. New overloads `GetRequestAsync(CancellationToken)`,
  `GetRequestAsJsonAsync(CancellationToken)` and
  `GraphClient.GetSchemaForSafeModeAsync(CancellationToken)` take the token; the existing
  parameterless methods are unchanged.

### Performance

- Responses are read as a stream and parsed directly from UTF-8 bytes
  (`GraphEnvelopeReader`): no `JsonDocument`, no UTF-16 round trip, sibling root fields
  are skipped without materializing them, and the buffered body is parsed in place
  (no second copy).
- One shared frozen `JsonSerializerOptions` per process instead of per-client metadata
  rebuilds.
- SSE events are delivered to subscribers as UTF-8 bytes; string decoding only happens
  for listeners actually attached to `Subscription`.
