using System.Diagnostics;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using Dapper;
using DapperVsEntityFramework.Common;
using Microsoft.EntityFrameworkCore;

namespace DapperVsEntityFramework.SelectN;

[MemoryDiagnoser(displayGenColumns: true)]
public class SelectNOrders : ReadBenchmarkBase
{
    [Params(10, 100, 1000)]
    public int N { get; set; }

    [Params(1, 5, 10, 20)]
    public int ItemsPerOrder { get; set; }

    private static readonly Func<AppDbContext, int, IAsyncEnumerable<EfCoreModels.Order>> CompiledQuery =
        EF.CompileAsyncQuery((AppDbContext db, int n) => db.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .ThenInclude(oi => oi.Product)
            .OrderBy(o => o.Id)
            .Take(n));

    [Benchmark(Baseline = true)]
    public async Task<List<EfCoreModels.Order>> EfCore()
    {
        var orders = await DbContext.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .ThenInclude(oi => oi.Product)
            .OrderBy(o => o.Id)
            .Take(N)
            .ToListAsync();

        return orders;
    }

    [Benchmark]
    public async Task<List<EfCoreModels.Order>> EfCore_Compiled()
    {
        var result = new List<EfCoreModels.Order>();
        await foreach (var order in CompiledQuery(DbContext, N))
        {
            result.Add(order);
        }

        return result;
    }

    [Benchmark]
    public async Task<List<DapperModels.Order>> Dapper()
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
            WHERE o.id IN (SELECT id FROM orders ORDER BY id LIMIT @N)
            ORDER BY o.id, p.id
            """;

        var orderMap = new Dictionary<int, DapperModels.Order>();

        await DapperConnection
            .QueryAsync<DapperModels.Order, DapperModels.OrderItem, DapperModels.Product, DapperModels.Order>(
                Sql,
                (o, oi, p) =>
                {
                    ref var order = ref CollectionsMarshal.GetValueRefOrAddDefault(
                        orderMap,
                        o.Id,
                        out var exists);

                    if (!exists)
                    {
                        order = o;
                        order.Items = new List<DapperModels.OrderItem>();
                    }

                    Debug.Assert(order is not null);
                    oi.Product = p;
                    order.Items.Add(oi);
                    return order;
                },
                new { N },
                splitOn: "Quantity,Id");

        return orderMap.Values
            .OrderBy(o => o.Id)
            .ToList();
    }

    protected override Task SetUpHookAsync()
    {
        return SeedDatabaseAsync(new SeedOptions(
            ProductsCount: Math.Max(5000, ItemsPerOrder * 1000),
            OrdersCount: 2000,
            MinItemsPerOrder: ItemsPerOrder,
            MaxItemsPerOrder: ItemsPerOrder));
    }
}