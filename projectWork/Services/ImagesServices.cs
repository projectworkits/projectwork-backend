using Dapper;
using Image = projectWork.Models.Image;

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
                photo_id,
                title,
                original_title,
                date,
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
                photo_id,
                title,
                original_title,
                date,
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

        return await connection.QuerySingleOrDefaultAsync<Image?>(query, new { Id = id });
    }

    public async Task InsertAsync(Image image, IFormFile file)
    {
        var connection = new Npgsql.NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        string query = """
            INSERT INTO public.photos
                (title, original_title, date, place, path, description, state, price, booked_by)
            VALUES
                (@Title, @OriginalTitle, @date, @Place, @Path, @Description, @State::photo_state, @Price, @BookedBy);
            """;

        await connection.ExecuteAsync(query, new
        {
            image.Title,
            image.OriginalTitle,
            image.Date,
            image.Place,
            image.Path,
            image.Description,
            State = image.State.ToString().ToLower(),
            image.Price,
            image.BookedBy
        });

        //inserimento file
        using var stream = new FileStream("/frontend"+image.Path, FileMode.Create);
        await file.CopyToAsync(stream);
    }

    public async Task UpdateAsync(Image image)
    {
        // potrebbe dover riscrivere anche l'immagine

        var connection = new Npgsql.NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        string query = """
            UPDATE public.photos
            SET
                title = @Title,
                original_title = @OriginalTitle,
                date = @Date,
                place = @Place,
                path = @Path,
                description = @Description,
                state = @State::photo_state,
                price = @price,
                booked_by = @BookedBy
            WHERE
                photo_id = @PhotoId;
            """;

        await connection.ExecuteAsync(query, new
        {
            image.Title,
            image.OriginalTitle,
            image.Date,
            image.Place,
            image.Path,
            image.Description,
            State = image.State.ToString().ToLower(),
            image.Price,
            image.BookedBy,
            image.PhotoId
        });
    }

    public async Task DeleteAsync(int id)
    {
        var connection = new Npgsql.NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        string query = """
            DELETE FROM public.photos
            WHERE
                photo_id = @Id
            RETURNING
                path;
            """;

        // esistenza del record già confermata dall'endpoint

        string path = (await connection.ExecuteScalarAsync<string>(query, new { Id = id }))!;

        //cancellazione del file
        if(File.Exists(path))
            File.Delete(path);
    }

    // ======================================================================= servizi api extra

    public async Task<IEnumerable<Image>> GetListFilterAsync(string state)
    {
        var connection = new Npgsql.NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        string query = """
            SELECT
                photo_id,
                title,
                original_title,
                date,
                place,
                path,
                description,
                state,
                price,
                booked_by
            FROM public.photos
            WHERE
                state = @state;
            """;

        return await connection.QueryAsync<Image>(query, new {state});
    }

    public async Task<bool> IsBooked(int photoId)
    {
        var connection = new Npgsql.NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        string query = """
            SELECT
                booked_by
            FROM public.photos
            WHERE
                photo_id = @photoId;
            """;

        if (await connection.ExecuteScalarAsync<bool?>(query, new { photoId }) ?? false)
            return true;
        else
            return false;
    }


    public async Task BookImage(int photoId, int userId)
    {
        var connection = new Npgsql.NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        string query = """
            UPDATE public.photos
            SET
                booked_by = @userId
            WHERE
                photo_id = @photoId;
            """;

        await connection.ExecuteAsync(query, new { userId, photoId });
    }

    public async Task<int> GetUserOfBookedImage(int photoId)
    {
        var connection = new Npgsql.NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        string query = """
            SELECT
                booked_by
            FROM public.photos
            WHERE
                photo_id = @photoId;
            """;

        return await connection.ExecuteScalarAsync<int>(query, new { photoId });
    }

    public async Task UnbookImage(int photoId)
    {
        var connection = new Npgsql.NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        string query = """
            UPDATE public.photos
            SET
                booked_by = NULL
            WHERE
                photo_id = @photoId;
            """;

        await connection.ExecuteAsync(query, new { photoId });
    }

    public async Task SetSoldImage(int photoId)
    {
        var connection = new Npgsql.NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        string query = """
            UPDATE public.photos
            SET
                state = 'sold'
            WHERE
                photo_id = @photoId;
            """;

        await connection.ExecuteAsync(query, new { photoId });
    }
}
