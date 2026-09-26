# Response shaping

How to decide what a response contains: fewer fields, changed values, computed or combined fields, conditional
fields and different views of the same data. This is what Laravel does with **API Resources** and FastAPI with
**response models / serializers**.

## The pattern

| Laravel / FastAPI | Here |
| --- | --- |
| `ProductResource`, Pydantic response model | a **response record** in `XDtos.cs` (`ProductResponse`) |
| `toArray()`, serializer logic | the **mapping** in `XMappings.cs` (`ToResponseExpression`, `ToResponse()`) |
| `ResourceCollection` with meta | `PagedResponse<T>` (items + page, total count, next/previous) |
| `$hidden` / `exclude` | not in the record = never sent |
| `when()`, `mergeWhen()` | nullable properties (nulls are omitted) or separate records |

Entities never leave the API; endpoints always return response records. A field exists in a response only if
the record declares it, so internal data (`User.PasswordHash`) can't leak by accident.

```text
entity (Product)  ──mapping──▶  response record (ProductResponse)  ──JSON──▶  client
```

## Two kinds of mapping

```csharp
// Features/Products/ProductMappings.cs
public static class ProductMappings
{
    // 1. For database queries: EF translates it into SQL, so only the needed columns are read.
    public static readonly Expression<Func<Product, ProductResponse>> ToResponseExpression = product => new ProductResponse(
        product.Id, product.Name, product.Description, product.Price, product.Stock, product.CreatedAt, product.UpdatedAt);

    // 2. For an entity already in memory (after create/update): the same mapping, compiled once.
    private static readonly Func<Product, ProductResponse> ToResponseFunc = ToResponseExpression.Compile();
    public static ProductResponse ToResponse(this Product product) => ToResponseFunc(product);
}
```

```csharp
// reads: project in the query
db.Products.AsNoTracking().Where(p => p.Id == id).Select(ProductMappings.ToResponseExpression).FirstOrDefaultAsync(ct);

// writes: map the saved entity
db.Products.Add(product);
await db.SaveChangesAsync(ct);
return product.ToResponse();
```

Keep one expression per response record and use it everywhere, so a response looks the same from every endpoint.

## Fewer fields: a record per view

Different endpoints can return different shapes of the same entity. Typical: a small record for lists, the full
one for details.

```csharp
// Features/Products/ProductDtos.cs
public sealed record ProductListItem(int Id, string Name, decimal Price, bool InStock);

public sealed record ProductResponse(
    int Id, string Name, string? Description, decimal Price, int Stock, DateTime CreatedAt, DateTime? UpdatedAt);

// Features/Products/ProductMappings.cs
public static readonly Expression<Func<Product, ProductListItem>> ToListItemExpression = p =>
    new ProductListItem(p.Id, p.Name, p.Price, p.Stock > 0);
```

```text
GET /api/products       → PagedResponse<ProductListItem>   (4 fields per item, fast)
GET /api/products/42    → ProductResponse                  (every field)
```

Name records after their use: `ProductListItem`, `ProductResponse`, `ProductSummary`, `ProductAdminResponse`.

## Changed values

Transform values in the mapping, not in the entity or the endpoint:

```csharp
public static readonly Expression<Func<User, UserResponse>> ToResponseExpression = user => new UserResponse(
    user.Id,
    user.FullName.Trim(),
    user.Email.ToLower(),                  // normalized
    user.Role,                             // enum: serialized by name ("Admin")
    user.IsActive,
    user.CreatedAt,
    user.UpdatedAt);
```

Formatting for display (currency symbols, localized dates) usually belongs in the client: send raw values
(`49.99`, ISO dates) and let the client format them for its user's language.

## Computed and combined fields

Add a property to the record and compute it in the mapping. In `ToResponseExpression` EF computes it **in SQL**:

```csharp
public sealed record ProductResponse(
    int Id,
    string Name,
    decimal Price,
    decimal PriceWithTax,      // computed
    int Stock,
    bool IsLowStock,           // computed
    string DisplayName);       // combined

public static readonly Expression<Func<Product, ProductResponse>> ToResponseExpression = p => new ProductResponse(
    p.Id,
    p.Name,
    p.Price,
    Math.Round(p.Price * 1.15m, 2),
    p.Stock,
    p.Stock < 5,
    p.Name + " (#" + p.Id + ")");
```

- Arithmetic, comparisons, string concatenation, `ToLower`/`ToUpper`, `Math.Round`, conditionals (`a ? b : c`)
  and null checks translate to SQL.
- A call EF can't translate (your own C# method) is allowed in this final `Select`: EF then runs that part in
  memory after reading the columns it needs. Inside `Where`/`OrderBy` it isn't allowed and fails with
  "could not be translated".
