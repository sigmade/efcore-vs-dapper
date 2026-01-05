using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using Dapper;
using DapperVsEntityFramework.Common;
using DapperVsEntityFramework.EfCoreModels;

namespace DapperVsEntityFramework.UpdateOne;

[MemoryDiagnoser(displayGenColumns: true)]
public class UpdateOneProductById : WriteBenchmarkBase
{
    private const int TargetId = 1;
    private const string NewDescription = "Hello, world!";

    [Benchmark(Baseline = true)]
    public async Task<int> EfCore()
    {
        await using var dbContext = CreateDbContext();
        var product = await dbContext.Products.FindAsync(TargetId);
        Debug.Assert(product is not null);
        product.Description = NewDescription;
        return await dbContext.SaveChangesAsync();
    }

    [Benchmark]
    public async Task<int> EfCore_Attach_Hack()
    {
        await using var dbContext = CreateDbContext();
        var product = new Product { Id = TargetId };
        dbContext.Attach(product);
        product.Description = NewDescription;
        return await dbContext.SaveChangesAsync();
    }

    [Benchmark]
    public async Task<int> Dapper()
    {
        const string Sql = "UPDATE products SET description = @Description WHERE id = @Id";
        return await DapperConnection.ExecuteAsync(Sql, new { Id = TargetId, Description = NewDescription });
    }

    protected override void SetUpHook()
    {
        // Ensure we have at least one product with a deterministic starting state
        SeedDatabase(new SeedOptions(
            ProductsCount: 10,
            OrdersCount: 0));
    }
}