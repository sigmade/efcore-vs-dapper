using BenchmarkDotNet.Attributes;
using Dapper;
using DapperVsEntityFramework.Common;
using Microsoft.EntityFrameworkCore;

namespace DapperVsEntityFramework.SelectOne;

[MemoryDiagnoser(displayGenColumns: true)]
public class SelectOneOrder : ReadBenchmarkBase
{
    private static readonly Func<AppDbContext, Task<EfCoreModels.Order?>> CompiledQuery = 
        EF.CompileAsyncQuery<AppDbContext, EfCoreModels.Order?>(c => c.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .ThenInclude(oi => oi.Product)
            .FirstOrDefault());
    
    [Benchmark(Baseline = true)]
    public async Task<EfCoreModels.Order?> EfCore()
    {
        var order = await DbContext.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync();

        return order;
    }

    [Benchmark]
    public async Task<EfCoreModels.Order?> EfCore_Compiled()
    {
        return await CompiledQuery(DbContext);
    }

    [Benchmark]
    public async Task<DapperModels.Order?> Dapper()
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
            WHERE o.id = (SELECT id FROM orders ORDER BY id LIMIT 1)
            ORDER BY p.id
            """;

        DapperModels.Order? order = null;

        await DapperConnection.QueryAsync<DapperModels.Order, DapperModels.OrderItem, DapperModels.Product, DapperModels.Order>(
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
            splitOn: "Quantity,Id");

        return order;
    }

    protected override Task SetUpHookAsync()
    {
        return SeedDatabaseAsync(new SeedOptions(
            ProductsCount: 100,
            OrdersCount: 25,
            MinItemsPerOrder: 1,
            MaxItemsPerOrder: 5));
    }
}