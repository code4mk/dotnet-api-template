namespace DotnetApiTemplate.Api.Domain.Common;

public abstract class BaseEntity
{
    public int Id { get; set; }

    /// <summary>Set automatically by <c>AppDbContext</c> (UTC).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Set automatically by <c>AppDbContext</c> on update (UTC).</summary>
    public DateTime? UpdatedAt { get; set; }
}
