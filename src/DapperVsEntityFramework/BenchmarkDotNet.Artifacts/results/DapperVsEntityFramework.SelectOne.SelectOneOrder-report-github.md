```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.7462/25H2/2025Update/HudsonValley2)
AMD Ryzen 7 6800H with Radeon Graphics 3.20GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.101
  [Host]     : .NET 10.0.1 (10.0.1, 10.0.125.57005), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.1 (10.0.1, 10.0.125.57005), X64 RyuJIT x86-64-v3


```
| Method          | Mean     | Error    | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
|---------------- |---------:|---------:|--------:|------:|-------:|----------:|------------:|
| EfCore          | 875.8 μs | 11.26 μs | 9.40 μs |  1.00 | 1.9531 |   16.6 KB |        1.00 |
| EfCore_Compiled | 801.3 μs |  8.85 μs | 7.39 μs |  0.92 | 0.9766 |   9.36 KB |        0.56 |
| Dapper          | 772.2 μs |  7.54 μs | 7.05 μs |  0.88 |      - |    4.2 KB |        0.25 |
