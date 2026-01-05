using System.Data;
using BenchmarkDotNet.Attributes;
using Dapper;
using DapperVsEntityFramework.Common;
using Microsoft.EntityFrameworkCore;

namespace DapperVsEntityFramework.SelectN;

[ThreadingDiagnoser]
[MemoryDiagnoser(displayGenColumns: true)]
public class SelectNProductsParallel : ReadBenchmarkBase
{
    [Params(10_000)] public int N { get; set; }

    [Params(128)] public int DegreeOfParallelism { get; set; }

    private static readonly Func<AppDbContext, int, IAsyncEnumerable<EfCoreModels.Product>> CompiledQuery =
        EF.CompileAsyncQuery((AppDbContext db, int n) => db.Products
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .Take(n));

    [Benchmark(Baseline = true)]
    public Task EfCore()
    {
        return ExecuteParallelAsync(async () =>
        {
            await using var dbContext = CreateDbContext();
            var count = 0;
            await foreach (var _ in dbContext.Products
                               .AsNoTracking()
                               .OrderBy(p => p.Name)
                               .Take(N)
                               .AsAsyncEnumerable())
            {
                count++;
            }

            return count;
        });
    }

    [Benchmark]
    public Task EfCore_Compiled()
    {
        return ExecuteParallelAsync(async () =>
        {
            await using var dbContext = CreateDbContext();
            var count = 0;
            await foreach (var _ in CompiledQuery(dbContext, N))
            {
                count++;
            }

            return count;
        });
    }

    [Benchmark]
    public Task Dapper_Buffered()
    {
        const string Sql = "SELECT id, name, description, price FROM products ORDER BY name LIMIT @N";

        return ExecuteParallelAsync(async () =>
        {
            await using var connection = CreateConnection();
            var products = await connection.QueryAsync<DapperModels.Product>(Sql, new { N });
            return products.Count();
        });
    }

    [Benchmark]
    public Task Dapper_Unbuffered()
    {
        const string Sql = "SELECT id, name, description, price FROM products ORDER BY name LIMIT @N";

        return ExecuteParallelAsync(async () =>
        {
            await using var connection = CreateConnection();
            var products = await connection.QueryAsync<DapperModels.Product>(new CommandDefinition(
                Sql,
                parameters: new { N },
                commandType: CommandType.Text,
                flags: CommandFlags.None));
            return products.Count();
        });
    }

    [Benchmark]
    public Task Dapper_QueryUnbuffered()
    {
        const string Sql = "SELECT id, name, description, price FROM products ORDER BY name LIMIT @N";

        return ExecuteParallelAsync(async () =>
        {
            await using var connection = CreateConnection();
            var count = 0;
            await foreach (var _ in connection.QueryUnbufferedAsync<DapperModels.Product>(Sql, new { N }))
            {
                count++;
            }

            return count;
        });
    }

    private Task<int[]> ExecuteParallelAsync(Func<Task<int>> worker)
    {
        return Task.WhenAll(Enumerable.Range(0, DegreeOfParallelism).Select(async _ =>
        {
            var subtotal = 0;
            for (var i = 0; i < 15; i++) subtotal += await worker();
            return subtotal;
        }));
    }

    protected override Task SetUpHookAsync()
    {
        return SeedDatabaseAsync(new SeedOptions(
            ProductsCount: 3000,
            OrdersCount: 0));
    }
}