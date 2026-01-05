using BenchmarkDotNet.Attributes;
using Dapper;
using DapperVsEntityFramework.Common;
using Microsoft.EntityFrameworkCore;

namespace DapperVsEntityFramework.SelectN;

[MemoryDiagnoser(displayGenColumns: true)]
public class SelectNProductsWithFilter : ReadBenchmarkBase
{
    [Params(10, 100, 1000)]
    public int N { get; set; }

    private const string Pattern = "%a%";

    private static readonly Func<AppDbContext, string, int, IAsyncEnumerable<EfCoreModels.Product>> CompiledQuery =
        EF.CompileAsyncQuery((AppDbContext db, string pattern, int n) => db.Products
            .AsNoTracking()
            .Where(p => EF.Functions.ILike(p.Name, pattern))
            .OrderBy(p => p.Name)
            .Take(n));

    [Benchmark(Baseline = true)]
    public async Task<List<EfCoreModels.Product>> EfCore()
    {
        var products = await DbContext.Products
            .AsNoTracking()
            .Where(p => EF.Functions.ILike(p.Name, Pattern))
            .OrderBy(p => p.Name)
            .Take(N)
            .ToListAsync();

        return products;
    }

    [Benchmark]
    public async Task<List<EfCoreModels.Product>> EfCore_Compiled()
    {
        var result = new List<EfCoreModels.Product>();
        await foreach (var p in CompiledQuery(DbContext, Pattern, N))
        {
            result.Add(p);
        }
        return result;
    }

    [Benchmark]
    public async Task<List<DapperModels.Product>> Dapper()
    {
        const string Sql = "SELECT id, name, description, price FROM products WHERE name ILIKE @Pattern ORDER BY name LIMIT @N";
        var products = await DapperConnection.QueryAsync<DapperModels.Product>(Sql, new { Pattern, N });
        return products.ToList();
    }

    protected override Task SetUpHookAsync()
    {
        return SeedDatabaseAsync(new SeedOptions(
            ProductsCount: 10_000,
            OrdersCount: 0));
    }
}