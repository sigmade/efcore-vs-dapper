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

public class WriteBenchmarkBase
{
    private PostgreSqlContainer? _container;
    private DbConnection? _dapperConnection;
    private AppDbContext? _dbContext;

    protected virtual void SetUpHook()
    {
    }

    protected virtual void TearDownHook()
    {
    }


    public DbConnection DapperConnection
    {
        get
        {
            _dapperConnection ??= CreateConnection();
            return _dapperConnection;
        }
    }

    public AppDbContext DbContext
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

    [IterationSetup]
    public void SetUp()
    {
        // Ensure Dapper maps snake_case (underscored) column names to PascalCase property names
        DefaultTypeMap.MatchNamesWithUnderscores = true;

        _container = new PostgreSqlBuilder()
            .WithImage("postgres:17.6")
            .WithLogger(NullLogger.Instance)
            .WithCommand()
            .WithWaitStrategy(Wait.ForUnixContainer())
            .Build();

        _container.StartAsync().Wait();

        using (var connection = GetConnection())
        {
            connection.Execute(GetSchemaSql());
        }

        SetUpHook();
    }

    [IterationCleanup]
    public void TearDown()
    {
        if (_dapperConnection is not null)
        {
            _dapperConnection.Dispose();
            _dapperConnection = null;
        }

        if (_dbContext is not null)
        {
            _dbContext.Dispose();
            _dbContext = null;
        }

        if (_container is not null)
        {
            _container.DisposeAsync().GetAwaiter().GetResult();
            _container = null;
        }

        TearDownHook();
    }

    protected void SeedDatabase(SeedOptions options)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(options.MinItemsPerOrder, options.ProductsCount);
        ArgumentOutOfRangeException.ThrowIfLessThan(options.MaxItemsPerOrder, options.MinItemsPerOrder);

        var faker = new Faker
        {
            Random = new Randomizer(options.RandomizerSeed)
        };

        using var connection = GetConnection();

        const string CopyProductsSql = "COPY products (id, name, description, price) FROM STDIN (FORMAT BINARY)";
        using (var writer = connection.BeginBinaryImport(CopyProductsSql))
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

                writer.StartRow();
                writer.Write(i + 1, NpgsqlDbType.Integer);
                writer.Write(name, NpgsqlDbType.Text);
                writer.Write(faker.Commerce.ProductDescription(), NpgsqlDbType.Text);
                writer.Write(faker.Random.Decimal(1, 1000), NpgsqlDbType.Numeric);
            }

            writer.Complete();
        }

        const string CopyOrdersSql = "COPY orders (id, customer_id, creation_date) FROM STDIN (FORMAT BINARY)";
        using (var writer = connection.BeginBinaryImport(CopyOrdersSql))
        {
            for (var i = 0; i < options.OrdersCount; i++)
            {
                writer.StartRow();
                writer.Write(i + 1, NpgsqlDbType.Integer);
                writer.Write(faker.Random.Int(1, 1000), NpgsqlDbType.Integer);
                writer.Write(faker.Date.BetweenOffset(
                    new DateTimeOffset(2025, 01, 01, 0, 0, 0, TimeSpan.Zero).AddYears(-10),
                    new DateTimeOffset(2025, 01, 01, 0, 0, 0, TimeSpan.Zero)), NpgsqlDbType.TimestampTz);
            }

            writer.Complete();
        }

        const string CopyOrderItemsSql = "COPY order_items (order_id, product_id, quantity) FROM STDIN (FORMAT BINARY)";
        using (var writer = connection.BeginBinaryImport(CopyOrderItemsSql))
        {
            foreach (var orderId in Enumerable.Range(1, options.OrdersCount))
            {
                var itemIds = faker.PickRandom(
                    Enumerable.Range(1, options.ProductsCount),
                    faker.Random.Number(options.MinItemsPerOrder, options.MaxItemsPerOrder));

                foreach (var itemId in itemIds)
                {
                    writer.StartRow();
                    writer.Write(orderId, NpgsqlDbType.Integer);
                    writer.Write(itemId, NpgsqlDbType.Integer);
                    writer.Write(faker.Random.Int(1, 10), NpgsqlDbType.Integer);
                }
            }

            writer.Complete();
        }

        connection.Execute("ALTER SYSTEM SET max_connections = 1000;");
    }

    private NpgsqlConnection GetConnection()
    {
        var connectionString = GetConnectionString();

        NpgsqlConnection? connection = null;

        try
        {
            connection = new NpgsqlConnection(connectionString);
            connection.Open();
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