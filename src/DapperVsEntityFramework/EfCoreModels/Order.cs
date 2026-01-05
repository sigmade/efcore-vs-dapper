namespace DapperVsEntityFramework.EfCoreModels;

public class Order
{
    public int Id { get; set; }

    public int CustomerId { get; set; }

    public DateTimeOffset CreationDate { get; set; }

    public ICollection<OrderItem> Items { get; set; } = null!;
}