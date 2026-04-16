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
                path
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
                path
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

        string query = """
            INSERT INTO public.photos
                (title, original_title, year, place, path)
            VALUES
                (@Title, @OriginalTitle, @Year, @Place, @Path)
            RETURNING photo_id;
            """;

        return await connection.QuerySingleAsync<int>(query, image);
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
                path = @Path
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
    public async Task UploadAsync(HttpRequest request)
    {
        var form = await request.ReadFormAsync();
        var file = form.Files.GetFile("photo");

        Image img = new Image();

        img.Title = form["Title"];
        img.OriginalTitle = form["OriginalTitle"];
        img.Year = short.Parse(form["Title"]);
        img.Place = form["Place"];
        img.Path = "/photos/" + Guid.NewGuid();

        using var stream = new FileStream(img.Path, FileMode.Create);
        await file.CopyToAsync(stream);

        await InsertAsync(img);
    }
}
