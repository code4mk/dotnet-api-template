using System.ComponentModel.DataAnnotations;

namespace DotnetApiTemplate.Api.Features.Products;

// Requests

public sealed record CreateProductRequest(
    [Required, StringLength(200)] string Name,
    [StringLength(2000)] string? Description,
    [Range(typeof(decimal), "0.01", "1000000")] decimal Price,
    [Range(0, int.MaxValue)] int Stock);

public sealed record UpdateProductRequest(
    [Required, StringLength(200)] string Name,
    [StringLength(2000)] string? Description,
    [Range(typeof(decimal), "0.01", "1000000")] decimal Price,
    [Range(0, int.MaxValue)] int Stock);

// Responses

public sealed record ProductResponse(
    int Id,
    string Name,
    string? Description,
    decimal Price,
    int Stock,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
