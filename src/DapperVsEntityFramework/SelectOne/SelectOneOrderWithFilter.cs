using BenchmarkDotNet.Attributes;
using Dapper;
using DapperVsEntityFramework.Common;
using Microsoft.EntityFrameworkCore;

namespace DapperVsEntityFramework.SelectOne;

[MemoryDiagnoser(displayGenColumns: true)]
public class SelectOneOrderWithFilter : ReadBenchmarkBase
{
    private static readonly Func<AppDbContext, int, Task<EfCoreModels.Order?>> CompiledQuery =
        EF.CompileAsyncQuery<AppDbContext, int, EfCoreModels.Order?>((dbContext, orderId) => dbContext.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .ThenInclude(oi => oi.Product)
            .FirstOrDefault(o => o.Id == orderId));

    // EF Core benchmarks
    [Benchmark(Baseline = true)]
    public Task<EfCoreModels.Order?> EfCore_Found() => EfByIdAsync(12);

    [Benchmark]
    public Task<EfCoreModels.Order?> EfCore_NotFound() => EfByIdAsync(-1);

    [Benchmark]
    public Task<EfCoreModels.Order?> EfCore_Found_Compiled() => EfByIdAsyncCompiled(12);
    
    [Benchmark]
    public Task<EfCoreModels.Order?> EfCore_NotFound_Compiled() => EfByIdAsyncCompiled(-1);
    
    // Dapper benchmarks
    [Benchmark]
    public Task<DapperModels.Order?> Dapper_Found() => DapperByIdAsync(12);

    [Benchmark]
    public Task<DapperModels.Order?> Dapper_NotFound() => DapperByIdAsync(-1);

    private async Task<EfCoreModels.Order?> EfByIdAsync(int id)
    {
        var order = await DbContext.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

        return order;
    }

    private async Task<EfCoreModels.Order?> EfByIdAsyncCompiled(int id)
    {
        return await CompiledQuery(DbContext, id);
    }

    private async Task<DapperModels.Order?> DapperByIdAsync(int id)
    {
        const string Sql =
            """
            SELECT 
                o.id,
                o.customer_id,
                o.creation_date,
                oi.quantity,
                p.id,
                p.name,
                p.description,
                p.price
            FROM orders o
            JOIN order_items oi ON oi.order_id = o.id
            JOIN products p ON p.id = oi.product_id
            WHERE o.id = @Id
            ORDER BY p.id
            """;

        DapperModels.Order? order = null;

        await DapperConnection
            .QueryAsync<DapperModels.Order, DapperModels.OrderItem, DapperModels.Product, DapperModels.Order>(
                Sql,
                (o, oi, p) =>
                {
                    if (order is null)
                    {
                        order = o;
                        order.Items = new List<DapperModels.OrderItem>();
                    }

                    oi.Product = p;
                    order.Items.Add(oi);
                    return order;
                },
                new { Id = id },
                splitOn: "Quantity,Id");

        return order;
    }

    protected override Task SetUpHookAsync()
    {
        return SeedDatabaseAsync(new SeedOptions(
            ProductsCount: 100,
            OrdersCount: 100,
            MinItemsPerOrder: 1,
            MaxItemsPerOrder: 7));
    }
}