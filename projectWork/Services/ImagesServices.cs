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
                photo_id AS Id,
                title,
                original_title,
                year,
                place,
                path,
                description,
                state,
                price,
                booked_by
            FROM public.photos;
            """;

        return await connection.QueryAsync<Image>(query);
    }


    public async Task<Image?> GetByIdAsync(int id)
    {
        var connection = new Npgsql.NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        string query = """
            SELECT
                photo_id AS Id,
                title,
                original_title,
                year,
                place,
                path,
                description,
                state,
                price,
                booked_by
            FROM public.photos
            WHERE
                photo_id = @Id;
            """;

        return await connection.QuerySingleOrDefaultAsync<Image>(query, new { Id = id });
    }

    public async Task<int> InsertAsync(Image image)
    {
        var connection = new Npgsql.NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var parameters = new DynamicParameters(image);
        parameters.Add("State", image.State.ToString().ToLower());

        string query = """
            INSERT INTO public.photos
                (title, original_title, year, place, path, description, state, price, booked_by)
            VALUES
                (@Title, @OriginalTitle, @Year, @Place, @Path, @Description, photo_state, @Price, @BookedBy)
            RETURNING photo_id;
            """;

        return await connection.QuerySingleAsync<int>(query, parameters);
    }

    public async Task UpdateAsync(Image image)
    {
        var connection = new Npgsql.NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        string query = """
            UPDATE public.photos
            SET
                title = @Title,
                original_title = @OriginalTitle,
                year = @Year,
                place = @Place,
                path = @Path,
                description = @description,
                state = @State::photo_state,
                price = @price,
                booked_by = @BookedBy
            WHERE
                photo_id = @Id;
            """;

        await connection.ExecuteAsync(query, image);
    }

    public async Task DeleteAsync(int id)
    {
        var connection = new Npgsql.NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        string query = """
            DELETE FROM public.photos
            WHERE photo_id = @Id;
            """;

        await connection.ExecuteAsync(query, new { Id = id });
    }

    // =============================================================================== Upload immagini
    public async Task UploadAsync(IFormFile file, string path)
    {
        using var stream = new FileStream(path, FileMode.Create);
        await file.CopyToAsync(stream);
    }
}
