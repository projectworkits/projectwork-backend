using Dapper;
using projectWork.Authentication;
using projectWork.Models;

namespace projectWork.Services;

public class UsersServices
{
    private readonly string _connectionString;
    public UsersServices(IConfiguration configuration, PasswordServices passwordServices)
    {
        _connectionString = configuration.GetConnectionString("db");
    }

    public async Task RegisterUser(string username, string password_hash, string password_salt, string email)
    {
        using var connection = new Npgsql.NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        string query = """
            INSERT INTO users
                (username, password_hash, password_salt, email)
            VALUES
                (@username, @password_hash, @password_salt, @email);
            """;

        await connection.ExecuteAsync(query, new {username, password_hash, password_salt, email});
    }

    public async Task<User> GetById(int userId)
    {
        using var connection = new Npgsql.NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        string query = """
            SELECT
                user_id as id,
                username,
                password_salt,
                password_hash,
                email,
                verified,
                admin,
                collaborator
            FROM users
            WHERE
                user_id = @userId;
            """;

        return (await connection.QueryFirstOrDefaultAsync<User>(query, new { userId }))!;
    }

    public async Task<IEnumerable<User>> GetList()
    {
        using var connection = new Npgsql.NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        string query = """
            SELECT
                user_id as id,
                username,
                password_salt,
                password_hash,
                email,
                verified,
                admin,
                collaborator
            FROM users;
            """;

        return await connection.QueryAsync<User>(query);
    }

    public async Task UpdateUser(User user)
    {
        using var connection = new Npgsql.NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        //campo admin non viene cambiato
        string query = """
            UPDATE users
            SET
                username = @Username,
                password_salt = @PasswordSalt,
                password_hash = @PasswordHash,
                email = @Email,
                verified = @Verified,
                collaborator = @Collaborator
            WHERE
                user_id = @Id;
            """;

        await connection.ExecuteAsync(query, user);
    }

    public async Task<bool> DeleteUser(int userId)
    {
        using var connection = new Npgsql.NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        if (await IsAdmin(userId))
            return false;

        //campo admin non viene cambiato
        string query = """
            DELETE FROM users WHERE user_id = @userId;
            """;

        await connection.ExecuteAsync(query, new {userId});
        return true;
    }

    public async Task<bool> IsAdmin(int userId)
    {
        using var connection = new Npgsql.NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        string query = """
            SELECT admin
            FROM users
            WHERE
                user_id = @userId;
            """;

        return await connection.ExecuteScalarAsync<bool>(query, new { userId });
    }

    public async Task<bool> IsCollaborator(int userId)
    {
        using var connection = new Npgsql.NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        string query = """
            SELECT collaborator
            FROM users
            WHERE
                user_id = @userId;
            """;

        return await connection.ExecuteScalarAsync<bool>(query, new { userId });
    }
}
