using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using Dapper;
using DapperVsEntityFramework.Common;
using Microsoft.EntityFrameworkCore;

namespace DapperVsEntityFramework.UpdateOne;

[MemoryDiagnoser(displayGenColumns: true)]
public class UpdateOneOrderById : WriteBenchmarkBase
{
    [Benchmark(Baseline = true)]
    public async Task<int> EfCore()
    {
        await using var dbContext = CreateDbContext();
        var targetId = 1;
        var order = await dbContext.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == targetId);
        Debug.Assert(order?.Items != null);
        order.CustomerId = 136;
        order.Items.Remove(order.Items.First(i => i.ProductId == 1));
        return await dbContext.SaveChangesAsync();
    }

    [Benchmark]
    public async Task<int> EfCore_Attach_Hack()
    {
        await using var dbContext = CreateDbContext();

        var item = new EfCoreModels.OrderItem() { ProductId = 1, OrderId = 1 };
        var order = new EfCoreModels.Order() { Id = 1, Items = new List<EfCoreModels.OrderItem> { item } };

        dbContext.Orders.Attach(order);
        dbContext.OrderItems.Attach(item);
        order.CustomerId = 136;
        order.Items.Remove(item);
        return await dbContext.SaveChangesAsync();
    }

    [Benchmark]
    public async Task<int> Dapper_Full_Update()
    {
        await using var connection = CreateConnection();

        // 1) Update order's customer_id
        var affected1 = await connection.ExecuteAsync(
            "UPDATE orders SET customer_id = @CustomerId WHERE id = @Id",
            new { Id = 1, CustomerId = 136 });

        // 2) Delete all existing order_item relations for this order
        var affected2 = await connection.ExecuteAsync(
            "DELETE FROM order_items WHERE order_id = @Id",
            new { Id = 1 });

        // 3) Insert 4 rows into order_items for this order
        var items = new[]
        {
            new { Id = 1, ProductId = 1, Quantity = 1 },
            new { Id = 1, ProductId = 2, Quantity = 2 },
            new { Id = 1, ProductId = 3, Quantity = 3 },
            new { Id = 1, ProductId = 4, Quantity = 4 },
        };
        var affected3 = await connection.ExecuteAsync(
            "INSERT INTO order_items (order_id, product_id, quantity) VALUES (@Id, @ProductId, @Quantity)",
            items);

        return affected1 + affected2 + affected3;
    }

    [Benchmark]
    public async Task<int> Dapper_Tailored_Update()
    {
        await using var connection = CreateConnection();
        return await connection.ExecuteAsync(
            """
            UPDATE orders SET customer_id = @CustomerId WHERE id = @Id;
            DELETE FROM order_items WHERE order_id = @Id AND product_id = @ProductId;
            """, new { Id = 1, ProductId = 1, CustomerId = 136 });
    }

    protected override void SetUpHook()
    {
        SeedDatabase(new SeedOptions(
            ProductsCount: 5,
            OrdersCount: 50,
            MinItemsPerOrder: 5,
            MaxItemsPerOrder: 5));
    }
}