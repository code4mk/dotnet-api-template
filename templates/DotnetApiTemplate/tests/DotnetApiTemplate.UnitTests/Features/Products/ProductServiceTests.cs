using DotnetApiTemplate.Api.Common.Results;
using DotnetApiTemplate.Api.Data;
using DotnetApiTemplate.Api.Features.Products;
using DotnetApiTemplate.UnitTests.TestUtilities;

namespace DotnetApiTemplate.UnitTests.Features.Products;

public sealed class ProductServiceTests : IDisposable
{
    private readonly AppDbContext _db = TestDbContextFactory.Create();
    private readonly ProductService _sut;

    public ProductServiceTests() => _sut = new ProductService(_db);

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task CreateAsync_WithValidRequest_ReturnsProduct()
    {
        var result = await _sut.CreateAsync(new CreateProductRequest(" Keyboard ", null, 49.99m, 10), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Keyboard", result.Value.Name);
        Assert.Equal(49.99m, result.Value.Price);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateName_ReturnsConflict()
    {
        await _sut.CreateAsync(new CreateProductRequest("Keyboard", null, 49.99m, 10), CancellationToken.None);

        var result = await _sut.CreateAsync(new CreateProductRequest("Keyboard", null, 59.99m, 5), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error!.Type);
    }

    [Fact]
    public async Task GetAllAsync_WithSearch_FiltersByName()
    {
        await _sut.CreateAsync(new CreateProductRequest("Gaming Mouse", null, 29m, 1), CancellationToken.None);
        await _sut.CreateAsync(new CreateProductRequest("Office Mouse", null, 9m, 1), CancellationToken.None);
        await _sut.CreateAsync(new CreateProductRequest("Monitor", null, 199m, 1), CancellationToken.None);

        var page = await _sut.GetAllAsync("mouse", page: null, pageSize: null, CancellationToken.None);

        Assert.Equal(2, page.TotalCount);
        Assert.All(page.Items, p => Assert.Contains("Mouse", p.Name));
    }

    [Fact]
    public async Task UpdateAsync_WhenProductDoesNotExist_ReturnsNotFound()
    {
        var result = await _sut.UpdateAsync(42, new UpdateProductRequest("Name", null, 1m, 1), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error!.Type);
    }

    [Fact]
    public async Task DeleteAsync_WhenProductExists_RemovesProduct()
    {
        var created = await _sut.CreateAsync(new CreateProductRequest("Keyboard", null, 49.99m, 10), CancellationToken.None);

        var result = await _sut.DeleteAsync(created.Value.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(_db.Products);
    }
}
