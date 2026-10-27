# Include Paths: Leaf-Only Fields, No ThenInclude, Exclude Deferred

**Status:** decided (2026-10-27). The current behaviour is intentional. An EF Core-style `ThenInclude` was prototyped and rejected. `Exclude` is deferred until there is a real case (see the last section). User docs: [Selecting fields → Include](../Linq2GraphQL.Docs/Pages/SelectingFields.razor) (`#include-paths`).

## Summary

An `Include` or `Select` path such as `e => e.Nodes.Select(n => n.Customer.Orders)` names the object you want. The object at the **end** of the path is fetched with all its scalar fields. The objects the path passes through (`Nodes`, `Customer`) are only the way there and get no scalar fields of their own unless they are included separately. We keep that rule, and we don't add `ThenInclude`, because it would give the same paths a second, different meaning.

## The rule

In `QueryExpressionVisitor`, `Select(expression)` resolves the path to a node and sets `IncludePrimitive = true` on that node only. Intermediate nodes are created by `ResolvePath` without the flag. When the query runs, `QueryNode.AddPrimitiveChildren` adds scalar fields to the flagged nodes. It also adds them to object nodes that have no children, because GraphQL requires at least one field per object. A node in the middle of a path always has a child, so it only contains the fields that lead somewhere.

```csharp
.Include(e => e.Nodes.Select(n => n.Customer.Orders))   // orders: all scalars
                                                         // nodes, customer: only the path
.Include(e => e.Nodes.Select(n => n.Customer))          // add this to get the customer's scalars too
```

## Why leaf-only

- **It's what GraphQL means.** A GraphQL query asks only for what it names, and an include path names its last field. Treating the steps along the way as requests would make the client ask for more than was written.
- **Intermediate levels are often just structure.** Connections, edges and `nodes` wrappers have few useful scalar fields. Fetching them by default would make every deep path bigger for nothing.
- **It's easy to add to, and hard to take away from.** One path gives one fully fetched object. If you want a level in the middle as well, you add an `Include` for it. The opposite default (fetch every level) would need an opt-out syntax to get back to a small query, and that syntax doesn't exist in LINQ.
- **It matches projections.** A projection fetches exactly what it reads. Leaf-only `Include` follows the same idea: what you name is what you get. Projections are the style we recommend, and the two shouldn't contradict each other.

## Known cost

When the result is a generated type rather than a projection, objects in the middle of a path come back with their scalar fields at their C# defaults. For references and nullable types that's `null`, which is fine. For non-nullable value types it's `0`, `false` or `Guid.Empty`, which can't be told apart from a value the server really returned. The nullable client doesn't help, because non-null schema fields are non-nullable in C# too. The tests rely on this (`QueryTests.Customers_WithIncludeOrdersAndAddress` asserts that `OrderId` is `default`).

We handle this with documentation (the user docs say it directly and recommend projecting when field presence matters), not by changing behaviour. If it turns out to cause real bugs, an opt-in check that flags reading a scalar that was never requested would be possible, because the client knows which fields it asked for. That isn't planned.

## ThenInclude: considered and rejected

**What was tried.** A prototype for `GraphQuery<T>` made the following work, with the full test suite passing:

```csharp
client.Query.Customers()
    .Include(c => c.Orders)
        .ThenInclude(o => o.Address)
    .Select()
```

- **Parser change:** `QueryExpressionVisitor.Parse` returned the node the path ends on, and `ThenInclude` parsed its lambda against that node. Lists don't add a level to the `QueryNode` tree, so this works for lists and single objects alike. This was the easy part.
- **Typing:** this worked the same way as EF Core. `Include` returned `IncludableGraphQuery<T, TProperty>`, a `GraphQuery<T>` subclass sharing the same `QueryNode`. It implemented an interface that is covariant in `TProperty`, so two extension overloads can take either `IEnumerable<TPrevious>` (the lambda gets the element) or a single `TPrevious`. A `List<Order>` matches the collection overload through covariance.

**Why it was rejected.**

- **It adds nothing new.** Nested `Select` already covers the same paths: `.Include(c => c.Orders.Select(o => o.Address))`. `ThenInclude` doesn't save typing when you branch either, because each branch still needs its own `Include`, just as in EF.
- **Its EF meaning breaks the rule above.** In EF, `Include(Orders).ThenInclude(Address)` loads the orders as well. To match, `ThenInclude` would have to fetch scalar fields at every level it passes through. The other choice is to look like EF while behaving differently. Either way, two paths that look equivalent would send different queries.
- **Too much API.** The return type must be the concrete query class so `.Select()` and execution still chain. C# generics can't express that once for all query kinds, so `GraphQuery`, `GraphCursorQuery` and `GraphSubscription` would each need their own class, interface and pair of extension methods. All of it would also be copied into standalone clients.
- **It breaks existing code.**
  - Binary: `Include` would return a different type.
  - Source: `var q = x.Include(a); q = q.Include(b);` stops compiling, because the two calls return different types.
- **It doesn't fit everywhere.**
  - The extension methods only show up with `using Linq2GraphQL.Client;`, which code built on a generated client often doesn't have.
  - Queries that return a list (`GraphQuery<List<Customer>>`) needed an extra `Include` overload on the element type. That overload is only chosen when the lambda doesn't compile against the list, which is fragile.

**Instead,** the user docs show EF Core users how to write a `ThenInclude` chain as nested `Select`. Reopen this only if users ask for it, and only with the leaf-only rule kept.

## Exclude: deferred until there is a real case

**Status:** discussed (2026-10-27), nothing to build yet.

**The question.** When a generated type is returned, every selected object gets all its scalar fields. An `Exclude` would mean "all of them except these". It would help with scalar fields that are expensive on the server, large, guarded by permissions (an error on one field makes the whole result partial), or deprecated.

**Why it fits better than `ThenInclude`.** It adds something new: today the only way to drop one field is to switch to a projection and give up the generated type. It refines what an included object gets without changing what a path means, so leaf-only stays intact. It's cheap: resolve the path with the existing parser, record the field name on the parent `QueryNode`, and skip it in `AddPrimitiveChildren`. `Include` keeps its return type, so nothing breaks.

**What counts against it.**

- **Projections already cover it.** They're the style the docs recommend.
- **It makes the known cost worse on purpose.** An excluded non-nullable `int` or `Guid` reads as `0` or `Guid.Empty`.
- **Edge cases need rules:**
  - Excluding every scalar field leaves an object with no fields, which is invalid GraphQL. The client would have to fall back to `__typename` or throw.
  - `Include` and `Exclude` on the same field need a defined winner.
  - Excluding a non-scalar field means nothing, so it should throw.

**Decision.**

1. **Build nothing until a concrete field causes trouble.** That field also shows whether the problem belongs to the field or to the query.
2. **If it belongs to the field (the likely case), add a generator setting first.** A field that is guarded or expensive is a problem in every query, so this is the right level. The `--config` file already has `scalarMappings`, and it would also list fields that are never fetched automatically, for example `"Customer.creditReport"`. Those fields would still be fetched when included explicitly or read in a projection. One setting fixes every query, and the confusing defaults are limited to fields someone chose on purpose.
3. **Add per-query `Exclude` only if** a need shows up that varies from query to query and a projection can't handle.
