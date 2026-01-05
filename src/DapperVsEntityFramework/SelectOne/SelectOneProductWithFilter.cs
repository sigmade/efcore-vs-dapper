using BenchmarkDotNet.Attributes;
using Dapper;
using DapperVsEntityFramework.Common;
using Microsoft.EntityFrameworkCore;

namespace DapperVsEntityFramework.SelectOne;

[MemoryDiagnoser(displayGenColumns: true)]
public class SelectOneProductWithFilter : ReadBenchmarkBase
{
    private static readonly Func<AppDbContext, int, Task<EfCoreModels.Product?>> CompiledQuery =
        EF.CompileAsyncQuery<AppDbContext, int, EfCoreModels.Product?>((dbContext, id) => dbContext.Products
            .AsNoTracking()
            .FirstOrDefault(p => p.Id == id));

    [Benchmark(Baseline = true)]
    public async Task<EfCoreModels.Product?> EfCore()
    {
        var id = 37; // This is needed to produce a query similar to dapper's query.
        
        var product = await DbContext.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);

        return product;
    }

    [Benchmark]
    public Task<EfCoreModels.Product?> EfCore_Compiled()
    {
        return CompiledQuery(DbContext, 37);
    }

    [Benchmark]
    public async Task<DapperModels.Product?> Dapper()
    {
        const string Sql = "SELECT id, name, description, price FROM products WHERE id = @Id LIMIT 1";

        var product = await DapperConnection.QueryFirstOrDefaultAsync<DapperModels.Product>(Sql, new { Id = 37 });

        return product;
    }

    protected override Task SetUpHookAsync()
    {
        return SeedDatabaseAsync(new SeedOptions(
            ProductsCount: 100,
            OrdersCount: 0));
    }
}