```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.7462/25H2/2025Update/HudsonValley2)
AMD Ryzen 7 6800H with Radeon Graphics 3.20GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.101
  [Host]     : .NET 10.0.1 (10.0.1, 10.0.125.57005), X64 RyuJIT x86-64-v3
  Job-CNUJVU : .NET 10.0.1 (10.0.1, 10.0.125.57005), X64 RyuJIT x86-64-v3

InvocationCount=1  UnrollFactor=1  

```
| Method             | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |---------:|----------:|----------:|---------:|------:|--------:|----------:|------------:|
| EfCore             | 3.375 ms | 0.1399 ms | 0.4102 ms | 3.283 ms |  1.01 |    0.17 |  89.32 KB |        1.00 |
| EfCore_Attach_Hack | 2.438 ms | 0.1378 ms | 0.4019 ms | 2.316 ms |  0.73 |    0.15 |  77.51 KB |        0.87 |
| Dapper             | 2.307 ms | 0.0585 ms | 0.1650 ms | 2.280 ms |  0.69 |    0.09 |  21.85 KB |        0.24 |
