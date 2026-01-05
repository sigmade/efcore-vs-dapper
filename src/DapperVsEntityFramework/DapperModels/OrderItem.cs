namespace DapperVsEntityFramework.DapperModels;

public sealed class OrderItem
{
    public int Quantity { get; set; }

    public Product Product { get; set; } = null!;
}