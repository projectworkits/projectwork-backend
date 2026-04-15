using Dapper;
using projectWork.Authentication;

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
}
