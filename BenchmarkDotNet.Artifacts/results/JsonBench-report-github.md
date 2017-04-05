``` ini

BenchmarkDotNet=v0.10.3.0, OS=OSX
Processor=Intel(R) Core(TM) i7-4980HQ CPU 2.80GHz, ProcessorCount=8
Frequency=1000000000 Hz, Resolution=1.0000 ns, Timer=UNKNOWN
dotnet cli version=1.0.1
  [Host]     : .NET Core 4.6.25009.03, 64bit RyuJIT
  DefaultJob : .NET Core 4.6.25009.03, 64bit RyuJIT


```
 |           Method |       Mean |    StdDev |     Gen 0 |    Gen 1 | Allocated |
 |----------------- |----------- |---------- |---------- |--------- |---------- |
 |   BigJson_Pidgin |  5.8688 ms | 0.1153 ms |         - |        - |   1.06 MB |
 |  BigJson_Sprache | 10.3357 ms | 0.1617 ms | 2287.5000 |  68.7500 |  16.19 MB |
 |  LongJson_Pidgin |  6.8819 ms | 0.2078 ms |         - |        - |   1.09 MB |
 | LongJson_Sprache | 11.7027 ms | 0.2257 ms | 2625.0000 |  62.5000 |  18.06 MB |
 |  DeepJson_Pidgin |  5.2995 ms | 0.1217 ms |         - |        - | 654.85 kB |
 | DeepJson_Sprache | 17.8569 ms | 0.2125 ms | 1387.5000 | 325.0000 |  12.25 MB |
 |  WideJson_Pidgin |  4.4128 ms | 0.0546 ms |         - |        - |    855 kB |
 | WideJson_Sprache |  7.1498 ms | 0.1873 ms | 1591.6667 |  29.1667 |  11.78 MB |
