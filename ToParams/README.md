将对象转换成Url格式
例：`new Car { Id = 2, CarName = "bc" }`
转换成 `Id=2&CarName=bc`

`BenchmarkDotNet=v0.11.4, OS=Windows 10.0.17134.590 (1803/April2018Update/Redstone4)
Intel Core i5-4590 CPU 3.30GHz (Haswell), 1 CPU, 4 logical and 4 physical cores
Frequency=3215225 Hz, Resolution=311.0202 ns, Timer=TSC
.NET Core SDK=2.1.504
  [Host]     : .NET Core 2.1.8 (CoreCLR 4.6.27317.03, CoreFX 4.6.27317.03), 64bit RyuJIT  [AttachedDebugger]
  DefaultJob : .NET Core 2.1.8 (CoreCLR 4.6.27317.03, CoreFX 4.6.27317.03), 64bit RyuJIT
`

|              Method |       Mean |     Error |    StdDev |
|-------------------- |-----------:|----------:|----------:|
|  ExpressionFuncTest |   823.2 ns | 11.603 ns | 10.853 ns |
| ExpressionFuncTest2 |   206.7 ns |  2.536 ns |  2.372 ns |
|       ReflectorTest | 1,720.0 ns | 17.377 ns | 16.254 ns |
|      ReflectorTest2 |   477.8 ns |  3.617 ns |  3.206 ns |

### 可以看到，使用表达式树消耗的时间是使用反射的一半
