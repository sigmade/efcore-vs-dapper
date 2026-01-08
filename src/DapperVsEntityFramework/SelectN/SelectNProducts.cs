using System.Data;
using BenchmarkDotNet.Attributes;
using Dapper;
using DapperVsEntityFramework.Common;
using Microsoft.EntityFrameworkCore;

namespace DapperVsEntityFramework.SelectN;

[MemoryDiagnoser(displayGenColumns: true)]
public class SelectNProducts : ReadBenchmarkBase
{
    [Params(10, 100, 1000, 10000, 50000, 100000)]
    public int N { get; set; }

    private static readonly Func<AppDbContext, int, IAsyncEnumerable<EfCoreModels.Product>> CompiledQuery =
        EF.CompileAsyncQuery((AppDbContext db, int n) => db.Products
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .Take(n));

    [Benchmark(Baseline = true)]
    public async Task<List<EfCoreModels.Product>> EfCore()
    {
        var products = await DbContext.Products
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .Take(N)
            .ToListAsync();

        return products;
    }

    [Benchmark]
    public async Task<List<EfCoreModels.Product>> EfCore_Compiled()
    {
        var result = new List<EfCoreModels.Product>();
        await foreach (var p in CompiledQuery(DbContext, N))
        {
            result.Add(p);
        }
        return result;
    }

    [Benchmark]
    public async Task<List<DapperModels.Product>> Dapper_Buffered()
    {
        const string Sql = "SELECT id, name, description, price FROM products ORDER BY name LIMIT @N";
        var products = await DapperConnection.QueryAsync<DapperModels.Product>(Sql, new { N });
        return products as List<DapperModels.Product>;
    }

    [Benchmark]
    public async Task<List<DapperModels.Product>> Dapper_Unbuffered()
    {
        const string Sql = "SELECT id, name, description, price FROM products ORDER BY name LIMIT @N";
        var products = await DapperConnection.QueryAsync<DapperModels.Product>(new CommandDefinition(
            Sql,
            parameters: new { N },
            commandType: CommandType.Text,
            flags: CommandFlags.None));
        return products.ToList();
    }

    [Benchmark]
    public async Task<List<DapperModels.Product>> Dapper_QueryUnbuffered()
    {
        const string Sql = "SELECT id, name, description, price FROM products ORDER BY name LIMIT @N";
        var products = new List<DapperModels.Product>();
        await foreach (var product in DapperConnection.QueryUnbufferedAsync<DapperModels.Product>(Sql, new { N }))
        {
            products.Add(product);
        }

        return products;
    }

    protected override Task SetUpHookAsync()
    {
        return SeedDatabaseAsync(new SeedOptions(
            ProductsCount: 100_000,
            OrdersCount: 0));
    }
}