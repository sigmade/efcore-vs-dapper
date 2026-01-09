```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.7462/25H2/2025Update/HudsonValley2)
AMD Ryzen 7 6800H with Radeon Graphics 3.20GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.101
  [Host]     : .NET 10.0.1 (10.0.1, 10.0.125.57005), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.1 (10.0.1, 10.0.125.57005), X64 RyuJIT x86-64-v3


```
| Method          | Mean     | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|---------------- |---------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| EfCore          | 758.6 μs | 11.13 μs |  9.86 μs |  1.00 |    0.02 | 0.9766 |   9.52 KB |        1.00 |
| EfCore_Compiled | 650.2 μs | 12.00 μs | 11.22 μs |  0.86 |    0.02 |      - |   4.43 KB |        0.46 |
| Dapper          | 633.9 μs | 10.99 μs | 10.28 μs |  0.84 |    0.02 |      - |   2.43 KB |        0.26 |
