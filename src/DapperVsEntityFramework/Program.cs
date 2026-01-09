using BenchmarkDotNet.Running;
using DapperVsEntityFramework.SelectN;
using DapperVsEntityFramework.SelectOne;
using DapperVsEntityFramework.UpdateOne;

BenchmarkRunner.Run<SelectOneProduct>();
BenchmarkRunner.Run<SelectOneProductWithFilter>();
BenchmarkRunner.Run<SelectOneOrder>();
BenchmarkRunner.Run<SelectNProducts>();
BenchmarkRunner.Run<SelectNProductsWithFilter>();
BenchmarkRunner.Run<SelectNOrders>();
BenchmarkRunner.Run<SelectNProductsParallel>();
BenchmarkRunner.Run<UpdateOneProductById>();
BenchmarkRunner.Run<UpdateOneOrderById>();