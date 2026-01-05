using BenchmarkDotNet.Attributes;
using Dapper;
using DapperVsEntityFramework.Common;
using Microsoft.EntityFrameworkCore;

namespace DapperVsEntityFramework.SelectOne;

[MemoryDiagnoser(displayGenColumns: true)]
public class SelectOneProduct : ReadBenchmarkBase
{
    private static readonly Func<AppDbContext, Task<EfCoreModels.Product?>> CompiledQuery =
        EF.CompileAsyncQuery<AppDbContext, EfCoreModels.Product?>(c => c.Products
            .AsNoTracking()
            .FirstOrDefault());

    [Benchmark(Baseline = true)]
    public async Task<EfCoreModels.Product?> EfCore()
    {
        var product = await DbContext.Products.AsNoTracking().FirstOrDefaultAsync();

        return product;
    }

    [Benchmark]
    public Task<EfCoreModels.Product?> EfCore_Compiled()
    {
        return CompiledQuery(DbContext);
    }

    [Benchmark]
    public async Task<DapperModels.Product?> Dapper()
    {
        const string Sql =
            """
            SELECT id, name, description, price FROM products LIMIT 1
            """;

        var product = await DapperConnection.QueryFirstOrDefaultAsync<DapperModels.Product>(Sql);

        return product;
    }

    protected override Task SetUpHookAsync()
    {
        return SeedDatabaseAsync(new SeedOptions(
            ProductsCount: 1,
            OrdersCount: 0));
    }
}