- Business rules that define a value ("low stock is fewer than 5") belong in one place: a constant or a domain
  method, used by both the mapping and the service.

## Extra data: related entities and counts

Project related data into the same record, still in one SQL query:

```csharp
// example: a Product with a Category navigation and an Orders collection
public sealed record ProductDetails(
    int Id,
    string Name,
    CategorySummary Category,       // nested record
    int OrderCount,                 // aggregate
    DateTime? LastOrderedAt);

public sealed record CategorySummary(int Id, string Name);

public static readonly Expression<Func<Product, ProductDetails>> ToDetailsExpression = p => new ProductDetails(
    p.Id,
    p.Name,
    new CategorySummary(p.Category.Id, p.Category.Name),
    p.Orders.Count(),
    p.Orders.Max(o => (DateTime?)o.CreatedAt));
```

EF turns this into joins and subqueries: no `Include`, no N+1 queries, no over-fetching. Keep nested records
small (`CategorySummary`, not the full `CategoryResponse`) and avoid deep nesting.

## Conditional fields

**Present only sometimes:** make the property nullable and leave it `null` when it doesn't apply. Null properties
are omitted from the JSON (see [JSON serialization](json-serialization.md)), so the field simply doesn't appear:

```csharp
public sealed record ProductResponse(int Id, string Name, decimal Price, decimal? CostPrice);   // CostPrice for staff only

// service: the caller's permission is passed in as a plain value, not a ClaimsPrincipal
public Task<Result<ProductResponse>> GetByIdAsync(int id, bool includeCost, CancellationToken ct) =>
    db.Products.AsNoTracking()
        .Where(p => p.Id == id)
        .Select(p => new ProductResponse(p.Id, p.Name, p.Price, includeCost ? p.CostPrice : null))
        ...
```

```json
{ "id": 42, "name": "Pen", "price": 1.5 }                       // regular user
{ "id": 42, "name": "Pen", "price": 1.5, "costPrice": 0.4 }     // staff
```

**Different audience, different shape:** when several fields depend on who's asking, use separate records
instead of many nullable ones:

```csharp
public sealed record UserResponse(int Id, string FullName);                                     // public profile
public sealed record UserAdminResponse(int Id, string FullName, string Email, UserRole Role,    // admin view
    bool IsActive, DateTime CreatedAt);
```

The endpoint (or service) chooses the record from the caller's role. Each shape is typed and appears correctly in
Swagger.

## Collections and paging

List endpoints return `PagedResponse<T>` with any record:

```csharp
db.Products.AsNoTracking()
    .OrderBy(p => p.Name).ThenBy(p => p.Id)
    .Select(ProductMappings.ToListItemExpression)
    .ToPagedResponseAsync(page, pageSize, cancellationToken);
```

```json
{
  "items": [ { "id": 1, "name": "Pen", "price": 1.5, "inStock": true } ],
  "page": 1, "pageSize": 20, "totalCount": 1, "totalPages": 1, "hasNextPage": false, "hasPreviousPage": false
}
```

Always order before paging, with a unique tiebreaker (`ThenBy(p => p.Id)`), so pages are stable.

## Input shaping (the request side)

Requests use their own records, never the response records or entities:

| Need | Record |
| --- | --- |
| Create with required fields | `CreateProductRequest` |
| Replace editable fields | `UpdateProductRequest` (`PUT`) |
| Change only some fields | nullable-field record or JSON Patch (`PATCH`), see [Request data](request-data.md#patch-partial-update) |

A field that isn't in the request record can't be set by the client (no mass assignment of `Role`, `Id`,
`PasswordHash`).

## What isn't built in

| Feature | Why not | If you need it |
| --- | --- | --- |
| Client-chosen fields (`?fields=name,price`) | Responses lose their fixed type: no Swagger schema, no compile-time checks | A separate lighter record/endpoint; for many different clients, GraphQL (Hot Chocolate) |
| Optional relations (`?include=category`) | Same reason; also easy to create expensive queries | A typed query flag (`bool? includeCategory` in an `[AsParameters]` record) that picks a richer record |
| AutoMapper / Mapster | Hand-written mappings are explicit, fail at compile time and translate reliably to SQL | Mapster for projects with many large, repetitive mappings |

## Checklist

- [ ] Endpoints return response records, never entities.
- [ ] One mapping expression per response record, used by every endpoint returning it.
- [ ] Reads project with `.Select(XMappings.ToXExpression)`; writes map with `.ToResponse()`.
- [ ] List and detail views have separate records when their fields differ.
- [ ] Sensitive fields are absent from records (not just set to null) unless they're conditional by design.
- [ ] Computed values reuse the business rule's single definition.
- [ ] Response records are part of the API contract: removing or renaming a field is a breaking change for clients.
