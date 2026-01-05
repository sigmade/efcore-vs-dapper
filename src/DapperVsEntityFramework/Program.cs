using BenchmarkDotNet.Running;
using DapperVsEntityFramework.SelectN;
using DapperVsEntityFramework.UpdateOne;
using Microsoft.EntityFrameworkCore;

BenchmarkRunner.Run<UpdateOneOrderById>();
// var bench = new UpdateOneOrderById();
//
// try
// {
//     await bench.SetUpAsync();
//     await bench.Dapper_Tailored_Update();
//
//     var orders = await bench.DbContext.Orders
//         .Include(o => o.Items)
//         .FirstOrDefaultAsync(o => o.Id == 1);
// }
// finally
// {
//     await bench.TearDownAsync();
// }