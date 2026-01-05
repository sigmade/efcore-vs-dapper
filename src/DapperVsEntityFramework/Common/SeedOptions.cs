namespace DapperVsEntityFramework.Common;

public readonly record struct SeedOptions(
    int ProductsCount,
    int OrdersCount,
    int MinItemsPerOrder = 0,
    int MaxItemsPerOrder = 10,
    int RandomizerSeed = 42069);