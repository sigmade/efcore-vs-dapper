```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.7462/25H2/2025Update/HudsonValley2)
AMD Ryzen 7 6800H with Radeon Graphics 3.20GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.101
  [Host]     : .NET 10.0.1 (10.0.1, 10.0.125.57005), X64 RyuJIT x86-64-v3
  Job-CNUJVU : .NET 10.0.1 (10.0.1, 10.0.125.57005), X64 RyuJIT x86-64-v3

InvocationCount=1  UnrollFactor=1  

```
| Method                 | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Allocated | Alloc Ratio |
|----------------------- |---------:|----------:|----------:|---------:|------:|--------:|----------:|------------:|
| EfCore                 | 4.298 ms | 0.1950 ms | 0.5688 ms | 4.135 ms |  1.02 |    0.18 |    104 KB |        1.00 |
| EfCore_Attach_Hack     | 3.125 ms | 0.1444 ms | 0.4235 ms | 2.953 ms |  0.74 |    0.14 |  86.59 KB |        0.83 |
| Dapper_Full_Update     | 6.009 ms | 0.1198 ms | 0.2605 ms | 5.974 ms |  1.42 |    0.19 |  42.26 KB |        0.41 |
| Dapper_Tailored_Update | 2.224 ms | 0.0634 ms | 0.1787 ms | 2.209 ms |  0.53 |    0.08 |  23.39 KB |        0.22 |
