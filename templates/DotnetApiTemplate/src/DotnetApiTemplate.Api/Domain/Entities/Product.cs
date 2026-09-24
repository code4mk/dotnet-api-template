using DotnetApiTemplate.Api.Domain.Common;

namespace DotnetApiTemplate.Api.Domain.Entities;

public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public int Stock { get; set; }
}
