using Dapper;
using projectWork.Models;

namespace projectWork.Services;

public class ImagesServices
{
    private readonly string _connectionString;
    
    public ImagesServices(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("db");
    }

    public async Task<IEnumerable<Image>> GetListAsync()
    {
        var connection = new Npgsql.NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        string query = """
            SELECT
                image_id AS Id,
                title,
                original_title,
                year,
                place,
                path
            FROM public.images;
            """;

        return await connection.QueryAsync<Image>(query);
    }


    public async Task<Image?> GetByIdAsync(int id)
    {
        var connection = new Npgsql.NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        string query = """
            SELECT
                image_id AS Id,
                title,
                original_title,
                year,
                place,
                path
            FROM public.images
            WHERE
                image_id = @Id;
            """;

        return await connection.QuerySingleOrDefaultAsync<Image>(query, new { Id = id });
    }

    public async Task<int> InsertAsync(Image image)
    {
        var connection = new Npgsql.NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        string query = """
            INSERT INTO public.images
                (title, original_title, year, place, path)
            VALUES
                (@Title, @OriginalTitle, @Year, @Place, @Path)
            RETURNING image_id;
            """;

        return await connection.QuerySingleAsync<int>(query, image);
    }

    public async Task UpdateAsync(Image image)
    {
        var connection = new Npgsql.NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        string query = """
            UPDATE public.images
            SET
                title = @Title,
                original_title = @OriginalTitle,
                year = @Year,
                place = @Place,
                path = @Path
            WHERE
                image_id = @Id;
            """;

        await connection.ExecuteAsync(query, image);
    }

    public async Task DeleteAsync(int id)
    {
        var connection = new Npgsql.NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        string query = """
            DELETE FROM public.images
            WHERE image_id = @Id;
            """;

        await connection.ExecuteAsync(query, new { Id = id });
    }

}
