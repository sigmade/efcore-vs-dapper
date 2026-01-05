using System.Data.Common;
using BenchmarkDotNet.Attributes;
using Bogus;
using Dapper;
using DotNet.Testcontainers.Builders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using NpgsqlTypes;
using Testcontainers.PostgreSql;

namespace DapperVsEntityFramework.Common;

public abstract class ReadBenchmarkBase
{
    private PostgreSqlContainer? _container;
    private DbConnection? _dapperConnection;
    private AppDbContext? _dbContext;

    protected virtual Task SetUpHookAsync() => Task.CompletedTask;
    
    protected virtual Task TearDownHookAsync() => Task.CompletedTask;


    protected DbConnection DapperConnection
    {
        get
        {
            _dapperConnection ??= CreateConnection();
            return _dapperConnection;
        }
    }

    protected AppDbContext DbContext
    {
        get
        {
            if (_dbContext is null)
            {
                _dbContext = CreateDbContext();
            }

            return _dbContext;
        }
    }

    protected NpgsqlConnection CreateConnection()
    {
        return new NpgsqlConnection(GetConnectionString());
    }

    protected AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(GetConnectionString())
            .Options;

        return new AppDbContext(options);
    }

    [GlobalSetup]
    public async Task SetUpAsync()
    {
        // Ensure Dapper maps snake_case (underscored) column names to PascalCase property names
        DefaultTypeMap.MatchNamesWithUnderscores = true;

        _container = new PostgreSqlBuilder()
            .WithImage("postgres:17.6")
            .WithLogger(NullLogger.Instance)
            .WithCommand()
            .WithWaitStrategy(Wait.ForUnixContainer())
            .Build();

        await _container.StartAsync();

        await using (var connection = await GetConnectionAsync())
        {
            await connection.ExecuteAsync(GetSchemaSql());
        }

        await SetUpHookAsync();
    }

    [GlobalCleanup]
    public async Task TearDownAsync()
    {
        if (_dapperConnection is not null)
        {
            await _dapperConnection.DisposeAsync();
            _dapperConnection = null;
        }

        if (_dbContext is not null)
        {
            await _dbContext.DisposeAsync();
            _dbContext = null;
        }

        if (_container is not null)
        {
            await _container.DisposeAsync();

            _container = null;
        }

        await TearDownHookAsync();
    }

    protected async Task SeedDatabaseAsync(SeedOptions options)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(options.MinItemsPerOrder, options.ProductsCount);
        ArgumentOutOfRangeException.ThrowIfLessThan(options.MaxItemsPerOrder, options.MinItemsPerOrder);

        var faker = new Faker
        {
            Random = new Randomizer(options.RandomizerSeed)
        };

        await using var connection = await GetConnectionAsync();

        const string CopyProductsSql = "COPY products (id, name, description, price) FROM STDIN (FORMAT BINARY)";
        await using (var writer = await connection.BeginBinaryImportAsync(CopyProductsSql))
        {
            var usedNames = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < options.ProductsCount; i++)
            {
                var baseName = faker.Commerce.ProductName();
                var name = baseName;
                var suffix = 2;
                while (!usedNames.Add(name))
                {
                    name = $"{baseName} #{suffix++}"; // Ideally, we shouldn't be here
                }

                await writer.StartRowAsync();
                await writer.WriteAsync(i + 1, NpgsqlDbType.Integer);
                await writer.WriteAsync(name, NpgsqlDbType.Text);
                await writer.WriteAsync(faker.Commerce.ProductDescription(), NpgsqlDbType.Text);
                await writer.WriteAsync(faker.Random.Decimal(1, 1000), NpgsqlDbType.Numeric);
            }

            await writer.CompleteAsync();
        }

        const string CopyOrdersSql = "COPY orders (id, customer_id, creation_date) FROM STDIN (FORMAT BINARY)";
        await using (var writer = await connection.BeginBinaryImportAsync(CopyOrdersSql))
        {
            for (var i = 0; i < options.OrdersCount; i++)
            {
                await writer.StartRowAsync();
                await writer.WriteAsync(i + 1, NpgsqlDbType.Integer);
                await writer.WriteAsync(faker.Random.Int(1, 1000), NpgsqlDbType.Integer);
                await writer.WriteAsync(faker.Date.BetweenOffset(
                    new DateTimeOffset(2025, 01, 01, 0, 0, 0, TimeSpan.Zero).AddYears(-10),
                    new DateTimeOffset(2025, 01, 01, 0, 0, 0, TimeSpan.Zero)), NpgsqlDbType.TimestampTz);
            }

            await writer.CompleteAsync();
        }

        const string CopyOrderItemsSql = "COPY order_items (order_id, product_id, quantity) FROM STDIN (FORMAT BINARY)";
        await using (var writer = await connection.BeginBinaryImportAsync(CopyOrderItemsSql))
        {
            foreach (var orderId in Enumerable.Range(1, options.OrdersCount))
            {
                var itemIds = faker.PickRandom(
                    Enumerable.Range(1, options.ProductsCount),
                    faker.Random.Number(options.MinItemsPerOrder, options.MaxItemsPerOrder));

                foreach (var itemId in itemIds)
                {
                    await writer.StartRowAsync();
                    await writer.WriteAsync(orderId, NpgsqlDbType.Integer);
                    await writer.WriteAsync(itemId, NpgsqlDbType.Integer);
                    await writer.WriteAsync(faker.Random.Int(1, 10), NpgsqlDbType.Integer);
                }
            }

            await writer.CompleteAsync();
        }

        await connection.ExecuteAsync("ALTER SYSTEM SET max_connections = 1000;");
    }

    private async Task<NpgsqlConnection> GetConnectionAsync()
    {
        var connectionString = GetConnectionString();

        NpgsqlConnection? connection = null;

        try
        {
            connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            return connection;
        }
        catch (Exception)
        {
            connection?.Dispose();
            throw;
        }
    }

    private string GetConnectionString()
    {
        if (_container is null) throw new InvalidOperationException("Database container is not initialized.");
        var builder = new NpgsqlConnectionStringBuilder(_container.GetConnectionString())
        {
            MaxPoolSize = 100,
            CommandTimeout = 120
        };
        return builder.ToString();
    }

    private static string GetSchemaSql()
    {
        return """
               CREATE TABLE IF NOT EXISTS products (
                   id INT NOT NULL,
                   name VARCHAR(255) NOT NULL,
                   price NUMERIC(10,2) NOT NULL,
                   description VARCHAR(255),
                   PRIMARY KEY (id),
                   UNIQUE (name)
               );

               CREATE TABLE IF NOT EXISTS orders (
                   id INT NOT NULL,
                   customer_id INT NOT NULL,
                   creation_date TIMESTAMPTZ NOT NULL,
                   PRIMARY KEY (id)
               );

               CREATE TABLE IF NOT EXISTS order_items (
                   order_id INT NOT NULL,
                   product_id INT NOT NULL,
                   quantity INT NOT NULL,
                   PRIMARY KEY (order_id, product_id),
                   FOREIGN KEY (order_id) REFERENCES orders (id),
                   FOREIGN KEY (product_id) REFERENCES products (id)
               );

               CREATE INDEX IF NOT EXISTS idx_orders_customer_id ON orders (customer_id);
               CREATE INDEX IF NOT EXISTS idx_order_items_product_id ON order_items (product_id);
               CREATE INDEX IF NOT EXISTS idx_order_items_order_id ON order_items (order_id);
               """;
    }
}