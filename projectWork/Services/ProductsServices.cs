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
                product_id AS Id,
                name,
                description,
                price,
                available,
                booked,
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
                product_id AS Id,
                name,
                description,
                price,
                available,
                booked,
                sold
            FROM public.products
            WHERE
                product_id = @productId;
            """;

        return await connection.QueryFirstOrDefaultAsync<Product?>(query, new {productId});
    }

    public async Task InsertAsync(Product product)
    {
        var connection = new Npgsql.NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        string query = """
            INSERT INTO public.products
                (name, description, price, available, booked, sold)
            VALUES
                (@name, @description, @price, @available, @booked, @sold);
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
                booked = @booked,
                sold = @sold
            WHERE
                product_id = @id;
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

        await connection.ExecuteAsync(query, new {productId});
    }
}
