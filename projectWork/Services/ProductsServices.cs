using Dapper;
using projectWork.Models;

namespace projectWork.Services;

public class ProductsServices
{
    private readonly string _connectionString;

    public ProductsServices(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("db");
    }

    public async Task<IEnumerable<Product>> GetListAsync()
    {
        var connection = new Npgsql.NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        string query = """
            SELECT
                product_id,
                name,
                description,
                price,
                available,
                sold
            FROM public.products;
            """;

        return await connection.QueryAsync<Product>(query);
    }

    public async Task<Product?> GetByIdAsync(int productId)
    {
        var connection = new Npgsql.NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        string query = """
            SELECT
                product_id,
                name,
                description,
                price,
                available,
                sold
            FROM public.products
            WHERE
                product_id = @productId;
            """;

        return await connection.QueryFirstOrDefaultAsync<Product?>(query, new { productId });
    }

    public async Task InsertAsync(Product product)
    {
        var connection = new Npgsql.NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        string query = """
            INSERT INTO public.products
                (name, description, price, available, sold)
            VALUES
                (@name, @description, @price, @available, @sold);
            """;

        await connection.ExecuteAsync(query, product);
    }

    public async Task UpdateAsync(Product product)
    {
        var connection = new Npgsql.NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        string query = """
            UPDATE public.products
            SET
                name = @name,
                description = @description,
                price = @price,
                available = @available,
                sold = @sold
            WHERE
                product_id = @productId;
            """;

        await connection.ExecuteAsync(query, product);
    }

    public async Task DeleteAsync(int productId)
    {
        var connection = new Npgsql.NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        string query = """
            DELETE FROM public.products
            WHERE
                product_id = @productId;
            """;

        await connection.ExecuteAsync(query, new { productId });
    }

    // ======================================================================= servizi api extra

    public async Task SetSoldProduct(int productId, int quantity)
    {
        var connection = new Npgsql.NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        string query = """
            UPDATE public.products
            SET
                available = available - @quantity,
                sold = sold + @quantity
            WHERE
                product_id = @productId;
            """;

        await connection.ExecuteAsync(query, new { productId, quantity });
    }

    public async Task AddAvailableProduct(int productId, int quantity)
    {
        var connection = new Npgsql.NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        string query = """
            UPDATE public.products
            SET
                available = available + @quantity
            WHERE
                product_id = @productId;
            """;

        await connection.ExecuteAsync(query, new { productId, quantity });
    }
}
